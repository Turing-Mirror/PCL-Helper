param(
 [Parameter(Mandatory=$true)][string]$OldPclPath,
 [Parameter(Mandatory=$true)][string]$NewPclPath,
 [Parameter(Mandatory=$true)][string]$HelperPath,
 [string[]]$LegacyHelpers=@(),
 [string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $root ('.test-data\compatibility-'+[Guid]::NewGuid().ToString('N'))}
New-Item -ItemType Directory -Force $output|Out-Null
$helper=(Resolve-Path -LiteralPath $HelperPath).Path
$old=(Resolve-Path -LiteralPath $OldPclPath).Path
$new=(Resolve-Path -LiteralPath $NewPclPath).Path
function Read-Hash([string]$Path){for($attempt=0;;$attempt++){try{return (Get-FileHash -LiteralPath $Path).Hash}catch{if($attempt -ge 50){throw};Start-Sleep -Milliseconds 100}}}
function Copy-TestFile([string]$Source,[string]$Target){for($attempt=0;;$attempt++){try{Copy-Item -LiteralPath $Source -Destination $Target;return}catch{if($attempt -ge 50){throw};Start-Sleep -Milliseconds 100}}}
$oldHash=Read-Hash $old;$newHash=Read-Hash $new
$script:number=0
function Assert-Hash([string]$Path,[string]$Expected){if((Read-Hash $Path) -ne $Expected){throw "Unexpected file contents: $Path"}}
function Invoke-Helper([string]$Program,[string]$Operation,[string]$Target,[int]$ExpectedExit=0){
 $script:number++
 $report=Join-Path $output ('operation-'+$script:number+'.txt')
 $process=Start-Process -FilePath $Program -ArgumentList @($Operation,('"'+$Target+'"'),'--report',('"'+$report+'"')) -Wait -PassThru -WindowStyle Hidden
 if($process.ExitCode -ne $ExpectedExit){throw "Helper $Operation exit $($process.ExitCode): $(Get-Content $report -Raw -ErrorAction SilentlyContinue)"}
}
foreach($legacy in @($LegacyHelpers)+@($helper)){
 $legacy=(Resolve-Path -LiteralPath $legacy).Path
 $label=[IO.Path]::GetFileNameWithoutExtension($legacy)
 $case=Join-Path $output $label;New-Item -ItemType Directory -Force $case|Out-Null
 $target=Join-Path $case 'PCL.exe'
 Copy-TestFile $old $target
 Invoke-Helper $legacy '--enable' $target
 Invoke-Helper $helper '--restore' $target
 Assert-Hash $target $oldHash
 Invoke-Helper $helper '--restore' $target
 Assert-Hash $target $oldHash
 Write-Output "PASS $label -> current Helper: 2.99.0.0 restored exactly."

 $backupHash=Read-Hash "$target.pcl-helper.bak"
 $recordHash=Read-Hash "$target.pcl-helper.json"
 # Reproduce the updater replacing the executable at the same path, keeping old sidecars.
 Copy-TestFile $new $target
 Invoke-Helper $helper '--restore' $target
 Assert-Hash $target $newHash
 Assert-Hash "$target.pcl-helper.bak" $backupHash
 Assert-Hash "$target.pcl-helper.json" $recordHash
 Invoke-Helper $helper '--enable' $target
 Assert-Hash "$target.pcl-helper.bak" $newHash
 $history="$target.pcl-helper.history"
 if(-not(Get-ChildItem -LiteralPath $history -Filter '*.bak'|Where-Object {(Read-Hash $_.FullName) -eq $backupHash})){throw 'Previous version backup lost'}
 if(-not(Get-ChildItem -LiteralPath $history -Filter '*.json'|Where-Object {(Read-Hash $_.FullName) -eq $recordHash})){throw 'Previous version record lost'}
 $patchedHash=Read-Hash $target
 Invoke-Helper $helper '--enable' $target
 Assert-Hash $target $patchedHash
 Invoke-Helper $helper '--restore' $target
 Assert-Hash $target $newHash
 Write-Output "PASS ${label}: same-path 2.99.0.0 -> 2.99.0.1, old backup preserved, new patch/restore/repeat operations."

 Copy-TestFile $old $target
 Invoke-Helper $helper '--enable' $target
 Invoke-Helper $helper '--restore' $target
 Assert-Hash $target $oldHash
 Write-Output "PASS ${label}: downgrade at same path and restore 2.99.0.0."
}
$fresh=Join-Path $output 'fresh-original';New-Item -ItemType Directory -Force $fresh|Out-Null
$target=Join-Path $fresh 'PCL.exe';Copy-TestFile $old $target
Invoke-Helper $helper '--restore' $target
Assert-Hash $target $oldHash
if(Test-Path "$target.pcl-helper.bak"){throw 'No-op restore created a backup'}
Invoke-Helper $helper '--enable' $target
$stream=[IO.File]::Open($target,[IO.FileMode]::Append);try{$stream.WriteByte(0)}finally{$stream.Dispose()}
$changed=Read-Hash $target
Invoke-Helper $helper '--restore' $target 1
Assert-Hash $target $changed
Write-Output 'PASS: unpatched original restore is a no-op; externally changed patched file is not overwritten.'
Write-Output "Evidence: $output"
