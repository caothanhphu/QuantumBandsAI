# Script to remove remaining Users classes from TestDataBuilder.cs
$filePath = "QuantumBands.Tests/Fixtures/TestDataBuilder.cs"
$content = Get-Content $filePath -Raw

Write-Host "Removing ChangePassword class..."
$content = $content -replace '(?s)    // SCRUM-42: Test data for change password endpoint testing\s*public static class ChangePassword\s*\{.*?\}', ''

Write-Host "Removing UserProfileUsers class..."
$content = $content -replace '(?s)    public static class UserProfileUsers\s*\{.*?\}', ''

Write-Host "Removing VerifyEmailUsers class..."
$content = $content -replace '(?s)    public static class VerifyEmailUsers\s*\{.*?\}', ''

# Clean up any extra whitespace
$content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"

Set-Content $filePath $content
Write-Host "All Users classes removed successfully!" 