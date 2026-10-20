param([string]$PclPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& "$root\tests\hmcl.ps1"
$fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$cecil=Join-Path $root 'tools\cecil\lib\net40\Mono.Cecil.dll'
Copy-Item -LiteralPath $cecil -Destination (Join-Path $root '.test-data\Mono.Cecil.dll')
$test=Join-Path $root '.test-data\LauncherDiscoveryTest.exe'
& "$fx\csc.exe" /nologo /optimize+ "/out:$test" "/reference:$cecil" "/reference:$fx\System.Web.Extensions.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\WPF\WindowsBase.dll" "$root\tests\LauncherDiscoveryTest.cs" "$root\src\LauncherDiscovery.cs" "$root\src\LauncherActions.cs" "$root\src\HmclLauncher.cs" "$root\src\Patcher.cs"
if($LASTEXITCODE -ne 0){throw 'Discovery test compilation failed'}
$source=if($PclPath){(Resolve-Path -LiteralPath $PclPath).Path}else{Join-Path $root '.test-data\original\Plain Craft Launcher 2.exe.pcl-helper.bak'}
$case=Join-Path $root ('.test-data\discovery test '+[Guid]::NewGuid().ToString('N'))
& $test $case $source (Join-Path $root '.test-data\hmcl launch test\HMCL-probe.exe') (Join-Path $root '.test-data\hmcl-preview\PCL-Helper-v1.exe')
if($LASTEXITCODE -ne 0){throw 'Discovery/batch test failed'}
