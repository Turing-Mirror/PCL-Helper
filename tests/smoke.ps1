param([string]$PclPath)
$ErrorActionPreference='Stop'
function Get-TestHash([string]$Path) {
 for($attempt=0;;$attempt++) {
  try{return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}
  catch{if($attempt -ge 50){throw};Start-Sleep -Milliseconds 100}
 }
}
$root=Split-Path $PSScriptRoot -Parent
& "$root\scripts\build.ps1" -Console
$test=Join-Path $root ('.test-data\smoke-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $test | Out-Null
$target=Join-Path $test 'PCL-test.exe'
$source=if($PclPath){(Resolve-Path -LiteralPath $PclPath).Path}else{Join-Path $root '.test-data\original\Plain Craft Launcher 2.exe.pcl-helper.bak'}
Copy-Item -LiteralPath $source -Destination $target
$cli=Join-Path $root '.test-data\PCL-Helper-CLI.exe'
$original=(Get-TestHash $target)
& $cli --enable $target
if($LASTEXITCODE -ne 0){throw 'enable failed'}
$patched=(Get-TestHash $target)
if($patched -eq $original){throw 'patch unchanged'}
& $cli --enable $target
if($LASTEXITCODE -ne 0 -or (Get-TestHash $target) -ne $patched){throw 'idempotence failed'}
& $cli --inspect $target
if($LASTEXITCODE -ne 0){throw 'inspect failed'}
& $cli --restore $target
if($LASTEXITCODE -ne 0 -or (Get-TestHash $target) -ne $original){throw 'restore hash failed'}
& $cli --restore $target
if($LASTEXITCODE -ne 0){throw 'repeat restore failed'}
& $cli --enable $target
if($LASTEXITCODE -ne 0){throw 'reapply failed'}
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& "$framework\csc.exe" /nologo /optimize+ "/out:$root\.test-data\JitTest.exe" "$root\tests\JitTest.cs"
if($LASTEXITCODE -ne 0){throw 'test compiler failed'}
& "$root\.test-data\JitTest.exe" $target
if($LASTEXITCODE -ne 0){throw 'JIT validation failed'}
& "$framework\csc.exe" /nologo /optimize+ "/out:$root\.test-data\PageTest.exe" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\WindowsBase.dll" "/reference:$framework\System.Xaml.dll" "$root\tests\PageTest.cs"
if($LASTEXITCODE -ne 0){throw 'WPF test compiler failed'}
& "$root\.test-data\PageTest.exe" $target
if($LASTEXITCODE -ne 0){throw 'actual WPF download page failed'}
$invalid=Join-Path $test 'invalid.exe'
Copy-Item -LiteralPath $cli -Destination $invalid
& $cli --enable $invalid
if($LASTEXITCODE -eq 0){throw 'invalid target accepted'}
'PASS: enable, inspect, repeat enable, SHA256 restore, repeat restore, reapply, six CLR JIT methods, invalid target rejection.'
$global:LASTEXITCODE=0



