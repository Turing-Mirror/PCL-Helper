param([string]$PclPath,[string]$PreviewDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$preview=if($PreviewDirectory){[IO.Path]::GetFullPath($PreviewDirectory)}else{Join-Path $root '.test-data\hmcl-preview'}
$build=& "$root\scripts\build.ps1" -OutputDirectory $preview
$fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$test=Join-Path $root '.test-data\GuiWorkflowTest.exe'
& "$fx\csc.exe" /nologo /optimize+ "/out:$test" "/reference:$fx\WPF\WindowsBase.dll" "/reference:$fx\WPF\PresentationCore.dll" "/reference:$fx\WPF\PresentationFramework.dll" "/reference:$fx\System.Xaml.dll" "$root\tests\GuiWorkflowTest.cs"
if($LASTEXITCODE -ne 0){throw 'GUI test compilation failed'}
$source=if($PclPath){(Resolve-Path -LiteralPath $PclPath).Path}else{Join-Path $root '.test-data\original\Plain Craft Launcher 2.exe.pcl-helper.bak'}
$case=Join-Path $root ('.test-data\gui case '+[Guid]::NewGuid().ToString('N'))
$evidence=Join-Path $root '.test-data\verification-v2'
& $test $build.FullName $source (Join-Path $root '.test-data\hmcl launch test\HMCL-probe.exe') $case $evidence
if($LASTEXITCODE -ne 0){throw 'GUI workflow test failed'}
