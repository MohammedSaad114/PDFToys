$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\PDFToys.App\PDFToys.App.csproj"
$output = Join-Path $root "artifacts\publish\win-x64"

dotnet publish $project `
    -c Release `
    -p:PublishProfile=win-x64-selfcontained `
    -o $output

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Published to $output"

# Release signing placeholder (requires a code signing certificate):
# signtool sign /fd SHA256 /a "$output\PDFToys.App.exe"
