param (
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$csproj = Join-Path $projectDir "InfiniteStamina.csproj"
$outDir = Join-Path $projectDir "bin\$Configuration\netstandard2.1"
$distDir = Join-Path $projectDir "dist"
$zipPath = Join-Path $projectDir "RenUI.zip"

Write-Host "Building RenUI ($Configuration)..." -ForegroundColor Cyan
dotnet build -c $Configuration $csproj

if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir | Out-Null

Copy-Item (Join-Path $outDir "InfiniteStamina.dll") -Destination (Join-Path $distDir "InfiniteStamina.dll")
Copy-Item (Join-Path $projectDir "manifest.json") -Destination (Join-Path $distDir "manifest.json")
Copy-Item (Join-Path $projectDir "icon.png") -Destination (Join-Path $distDir "icon.png")
Copy-Item (Join-Path $projectDir "README.md") -Destination (Join-Path $distDir "README.md")

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$distDir\*" -DestinationPath $zipPath -Force

$customMenuZip = Join-Path $projectDir "CustomMenu.zip"
Copy-Item $zipPath -Destination $customMenuZip -Force

# Generate mm_v2_manifest.json for r2modman cache
$mmv2Content = '{"manifestVersion":1,"name":"Pattern-RenUI","authorName":"Pattern","websiteUrl":"","displayName":"RenUI","description":"RenUI by Pattern: Draggable Cyber-Modern menu with GodMode, Infinite Stamina, Battery, Anti-Conta, FullBright, Custom Allies & Enemies ESP, Item ESP, Item Spawner & Keybinds.","gameVersion":"","networkMode":"","packageType":"","installMode":"","installedAtTime":1794202040817,"loaders":[],"dependencies":["LavaGang-MelonLoader-0.7.3"],"incompatibilities":[],"optionalDependencies":[],"versionNumber":{"major":1,"minor":8,"patch":1},"enabled":true,"icon":"","onlineSource":false,"trustedPackage":false}'

# 1. Deploy to r2modman Profile Mods
$r2ModDir = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\profiles\Default\Mods\Pattern-RenUI"
if (-not (Test-Path $r2ModDir)) { New-Item -ItemType Directory -Path $r2ModDir -Force | Out-Null }
Copy-Item "$distDir\*" -Destination $r2ModDir -Recurse -Force

# 2. Deploy to r2modman Profile BepInEx plugins (where r2modman looks for local mod icons)
$bep1 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\profiles\Default\BepInEx\plugins\Pattern-RenUI"
if (-not (Test-Path $bep1)) { New-Item -ItemType Directory -Path $bep1 -Force | Out-Null }
Copy-Item (Join-Path $projectDir "icon.png") -Destination (Join-Path $bep1 "icon.png") -Force

$bep2 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\profiles\Default\BepInEx\plugins\Pattern-CustomMenu"
if (-not (Test-Path $bep2)) { New-Item -ItemType Directory -Path $bep2 -Force | Out-Null }
Copy-Item (Join-Path $projectDir "icon.png") -Destination (Join-Path $bep2 "icon.png") -Force

# 3. Deploy to r2modman Cache for Pattern-RenUI 1.8.1 and 1.8.0
$r2CacheDir181 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-RenUI\1.8.1"
if (-not (Test-Path $r2CacheDir181)) { New-Item -ItemType Directory -Path $r2CacheDir181 -Force | Out-Null }
Copy-Item "$distDir\*" -Destination $r2CacheDir181 -Recurse -Force
Set-Content -Path (Join-Path $r2CacheDir181 "mm_v2_manifest.json") -Value $mmv2Content -NoNewline

$r2CacheDir180 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-RenUI\1.8.0"
if (-not (Test-Path $r2CacheDir180)) { New-Item -ItemType Directory -Path $r2CacheDir180 -Force | Out-Null }
Copy-Item "$distDir\*" -Destination $r2CacheDir180 -Recurse -Force
Set-Content -Path (Join-Path $r2CacheDir180 "mm_v2_manifest.json") -Value $mmv2Content -NoNewline

# 4. Deploy fallback to r2modman Cache for Pattern-CustomMenu 1.7.0 and 1.5.1
$r2Custom17 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-CustomMenu\1.7.0"
if (-not (Test-Path $r2Custom17)) { New-Item -ItemType Directory -Path $r2Custom17 -Force | Out-Null }
Copy-Item "$distDir\*" -Destination $r2Custom17 -Recurse -Force
$mmv2Custom17 = '{"manifestVersion":1,"name":"Pattern-CustomMenu","authorName":"Pattern","websiteUrl":"","displayName":"CustomMenu","description":"RenUI / CustomMenu by Pattern","gameVersion":"","networkMode":"","packageType":"","installMode":"","installedAtTime":1794202040817,"loaders":[],"dependencies":["LavaGang-MelonLoader-0.7.3"],"incompatibilities":[],"optionalDependencies":[],"versionNumber":{"major":1,"minor":7,"patch":0},"enabled":true,"icon":"","onlineSource":false,"trustedPackage":false}'
Set-Content -Path (Join-Path $r2Custom17 "mm_v2_manifest.json") -Value $mmv2Custom17 -NoNewline

$r2Custom15 = "C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-CustomMenu\1.5.1"
if (Test-Path $r2Custom15) {
    Copy-Item (Join-Path $projectDir "icon.png") -Destination (Join-Path $r2Custom15 "icon.png") -Force
}

# 5. Deploy to Steam game folder
$steamModsDir = "C:\Program Files (x86)\Steam\steamapps\common\MIMESIS\Mods"
if (Test-Path $steamModsDir) {
    Copy-Item (Join-Path $outDir "InfiniteStamina.dll") -Destination (Join-Path $steamModsDir "InfiniteStamina.dll") -Force
}
$launcherExe = "C:\My_Games\Mimesis\RenUI.exe"
if (Test-Path $launcherExe) {
    Copy-Item $launcherExe -Destination "C:\Program Files (x86)\Steam\steamapps\common\MIMESIS\RenUI.exe" -Force
}

Write-Host "Package and Deployment complete: $zipPath and deployed to r2modman and Steam!" -ForegroundColor Green
