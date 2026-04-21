$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$historyDir = "publish-history\$timestamp"

Write-Host "Publishing..."
dotnet publish src/Antinote/Antinote.csproj -c Release -o publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "Saving to $historyDir..."
    Copy-Item -Recurse publish $historyDir
    Write-Host "Done. History saved to $historyDir"
} else {
    Write-Host "Publish failed. History not saved."
}
