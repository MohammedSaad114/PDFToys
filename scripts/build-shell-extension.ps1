$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\PDFToys.ShellExtension\PDFToys.ShellExtension.vcxproj"
$output = Join-Path $root "artifacts\shell-extension"

New-Item -ItemType Directory -Force -Path $output | Out-Null

msbuild $project /p:Configuration=Release /p:Platform=x64 /p:OutDir="$output\"

if ($LASTEXITCODE -ne 0)
{
    throw "Shell extension build failed with exit code $LASTEXITCODE."
}

Write-Host "Shell extension built to $output"
