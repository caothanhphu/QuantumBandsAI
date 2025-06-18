# Script to add UsersTestDataBuilder using statements to test files
Write-Host "Adding UsersTestDataBuilder using statements..."

# Files to update
$files = @(
    "QuantumBands.Tests/Services/AuthServiceTests.cs",
    "QuantumBands.Tests/Controllers/UsersControllerTests.cs", 
    "QuantumBands.Tests/Controllers/AuthControllerTests.cs"
)

foreach ($file in $files) {
    Write-Host "Processing $file..."
    $content = Get-Content $file -Raw
    
    # Check if it already has the using statement
    if ($content -notmatch "using static.*UsersTestDataBuilder") {
        # Find the line with AuthenticationTestDataBuilder static import and add after it
        if ($content -match "using static.*AuthenticationTestDataBuilder;") {
            $content = $content -replace "(using static.*AuthenticationTestDataBuilder;)", "`$1`r`nusing static QuantumBands.Tests.Fixtures.UsersTestDataBuilder;"
        } else {
            # If no AuthenticationTestDataBuilder import, add after the last using statement
            $content = $content -replace "(using.*Fixtures;)", "`$1`r`nusing static QuantumBands.Tests.Fixtures.UsersTestDataBuilder;"
        }
        
        Set-Content $file $content
        Write-Host "Added using statement to $file"
    } else {
        Write-Host "Already has using statement in $file"
    }
}

Write-Host "All using statements added successfully!" 