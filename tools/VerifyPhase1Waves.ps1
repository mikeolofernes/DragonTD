param(
    [switch]$RequireGeneratedAssets
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$sceneBootstrapperPath = Join-Path $repoRoot "Unity/Assets/Editor/SceneBootstrapper.cs"
$gameManagerPath = Join-Path $repoRoot "Unity/Assets/Scripts/Core/GameManager.cs"
$battleScenePath = Join-Path $repoRoot "Unity/Assets/Scenes/BattleScene.unity"
$wavesDir = Join-Path $repoRoot "Unity/Assets/ScriptableObjects/Waves"

$sceneBootstrapper = Get-Content $sceneBootstrapperPath -Raw
$gameManager = Get-Content $gameManagerPath -Raw

$expectedWaves = 1..5 | ForEach-Object { "Wave{0:D2}" -f $_ }
$failures = New-Object System.Collections.Generic.List[string]

foreach ($wave in $expectedWaves) {
    if ($sceneBootstrapper -notmatch "CreateWave\(`"$wave`"") {
        $failures.Add("SceneBootstrapper does not create $wave.")
    }

    if ($sceneBootstrapper -notmatch "Waves/$wave\.asset") {
        $failures.Add("SceneBootstrapper does not wire $wave into WaveManager.")
    }

    if ($RequireGeneratedAssets) {
        $assetPath = Join-Path $wavesDir "$wave.asset"
        if (-not (Test-Path $assetPath)) {
            $failures.Add("Generated asset missing: $assetPath")
        }
    }
}

if ($gameManager -match "all 3 waves") {
    $failures.Add("GameManager victory message still says all 3 waves.")
}

if ($RequireGeneratedAssets -and (Test-Path $battleScenePath)) {
    $scene = Get-Content $battleScenePath
    $wavesLine = -1
    for ($i = 0; $i -lt $scene.Count; $i++) {
        if ($scene[$i] -match "^\s*_waves:\s*$") {
            $wavesLine = $i
            break
        }
    }

    if ($wavesLine -lt 0) {
        $failures.Add("BattleScene does not contain a serialized _waves list.")
    }
    else {
        $wiredWaveCount = 0
        for ($i = $wavesLine + 1; $i -lt $scene.Count; $i++) {
            if ($scene[$i] -match "^\s*_[A-Za-z]") {
                break
            }

            if ($scene[$i] -match "^\s*-\s+\{fileID:") {
                $wiredWaveCount++
            }
        }

        if ($wiredWaveCount -ne 5) {
            $failures.Add("BattleScene wires $wiredWaveCount waves; expected 5.")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Phase 1 wave configuration verified for 5 waves."
