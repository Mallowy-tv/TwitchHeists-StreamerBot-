[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [Parameter(Mandatory = $true)]
  [string]$DestinationPath,

  [ValidateSet('Release', 'Debug')]
  [string]$Configuration = 'Release',

  [switch]$OverwriteConfig,

  [switch]$IncludeSymbols
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$buildOutput = Join-Path $repoRoot "src\TwitchHeists.StreamerBot.Bridge\bin\$Configuration\net48"
if (-not (Test-Path -LiteralPath $buildOutput)) {
  throw "Build output not found at $buildOutput. Run a $Configuration build first."
}

$requiredFiles = @(
  'TwitchHeists.StreamerBot.Bridge.dll',
  'TwitchHeists.StreamerBot.dll',
  'TwitchHeists.Core.dll',
  'TwitchHeists.Data.Sqlite.dll',
  'appsettings.json',
  'heist-messages.json'
)

foreach ($requiredFile in $requiredFiles) {
  $requiredPath = Join-Path $buildOutput $requiredFile
  if (-not (Test-Path -LiteralPath $requiredPath)) {
    throw "Required build artifact missing: $requiredPath"
  }
}

$resolvedDestination = [System.IO.Path]::GetFullPath($DestinationPath)
New-Item -ItemType Directory -Force -Path $resolvedDestination | Out-Null

$excludedNames = @('TwitchHeists.zip')
if (-not $IncludeSymbols) {
  $excludedNames += @(
    'TwitchHeists.Core.pdb',
    'TwitchHeists.Data.Sqlite.pdb',
    'TwitchHeists.StreamerBot.Bridge.pdb',
    'TwitchHeists.StreamerBot.pdb'
  )
}

if (-not $OverwriteConfig) {
  $excludedNames += @('appsettings.json', 'heist-messages.json')
}

$itemsToCopy = Get-ChildItem -LiteralPath $buildOutput -Force | Where-Object { $_.Name -notin $excludedNames }

foreach ($item in $itemsToCopy) {
  $destinationItem = Join-Path $resolvedDestination $item.Name

  if ($PSCmdlet.ShouldProcess($destinationItem, "Copy $($item.Name)")) {
    Copy-Item -LiteralPath $item.FullName -Destination $destinationItem -Recurse -Force
  }
}

if ($OverwriteConfig) {
  foreach ($configFile in @('appsettings.json', 'heist-messages.json')) {
    $sourceConfig = Join-Path $buildOutput $configFile
    $destinationConfig = Join-Path $resolvedDestination $configFile

    if ($PSCmdlet.ShouldProcess($destinationConfig, "Copy $configFile")) {
      Copy-Item -LiteralPath $sourceConfig -Destination $destinationConfig -Force
    }
  }
} else {
  foreach ($configFile in @('appsettings.json', 'heist-messages.json')) {
    $sourceConfig = Join-Path $buildOutput $configFile
    $destinationConfig = Join-Path $resolvedDestination $configFile

    if (-not (Test-Path -LiteralPath $destinationConfig)) {
      if ($PSCmdlet.ShouldProcess($destinationConfig, "Copy missing $configFile")) {
        Copy-Item -LiteralPath $sourceConfig -Destination $destinationConfig -Force
      }
    }
  }
}

Write-Output "Installed TwitchHeists files to $resolvedDestination"
if ($OverwriteConfig) {
  Write-Output 'Configuration files were overwritten.'
} else {
  Write-Output 'Existing appsettings.json and heist-messages.json were preserved.'
}
Write-Output 'Your data folder is not modified by this script.'
Write-Output 'TwitchHeists.txt is not copied by this script.'
