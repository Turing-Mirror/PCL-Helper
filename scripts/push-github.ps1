param(
 [string]$Remote='origin',
 [string]$Branch='main',
 [string]$SourceEmail=(git config --local user.email),
 [string]$TargetName=(git config --global user.name),
 [string]$TargetEmail=(git config --global user.email),
 [switch]$NoPush
)
# Local history and CNB keep the CNB identity; GitHub receives a rewritten copy.
# The rewrite is deterministic, so repeated runs produce the same commits for unchanged history.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$url=(git -C $root remote get-url $Remote).Trim()
$work=Join-Path ([IO.Path]::GetTempPath()) ('pcl-helper-github-'+[Guid]::NewGuid().ToString('N'))
function Invoke-Git([string[]]$Arguments){& git @Arguments;if($LASTEXITCODE -ne 0){throw "git $($Arguments -join ' ') failed"}}
try {
 Invoke-Git @('clone','--quiet','--no-local','--branch',$Branch,$root,$work)
 Push-Location $work
 try {
  Invoke-Git @('fetch','--quiet','origin','+refs/tags/*:refs/tags/*')
  $env:FILTER_BRANCH_SQUELCH_WARNING='1'
  $env:PH_SOURCE=$SourceEmail;$env:PH_NAME=$TargetName;$env:PH_EMAIL=$TargetEmail
  $filter='if [ "$GIT_AUTHOR_EMAIL" = "$PH_SOURCE" ]; then GIT_AUTHOR_NAME="$PH_NAME"; GIT_AUTHOR_EMAIL="$PH_EMAIL"; fi; if [ "$GIT_COMMITTER_EMAIL" = "$PH_SOURCE" ]; then GIT_COMMITTER_NAME="$PH_NAME"; GIT_COMMITTER_EMAIL="$PH_EMAIL"; fi'
  Invoke-Git @('filter-branch','-f','--env-filter',$filter,'--tag-name-filter','cat','--',$Branch,'--tags')
  # Re-create annotated tags with the target tagger, keeping the original date and message.
  foreach($tag in (git for-each-ref refs/tags --format='%(refname:short) %(objecttype)')){
   $name,$type=$tag -split ' '
   if($type -ne 'tag'){continue}
   $raw=git cat-file tag $name
   $tagger=($raw|Where-Object {$_ -like 'tagger *'}|Select-Object -First 1)
   if($tagger -notlike "*<$SourceEmail>*"){continue}
   $date=($tagger -split '> ',2)[1]
   $message=(git for-each-ref "refs/tags/$name" --format='%(contents)') -join "`n"
   $env:GIT_COMMITTER_NAME=$TargetName;$env:GIT_COMMITTER_EMAIL=$TargetEmail;$env:GIT_COMMITTER_DATE=$date
   Invoke-Git @('tag','-f','-a',$name,"$name^{commit}",'-m',$message.TrimEnd())
  }
  Remove-Item Env:GIT_COMMITTER_NAME,Env:GIT_COMMITTER_EMAIL,Env:GIT_COMMITTER_DATE -ErrorAction SilentlyContinue
  $left=git log $Branch --tags --format='%ae%n%ce' | Where-Object {$_ -eq $SourceEmail}
  if($left){throw 'Source identity remains after rewrite'}
  if($NoPush){git log $Branch --format='%h %an <%ae> | %cn <%ce> | %s';git for-each-ref refs/tags --format='%(refname:short) %(objectname:short) %(*objectname:short) %(taggeremail)';return}
  Invoke-Git @('push','--force',$url,"refs/heads/${Branch}:refs/heads/$Branch")
  Invoke-Git @('push','--force',$url,'refs/tags/*:refs/tags/*')
  git ls-remote $url
 } finally {Pop-Location}
} finally {
 Remove-Item Env:FILTER_BRANCH_SQUELCH_WARNING,Env:PH_SOURCE,Env:PH_NAME,Env:PH_EMAIL -ErrorAction SilentlyContinue
 if(Test-Path $work){Remove-Item -Recurse -Force $work}
}
