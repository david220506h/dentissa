# Levanta la DEMO de Dentissa con un comando (docs/DEMO.md). Solo datos ficticios; todo en 127.0.0.1.
#   powershell -ExecutionPolicy Bypass -File demo\demo.ps1
# Requiere Docker Desktop abierto. Para empezar de cero: docker compose -f docker-compose.demo.yml --env-file .env.demo down -v
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

docker info --format '{{.ServerVersion}}' *> $null
if ($LASTEXITCODE -ne 0) { Write-Host 'Docker no está corriendo. Abre Docker Desktop y vuelve a intentarlo.'; exit 1 }

# .env.demo con valores al azar (no se versiona)
if (-not (Test-Path '.env.demo')) {
  function Get-Azar([int]$bytes) {
    $b = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
    return ([Convert]::ToBase64String($b) -replace '[+/=]', '')
  }
  $texto = (Get-Content '.env.demo.example' -Raw) `
    -replace '(?m)^POSTGRES_PASSWORD=\s*$', ('POSTGRES_PASSWORD=' + (Get-Azar 24)) `
    -replace '(?m)^JWT_SECRET=\s*$', ('JWT_SECRET=' + (Get-Azar 48))
  [System.IO.File]::WriteAllText((Join-Path (Get-Location) '.env.demo'), $texto)
  Write-Host 'Se creó .env.demo con valores al azar.'
}

$compose = @('compose', '-f', 'docker-compose.demo.yml', '--env-file', '.env.demo')
& docker @compose up -d --build --wait
if ($LASTEXITCODE -ne 0) { Write-Host 'No se pudo levantar la demo (revisa: docker compose -f docker-compose.demo.yml logs).'; exit 1 }

& docker @compose run --rm sembrar
if ($LASTEXITCODE -eq 3) { Write-Host 'La base ya tenía datos: se conservan (las cuentas se mostraron al sembrar la primera vez).' }
elseif ($LASTEXITCODE -ne 0) { Write-Host 'Falló el sembrado.'; exit 1 }

$panel = (Select-String -Path '.env.demo' -Pattern '^PANEL_PUERTO=(\d+)').Matches.Groups[1].Value
$correo = (Select-String -Path '.env.demo' -Pattern '^MAILPIT_PUERTO=(\d+)').Matches.Groups[1].Value
Write-Host ''
Write-Host "Panel:   http://127.0.0.1:$panel"
Write-Host "Correos: http://127.0.0.1:$correo"
