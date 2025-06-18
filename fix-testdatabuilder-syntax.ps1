# Script to fix syntax errors in TestDataBuilder.cs after removing Users classes
$filePath = "QuantumBands.Tests/Fixtures/TestDataBuilder.cs"
$content = Get-Content $filePath -Raw

Write-Host "Fixing syntax errors..."

# Remove orphaned comments and syntax
$content = $content -replace '    // SCRUM-39: Test data for resend verification email endpoint testing\s*\n\s*,', ''
$content = $content -replace '\s*;\s*\n\s*public static User AlreadyVerifiedUser', "`r`n    public static class VerifyEmailUsers`r`n    {`r`n        public static User AlreadyVerifiedUser"

# Clean up any extra whitespace and fix class structure
$content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"

Set-Content $filePath $content
Write-Host "Syntax errors fixed!" 