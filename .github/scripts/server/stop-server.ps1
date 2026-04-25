param()

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pidFile = Join-Path $scriptDir 'server.pid'

if (-not (Test-Path -LiteralPath $pidFile)) {
  Write-Error "No PID file found at $pidFile - server may not be running."
  exit 0
}

$serverPid = (Get-Content -LiteralPath $pidFile -Raw).Trim()

if ($serverPid -notmatch '^\d+$') {
  Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
  Write-Error "PID file at $pidFile did not contain a valid process id."
  exit 0
}

$process = Get-Process -Id ([int]$serverPid) -ErrorAction SilentlyContinue
if ($null -eq $process) {
  Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
  Write-Error "Process $serverPid is not running - cleaned up stale PID file."
  exit 0
}

Stop-Process -Id $process.Id -ErrorAction SilentlyContinue

for ($i = 0; $i -lt 50; $i++) {
  Start-Sleep -Milliseconds 100
  if ($null -eq (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
    break
  }
}

if ($null -ne (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
  Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}

Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
Write-Output 'Server stopped.'
