# Script to update Users references from TestDataBuilder to UsersTestDataBuilder
Write-Host "Updating Users references in test files..."

# Files to update
$files = @(
    "QuantumBands.Tests/Services/AuthServiceTests.cs",
    "QuantumBands.Tests/Controllers/UsersControllerTests.cs", 
    "QuantumBands.Tests/Controllers/AuthControllerTests.cs"
)

foreach ($file in $files) {
    Write-Host "Processing $file..."
    $content = Get-Content $file -Raw
    
    # Update Users classes references
    $content = $content -replace 'TestDataBuilder\.Users\.', 'UsersTestDataBuilder.Users.'
    $content = $content -replace 'TestDataBuilder\.UserDtos\.', 'UsersTestDataBuilder.UserDtos.'
    $content = $content -replace 'TestDataBuilder\.UserProfile\.', 'UsersTestDataBuilder.UserProfile.'
    $content = $content -replace 'TestDataBuilder\.UpdateUserProfile\.', 'UsersTestDataBuilder.UpdateUserProfile.'
    $content = $content -replace 'TestDataBuilder\.ChangePassword\.', 'UsersTestDataBuilder.ChangePassword.'
    $content = $content -replace 'TestDataBuilder\.UserProfileUsers\.', 'UsersTestDataBuilder.UserProfileUsers.'
    $content = $content -replace 'TestDataBuilder\.VerifyEmailUsers\.', 'UsersTestDataBuilder.VerifyEmailUsers.'
    
    Set-Content $file $content
    Write-Host "Updated $file"
}

Write-Host "All references updated successfully!" 