<#
.SYNOPSIS
    Builds a Gloss release on this machine.

.DESCRIPTION
    There is no CI. The sync makes 123 requests to a volunteer-run API and only
    changes meaningfully when CIG patch, so a release is cut by hand when a
    patch lands rather than on every commit.

    Two assets come out. The tool and facts.json are the ones to prefer: they
    build on whatever text mod the user already has, and pick up their own
    receipts.

    The drop-in global.ini is for people who will not run a command line. It is
    built with -from-game and without -sold, deliberately: whatever is installed
    on the machine cutting the release is that person's business, and their
    receipts would quietly say which items they had bought. It replaces any
    other text mod, and the release notes have to say so.

.PARAMETER Version
    Release version, e.g. 0.2.0. Written into the binary and the folder name.

.PARAMETER SkipSync
    Reuse the facts.json already here instead of fetching.

.EXAMPLE
    .\publish.ps1 -Version 0.2.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$SkipSync
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$out = Join-Path $PSScriptRoot "release\$Version"
$project = 'src\Gloss\Gloss.csproj'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null

Write-Host "Building $Version" -ForegroundColor Cyan

# Self-contained: whoever downloads this should not have to install a runtime
# before they can find out whether the idea is any good.
dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version -o $out --nologo | Out-Null

if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

Get-ChildItem $out -Include *.pdb -Recurse | Remove-Item -Force

if (-not $SkipSync) {
    Write-Host 'Fetching facts. This is the slow part.' -ForegroundColor Cyan
    & (Join-Path $out 'Gloss.exe') sync --facts (Join-Path $PSScriptRoot 'facts.json')
    if ($LASTEXITCODE -ne 0) { throw 'Sync failed; the release would ship stale facts.' }
}

if (-not (Test-Path 'facts.json')) { throw 'No facts.json, and -SkipSync was given.' }

Copy-Item 'facts.json' $out
foreach ($doc in 'README.md', 'CREDITS.md', 'LICENSE', 'NOTICE') { Copy-Item $doc $out }

# The facts go stale at every patch, so the release has to say which one they
# describe. Read it back out rather than trusting whoever ran this to remember.
$facts = Get-Content (Join-Path $out 'facts.json') -Raw | ConvertFrom-Json
$built = [datetimeoffset]::Parse($facts.builtAt).ToString('yyyy-MM-dd')
$count = ($facts.items.PSObject.Properties | Measure-Object).Count

$zip = Join-Path $PSScriptRoot "release\Gloss-$Version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$out\*" -DestinationPath $zip

# ---- the drop-in, for people who will not run a command line ----

Write-Host 'Building the drop-in table.' -ForegroundColor Cyan

$drop = Join-Path $PSScriptRoot "release\dropin-$Version"
if (Test-Path $drop) { Remove-Item $drop -Recurse -Force }
New-Item -ItemType Directory $drop | Out-Null

# --from-game and no --sold. Both matter: one keeps whatever is installed here
# out of the file, the other keeps this machine's purchase history out of it.
& (Join-Path $out 'Gloss.exe') build --facts (Join-Path $out 'facts.json') --out $drop --from-game
if ($LASTEXITCODE -ne 0) { throw 'Drop-in build failed.' }

$howTo = @"
Gloss $Version - drop-in table

Copy both files into your Star Citizen channel folder, keeping the layout:

  <StarCitizen>/LIVE/data/localization/english/global.ini
  <StarCitizen>/LIVE/user.cfg

Back up anything already at those paths first. Restart the game afterwards.

THIS REPLACES ANY OTHER TEXT MOD. Only one loose table can win, so if you use
StarStrings this file takes its place. To keep both, use the tool instead - it
builds on top of whatever you already have.

It also cannot know what you have bought. Running the tool yourself, with your
own kiosk receipts, marks fewer things wrongly.

To undo: delete global.ini, and user.cfg if you did not have one before.

Facts fetched $built, $count items.
"@

$howTo | Set-Content (Join-Path $drop 'HOW-TO.txt') -Encoding utf8
Copy-Item 'NOTICE' $drop
Copy-Item 'CREDITS.md' $drop

$dropZip = Join-Path $PSScriptRoot "release\Gloss-$Version-dropin.zip"
if (Test-Path $dropZip) { Remove-Item $dropZip -Force }
Compress-Archive -Path "$drop\*" -DestinationPath $dropZip

Write-Host ''
Write-Host "  $zip" -ForegroundColor Green
Write-Host "  $dropZip" -ForegroundColor Green
Write-Host "  facts: $count items, fetched $built"
Write-Host ''
Write-Host 'Nothing has been uploaded. Attach both to a release when you are ready.'
