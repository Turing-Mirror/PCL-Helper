$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$preview=Join-Path $root '.test-data\hmcl-preview'
& "$root\scripts\build.ps1" -OutputDirectory $preview
$fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$probeDir=Join-Path $root '.test-data\hmcl launch test'
New-Item -ItemType Directory -Force $probeDir | Out-Null
$probe=Join-Path $probeDir 'HMCL-probe.exe'
& "$fx\csc.exe" /nologo /optimize+ "/out:$probe" "$root\tests\HmclLaunchProbe.cs"
if($LASTEXITCODE -ne 0){throw 'Probe compilation failed'}
$test=Join-Path $root '.test-data\HmclLaunchTest.exe'
& "$fx\csc.exe" /nologo /optimize+ "/out:$test" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\System.Xaml.dll" "$root\tests\HmclLaunchTest.cs" "$root\src\HmclLauncher.cs"
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& $test $probe (Join-Path $preview 'PCL-Helper-v2.exe')
if($LASTEXITCODE -ne 0){throw 'HMCL launch test failed'}
