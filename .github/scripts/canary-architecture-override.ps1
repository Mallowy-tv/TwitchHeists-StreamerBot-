param()

$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$overrideSkill = Join-Path $projectRoot '.mimir\skills\architecture\SKILL.md'
$overridePrompt = Join-Path $projectRoot '.mimir\skills\architecture\prompts\writing-adrs.md'
$coreSkill = Join-Path $projectRoot '.github\skills\architecture\SKILL.md'

if (-not (Test-Path -LiteralPath $overrideSkill)) {
  throw "Missing override skill: $overrideSkill"
}

if (-not (Test-Path -LiteralPath $overridePrompt)) {
  throw "Missing override prompt: $overridePrompt"
}

if (-not (Test-Path -LiteralPath $coreSkill)) {
  throw "Missing core skill: $coreSkill"
}

$overrideContent = Get-Content -LiteralPath $overrideSkill -Raw
$promptContent = Get-Content -LiteralPath $overridePrompt -Raw
$coreContent = Get-Content -LiteralPath $coreSkill -Raw

if ($overrideContent -notmatch 'Canary marker:\s*`architecture-project-replace-active`') {
  throw 'Override skill canary marker is missing.'
}

if ($promptContent -notmatch 'Canary marker:\s*`architecture-override-adr-prompt-active`') {
  throw 'Override ADR prompt canary marker is missing.'
}

if ($overrideContent -notmatch '(?m)^\s*name:\s*architecture\s*$') {
  throw 'Override skill frontmatter name must be architecture.'
}

if ($promptContent -notmatch '(?m)^\s*parent:\s*architecture\s*$') {
  throw 'Override ADR prompt parent must be architecture.'
}

if ($overrideContent -notmatch 'prompts/writing-adrs\.md') {
  throw 'Override skill must reference prompts/writing-adrs.md.'
}

if ($coreContent -eq $overrideContent) {
  throw 'Override skill should be distinct from the core architecture skill.'
}

$result = [ordered]@{
  skill_name = 'architecture'
  resolution_mode = 'project_replace'
  resolved_from = '.mimir\skills\architecture\SKILL.md'
  prompt_resolved_from = '.mimir\skills\architecture\prompts\writing-adrs.md'
  status = 'pass'
}

$result | ConvertTo-Json -Compress
