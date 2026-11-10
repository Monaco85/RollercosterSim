# Rechnet die Telemetrie-Signatur mit SimHubs eigener Bibliothek aus. So stimmt sie auch nach SimHub-Updates.
param([Parameter(Mandatory=$true)][string]$Definition)
$ErrorActionPreference = 'Stop'
$candidates = @($env:SIMHUB_INSTALL_PATH, "${env:ProgramFiles(x86)}\SimHub", "$env:ProgramFiles\SimHub")
$running = Get-Process SimHubWPF -ErrorAction SilentlyContinue | Select-Object -First 1
if ($running) { try { $candidates = @((Split-Path $running.Path)) + $candidates } catch {} }
$dir = $candidates | Where-Object { $_ -and (Test-Path (Join-Path $_ 'SimHub.Plugins.dll')) } | Select-Object -First 1
if (-not $dir) { throw 'SimHub nicht gefunden' }
[void][Reflection.Assembly]::LoadFrom((Join-Path $dir 'Newtonsoft.Json.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $dir 'SimHub.Plugins.dll'))
$in = Get-Content -LiteralPath $Definition -Raw -Encoding UTF8 | ConvertFrom-Json
$m = New-Object SimHub.Plugins.ExternalSims.ExternalSimManifest
$m.Name = $in.Name
$m.UniqueId = [guid]$in.UniqueId
$m.DefaultUDPPort = $in.DefaultUDPPort
$m.TelemetryBehaviour.GameFamily = $in.TelemetryBehaviour.GameFamily
$m.TelemetryBehaviour.DefaultMaxSpeed = $in.TelemetryBehaviour.DefaultMaxSpeed
$m.TelemetryBehaviour.HasRumbleStripsContactSurface = $false
foreach ($f in $in.TelemetryDefinition.Fields) {
  if ($f.TypeName -eq 'StandardField') {
    $t = New-Object SimHub.Plugins.ExternalSims.Fields.StandardFields.StandardField
    $t.FieldType = [Enum]::Parse([SimHub.Plugins.ExternalSims.Fields.StandardFields.StandardFieldType], $f.FieldType)
  } else {
    $t = New-Object SimHub.Plugins.ExternalSims.Fields.CustomFields.CustomTelemetryField
    $t.Name = $f.Name; $t.Comment = $f.Comment; $t.BinaryType = $f.BinaryType
  }
  $m.TelemetryDefinition.Fields.Add($t)
}
foreach ($p in $in.DetectionProcesses) {
  $d = New-Object SimHub.Plugins.ExternalSims.ExternalSimProcessDetection
  $d.ProcessName = $p.ProcessName
  $m.DetectionProcesses.Add($d)
}
$m.SaveToFile($Definition)
Write-Output ('SIG {0} {1} {2}' -f $m.GameSignature, $m.TelemetrySignature, $m.PacketLength)
