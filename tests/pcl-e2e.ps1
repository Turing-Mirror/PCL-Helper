param([Parameter(Mandatory=$true)][string]$PclPath,[Parameter(Mandatory=$true)][string]$ModpackPath,[ValidateSet('install','repair')][string]$Mode='install',[string]$InstanceName='helper-e2e-modrinth',[string]$DownloadUrl,[string]$Sha1,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $root '.test-data\e2e\pcl'}
New-Item -ItemType Directory -Force $output | Out-Null
$target=Join-Path $output 'PCL.exe'
if(-not(Test-Path -LiteralPath $target)){Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PclPath).Path -Destination $target}
& "$root\scripts\build.ps1" -Console | Out-Null
& "$root\.test-data\PCL-Helper-CLI.exe" --enable $target
if($LASTEXITCODE -ne 0){throw 'PCL test copy patching failed'}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$runner=Join-Path $output 'PclModpackE2e.exe'
& "$framework\csc.exe" /nologo "/out:$runner" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\WindowsBase.dll" "/reference:$framework\System.Xaml.dll" "$PSScriptRoot\PclModpackE2e.cs"
if($LASTEXITCODE -ne 0){throw 'PCL e2e compilation failed'}
'<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>' | Set-Content -LiteralPath "$runner.config" -Encoding UTF8
$arguments=@($target,[IO.Path]::GetFullPath($ModpackPath),$Mode,$InstanceName)
if($DownloadUrl){if(-not $Sha1){throw 'Online download test requires the expected SHA1'};$arguments+=@($DownloadUrl,$Sha1)}
& $runner @arguments 2>&1 | Tee-Object -FilePath (Join-Path $output "e2e-$InstanceName-$Mode.log")
if($LASTEXITCODE -ne 0){throw 'Actual PCL e2e failed'}
