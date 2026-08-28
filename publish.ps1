<#
.SYNOPSIS
    Builds a Gloss release on this machine.

.DESCRIPTION
    There is no CI. The sync makes 123 requests to a volunteer-run API and only
    changes meaningfully when CIG patch, so a release is cut by hand when a
    patch lands rather than on every commit.

    What comes out is the tool and the facts - never a built global.ini. That
    file is Cloud Imperium's text with our annotations, and it cannot layer over
    another text mod: only one loose table wins, so a published one would force
    everybody to choose between Gloss and StarStrings. Building on the user's
    own machine avoids both problems. See NOTICE.

.PARAMETER Version
    Release version, e.g. 0.1.0. Written into the binary and the folder name.

.PARAMETER SkipSync
    Reuse the facts.json already here instead of fetching. For a rebuild that
    changes only the tool.

.EXAMPLE
    .\publish.ps1 -Version 0.1.0
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

Write-Host ''
Write-Host "  $zip" -ForegroundColor Green
Write-Host "  facts: $count items, fetched $built"
Write-Host ''
Write-Host 'Nothing has been uploaded. Attach the zip to a release when you are ready.'
