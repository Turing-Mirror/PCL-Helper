param([switch]$Console, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$cecil=Join-Path $root 'tools\cecil\lib\net40\Mono.Cecil.dll'
if(-not (Test-Path $cecil)) {
 New-Item -ItemType Directory -Force (Join-Path $root 'tools') | Out-Null
 $package=Join-Path $root 'tools\mono.cecil.nupkg'
 Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/mono.cecil/0.11.6/mono.cecil.0.11.6.nupkg' -OutFile $package
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 [IO.Compression.ZipFile]::ExtractToDirectory($package,(Join-Path $root 'tools\cecil'))
}
New-Item -ItemType Directory -Force (Join-Path $root 'release') | Out-Null
$kind=if($Console){'exe'}else{'winexe'}
$versionSource=Get-Content -Raw (Join-Path $root 'src\AppVersion.cs')
if($versionSource -notmatch 'public const string Name = "(v\d+)"'){throw 'Version definition missing'}
$versionName=$Matches[1]
$out=if($Console){'PCL-Helper-CLI.exe'}else{"PCL-Helper-$versionName.exe"}
$outputDir=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}elseif($Console){Join-Path $root '.test-data'}else{Join-Path $root 'release'}
New-Item -ItemType Directory -Force $outputDir | Out-Null
$refs=@('System.dll','System.Core.dll','System.Windows.Forms.dll','System.Xaml.dll','System.Web.Extensions.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:'+(Join-Path $framework $_) }
& $compiler /nologo /optimize+ /platform:anycpu "/target:$kind" "/out:$outputDir\$out" "/reference:$cecil" "/resource:$cecil,Mono.Cecil.dll" "/resource:$root\docs\Mono.Cecil.LICENSE.txt,Mono.Cecil.LICENSE.txt" @refs "$root\src\AppVersion.cs" "$root\src\Patcher.cs" "$root\src\HmclLauncher.cs" "$root\src\LauncherDiscovery.cs" "$root\src\LauncherActions.cs" "$root\src\Program.cs"
if($LASTEXITCODE -ne 0){throw '编译失败'}
Get-Item (Join-Path $outputDir $out) | Select-Object FullName,Length
