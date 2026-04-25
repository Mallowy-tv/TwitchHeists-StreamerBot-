param(
  [Parameter(Mandatory = $true)]
  [string]$ProjectDir
)

$ErrorActionPreference = 'Stop'

function Test-PortAvailable {
  param(
    [Parameter(Mandatory = $true)]
    [int]$Port
  )

  $listener = $null
  try {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    $listener.Start()
    return $true
  } catch {
    return $false
  } finally {
    if ($null -ne $listener) {
      $listener.Stop()
    }
  }
}

function Get-FreePort {
  param(
    [Parameter(Mandatory = $true)]
    [int]$StartingPort
  )

  for ($port = $StartingPort; $port -lt ($StartingPort + 50); $port++) {
    if (Test-PortAvailable -Port $port) {
      return $port
    }
  }

  throw "No free port found starting at $StartingPort"
}

$resolvedProjectDir = (Resolve-Path -LiteralPath $ProjectDir).Path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverJs = Join-Path $scriptDir 'server.js'
$uiHtml = Join-Path $scriptDir 'ui.html'
$pidFile = Join-Path $scriptDir 'server.pid'
$visualRoot = Join-Path $resolvedProjectDir '.github\.mimir-visual'
$legacyVisualRoot = Join-Path $resolvedProjectDir '.mimir-visual'
$screenDir = Join-Path $visualRoot 'screens'
$stateDir = Join-Path $visualRoot 'state'
$serverPort = Get-FreePort -StartingPort 3737

if (-not (Test-Path -LiteralPath $serverJs)) {
  throw "server.js not found at $serverJs"
}

if (-not (Test-Path -LiteralPath $uiHtml)) {
  throw "ui.html not found at $uiHtml"
}

if ((Test-Path -LiteralPath $legacyVisualRoot) -and (-not (Test-Path -LiteralPath $visualRoot))) {
  Move-Item -LiteralPath $legacyVisualRoot -Destination $visualRoot
}

New-Item -ItemType Directory -Force -Path $visualRoot | Out-Null
New-Item -ItemType Directory -Force -Path $screenDir, $stateDir | Out-Null

if (Test-Path -LiteralPath $pidFile) {
  $oldPid = (Get-Content -LiteralPath $pidFile -Raw).Trim()
  if ($oldPid -match '^\d+$') {
    $existingProcess = Get-Process -Id ([int]$oldPid) -ErrorAction SilentlyContinue
    if ($null -ne $existingProcess) {
      Stop-Process -Id $existingProcess.Id
      Start-Sleep -Milliseconds 500
    }
  }

  Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue
}

$startupFile = [System.IO.Path]::GetTempFileName()
$stderrFile = [System.IO.Path]::GetTempFileName()

try {
  $env:SCREEN_DIR = $screenDir
  $env:STATE_DIR = $stateDir
  $env:UI_HTML_PATH = $uiHtml
  $env:SERVER_PORT = $serverPort.ToString()

  $process = Start-Process -FilePath 'node' `
    -ArgumentList @("""$serverJs""") `
    -RedirectStandardOutput $startupFile `
    -RedirectStandardError $stderrFile `
    -PassThru `
    -WindowStyle Hidden

  Set-Content -LiteralPath $pidFile -Value $process.Id -NoNewline

  $serverUrl = "http://localhost:$serverPort"

  for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 100

    try {
      & node -e "const http=require('http'); const req=http.get(process.argv[1], function (r) { process.exit(r.statusCode === 200 ? 0 : 1); }); req.setTimeout(1000, function () { req.destroy(new Error('timeout')); }); req.on('error', function () { process.exit(1); });" "$serverUrl/health"
      if ($LASTEXITCODE -eq 0) {
        Write-Output (@{
          url = $serverUrl
          screen_dir = $screenDir
          state_dir = $stateDir
        } | ConvertTo-Json -Compress)
        return
      }
    } catch {
    }
  }

  $stderr = ''
  if ((Get-Item -LiteralPath $stderrFile).Length -gt 0) {
    $stderr = Get-Content -LiteralPath $stderrFile -Raw
  }

  Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $pidFile -Force -ErrorAction SilentlyContinue

  if ($stderr) {
    throw "Server failed to start within 3 seconds.`n$stderr"
  }

  throw 'Server failed to start within 3 seconds.'
} finally {
  Remove-Item -LiteralPath $startupFile, $stderrFile -Force -ErrorAction SilentlyContinue
}
