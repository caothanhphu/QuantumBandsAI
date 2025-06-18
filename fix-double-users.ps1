# Script to fix double Users references in test files
Write-Host "Fixing double Users references..."

# Files to update
$files = @(
    "QuantumBands.Tests/Services/AuthServiceTests.cs",
    "QuantumBands.Tests/Controllers/UsersControllerTests.cs", 
    "QuantumBands.Tests/Controllers/AuthControllerTests.cs"
)

foreach ($file in $files) {
    Write-Host "Processing $file..."
    
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        # Check if file contains the double reference
        if ($content -match 'UsersUsersTestDataBuilder') {
            Write-Host "Found double Users reference in $file, fixing..."
            
            # Fix double Users in class names
            $content = $content -replace 'UsersUsersTestDataBuilder', 'UsersTestDataBuilder'
            
            Set-Content $file $content
            Write-Host "Fixed $file"
        } else {
            Write-Host "No double Users reference found in $file"
        }
    } else {
        Write-Host "File not found: $file"
    }
}

Write-Host "All double Users references fixed!" 