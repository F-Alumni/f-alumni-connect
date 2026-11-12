$ErrorActionPreference = 'Stop'
$chrome = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
if (-not (Test-Path -LiteralPath $chrome)) {
    $chrome = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
}
if (-not (Test-Path -LiteralPath $chrome)) {
    throw 'Install Chrome or Edge to render PNG previews.'
}
$diagramProfile = Join-Path $env:TEMP 'falumni-diagram-renderer'
Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.svg' | ForEach-Object {
    $svg = $_
    $xml = [xml](Get-Content -LiteralPath $svg.FullName -Raw)
    $diagramWidth = $xml.DocumentElement.width
    $diagramHeight = $xml.DocumentElement.height
    $uri = ([System.Uri]$svg.FullName).AbsoluteUri
    $png = [System.IO.Path]::ChangeExtension($svg.FullName, '.png')
    & $chrome --headless --disable-gpu --no-sandbox --hide-scrollbars `
        "--user-data-dir=$diagramProfile" "--screenshot=$png" `
        "--window-size=$diagramWidth,$diagramHeight" $uri 2>&1 | Select-Object -Last 1
    if (-not (Test-Path -LiteralPath $png)) { throw "Missing PNG: $png" }
    Write-Output "Rendered $($svg.Name)"
}
