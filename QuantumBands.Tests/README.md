# QuantumBands API - Unit Tests

## Overview

This project contains comprehensive unit tests for the QuantumBands API, specifically focusing on the User Registration endpoint (`POST /api/v1/auth/register`).

## Test Structure

### Project Organization

```
QuantumBands.Tests/
├── Common/
│   └── TestBase.cs                     # Base class with common test setup
├── Controllers/
│   └── AuthControllerTests.cs          # Controller layer tests
├── Services/
│   └── AuthServiceTests.cs             # Service layer tests
├── Validators/
│   └── RegisterUserCommandValidatorTests.cs # FluentValidation tests
├── Fixtures/
│   └── TestDataBuilder.cs              # Test data generation
└── QuantumBands.Tests.csproj           # Project file
```

### Test Framework & Libraries

- **xUnit** - Primary testing framework
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - Fluent assertion library
- **AutoFixture** - Test data generation
- **FluentValidation.TestHelper** - Testing FluentValidation rules

## Test Coverage

### ✅ Happy Path Tests
- Valid registration with all required fields
- Registration with optional fullName
- Email verification token generation
- Wallet creation for new user
- Return UserDto with correct data

### ❌ Validation Tests
- Username too short (< 3 chars)
- Username too long (> 50 chars)
- Username with invalid characters
- Invalid email format
- Email too long (> 255 chars)
- Weak password (< 8 chars)
- Password without special characters
- Full name too long (> 200 chars)

### 🔧 Business Logic Tests
- Duplicate username registration
- Duplicate email registration
- Database transaction rollback on failure

### 🔗 Integration Tests
- Email service called with correct parameters
- User entity saved to database
- Associated wallet created
- Verification token stored

### 🛡️ Security Tests
- Password not exposed in responses
- Sensitive information not logged
- Proper error handling

## Running Tests

### Prerequisites

1. .NET 8.0 SDK
2. Visual Studio 2022 or VS Code
3. QuantumBands solution built successfully

### Run All Tests

```bash
# From solution root
dotnet test QuantumBands.Tests/

# With coverage
dotnet test QuantumBands.Tests/ --collect:"XPlat Code Coverage"

# With detailed output
dotnet test QuantumBands.Tests/ --logger "console;verbosity=detailed"
```

### Run Specific Test Categories

```bash
# Controller tests only
dotnet test QuantumBands.Tests/ --filter "FullyQualifiedName~AuthControllerTests"

# Service tests only
dotnet test QuantumBands.Tests/ --filter "FullyQualifiedName~AuthServiceTests"

# Validation tests only
dotnet test QuantumBands.Tests/ --filter "FullyQualifiedName~RegisterUserCommandValidatorTests"
```

### Run Tests by Category

```bash
# Happy path tests
dotnet test QuantumBands.Tests/ --filter "Category=HappyPath"

# Validation tests
dotnet test QuantumBands.Tests/ --filter "Category=Validation"

# Business logic tests
dotnet test QuantumBands.Tests/ --filter "Category=BusinessLogic"
```

## Test Data

### TestDataBuilder

The `TestDataBuilder` class provides pre-configured test data for various scenarios:

```csharp
// Valid registration command
var validCommand = TestDataBuilder.RegisterUser.ValidCommand();

// Invalid username scenarios
var shortUsername = TestDataBuilder.RegisterUser.CommandWithShortUsername();
var longUsername = TestDataBuilder.RegisterUser.CommandWithLongUsername();

// User entities
var validUser = TestDataBuilder.Users.ValidUser();
var existingUser = TestDataBuilder.Users.ExistingUserWithSameUsername();
```

## Mock Configuration

### Common Mocks

All tests inherit from `TestBase` which provides pre-configured mocks:

- `MockUnitOfWork` - Database operations
- `MockEmailService` - Email sending
- `MockJwtTokenGenerator` - JWT token generation
- `MockConfiguration` - Application configuration
- `MockCachingService` - Caching operations

### Mock Setup Examples

```csharp
// Setup successful user creation
_mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>()))
    .ReturnsAsync(expectedUser);

// Setup duplicate username check
_mockUserRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
    .ReturnsAsync(existingUser);

// Verify service calls
_mockEmailService.Verify(
    x => x.SendEmailAsync(
        command.Email,
        "Verify Your Email Address",
        It.IsAny<string>()),
    Times.Once);
```

## Test Scenarios Coverage

### Controller Tests (AuthControllerTests)

| Test Category | Tests Count | Description |
|---------------|-------------|-------------|
| Happy Path | 3 | Valid requests return correct responses |
| Validation Errors | 8 | Invalid model state handling |
| Business Logic Errors | 3 | Duplicate data and service errors |
| Exception Handling | 2 | Unexpected exceptions handling |
| Integration | 3 | End-to-end flow verification |
| Data Validation | 2 | Input sanitization and validation |
| Security | 2 | Sensitive data protection |

### Service Tests (AuthServiceTests)

| Test Category | Tests Count | Description |
|---------------|-------------|-------------|
| Happy Path | 4 | Successful user registration flow |
| Validation | 8 | Input validation scenarios |
| Business Logic | 3 | Duplicate checks and error handling |
| Integration | 4 | External service integration |

### Validator Tests (RegisterUserCommandValidatorTests)

| Test Category | Tests Count | Description |
|---------------|-------------|-------------|
| Username Validation | 8 | Length, format, and character validation |
| Email Validation | 8 | Format and length validation |
| Password Validation | 8 | Complexity requirements |
| FullName Validation | 5 | Optional field validation |
| Integration | 4 | Complete validation scenarios |
| Edge Cases | 2 | Special case handling |

## Quality Standards

### Code Coverage Target
- **Minimum**: 80% code coverage
- **Target**: 90%+ code coverage
- **Critical paths**: 100% coverage

### Test Pattern
All tests follow the **AAA pattern**:
- **Arrange**: Set up test data and mocks
- **Act**: Execute the method under test
- **Assert**: Verify expected behavior

### Naming Convention
```
[Method]_[Scenario]_[ExpectedBehavior]

Examples:
RegisterUserAsync_WithValidCommand_ShouldReturnSuccessResult
RegisterUserAsync_WithDuplicateUsername_ShouldReturnError
```

## Continuous Integration

### GitHub Actions Integration

```yaml
- name: Run Unit Tests
  run: dotnet test QuantumBands.Tests/ --configuration Release --logger trx --collect:"XPlat Code Coverage"

- name: Generate Coverage Report
  run: |
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

### Test Categories for CI

Tests can be categorized and run selectively:

```csharp
[Fact]
[Trait("Category", "Unit")]
[Trait("Layer", "Controller")]
public async Task Register_WithValidCommand_ShouldReturnCreatedResult()
```

## Troubleshooting

### Common Issues

1. **Mock Setup Issues**
   ```csharp
   // Ensure proper setup for async methods
   _mockService.Setup(x => x.MethodAsync(It.IsAny<Parameter>()))
       .ReturnsAsync(expectedResult);
   ```

2. **AutoFixture Circular References**
   ```csharp
   // Already handled in TestBase
   Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
   ```

3. **FluentValidation Testing**
   ```csharp
   // Use TestValidate for comprehensive validation testing
   var result = _validator.TestValidate(command);
   result.ShouldHaveValidationErrorFor(x => x.PropertyName);
   ```

### Debug Test Failures

```bash
# Run specific failing test with detailed output
dotnet test QuantumBands.Tests/ --filter "Method=RegisterUserAsync_WithValidCommand_ShouldReturnSuccessResult" --logger "console;verbosity=diagnostic"
```

## Contributing

### Adding New Tests

1. Follow existing naming conventions
2. Use TestDataBuilder for test data
3. Include proper documentation
4. Ensure AAA pattern compliance
5. Add appropriate test categories

### Test Review Checklist

- [ ] Test follows AAA pattern
- [ ] Proper mock verification
- [ ] Meaningful assertions
- [ ] Edge cases covered
- [ ] Error scenarios tested
- [ ] Documentation updated

## Related Documentation

- [QuantumBands API Documentation](../README.md)
- [Clean Architecture Patterns](../docs/architecture.md)
- [CQRS Implementation](../docs/cqrs.md)
- [FluentValidation Rules](../docs/validation.md) 