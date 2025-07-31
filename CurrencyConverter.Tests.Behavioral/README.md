# Behavioral Tests for Currency Converter API

This project contains behavioral tests that verify the API's behavior from a user's perspective, following Given-When-Then patterns.

## What are Behavioral Tests?

Behavioral tests focus on **what** the system does rather than **how** it does it. They test complete user scenarios and workflows to ensure the API behaves correctly under various conditions.

## Structure

### Base Classes
- `BaseBehavioralSpecification`: Common functionality for all behavioral tests
- `TestDataBuilder`: Fluent API for creating test data
- `CurrencyConversionRequestBuilder`: Specific builder for conversion requests

### Test Organization
Tests are organized by feature/functionality:
- `CurrencyConversionSpecifications`: Currency conversion behaviors
- `ExchangeRatesSpecifications`: Exchange rate retrieval behaviors
- `GetLatestExchangeRateSpecification`: Latest rates specific behaviors
- `GetHistoricalExchangeRatesSpecification`: Historical rates specific behaviors

## Given-When-Then Pattern

Each test follows the behavioral pattern:

```csharp
[Fact(DisplayName = "Given valid request, when converting currency, then should return converted amount")]
public async Task ConvertCurrency_ShouldReturnConvertedAmount_WhenValidRequest()
{
    // Given - Set up the test context
    await GivenUserHasConversionPermissions();
    var request = GivenConversionRequest("EUR", "USD", 100);

    // When - Execute the action being tested
    var response = await WhenRequestingCurrencyConversion(request);

    // Then - Verify the expected outcome
    await ThenShouldReturnSuccessfulConversion(response, "EUR", "USD", 100, 125);
}
```

## Test Categories

### 1. Success Scenarios
Test that the happy path works correctly:
- Valid inputs return expected outputs
- Authenticated users can access resources
- Data is processed correctly

### 2. Authorization & Authentication
Test security behaviors:
- Unauthorized access is blocked
- Missing permissions return 403 Forbidden
- Invalid tokens return 401 Unauthorized

### 3. Validation Scenarios
Test input validation:
- Invalid inputs return 400 Bad Request
- Missing required fields are caught
- Business rule violations are detected

### 4. External Service Integration
Test integration with external services:
- Service unavailability is handled gracefully
- Invalid responses are managed
- Timeouts and retries work correctly

### 5. Error Handling
Test error scenarios:
- Meaningful error messages are returned
- Appropriate status codes are used
- System remains stable during errors

## Writing New Tests

### 1. Inherit from BaseBehavioralSpecification
```csharp
public class NewFeatureSpecifications : BaseBehavioralSpecification
{
    public NewFeatureSpecifications(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }
}
```

### 2. Use Descriptive Test Names
Follow the pattern: `Given_When_Then` or use DisplayName attribute:
```csharp
[Fact(DisplayName = "Given invalid input, when processing request, then should return validation error")]
public async Task ProcessRequest_ShouldReturnValidationError_WhenInvalidInput()
```

### 3. Organize with Regions
Group related tests and methods:
```csharp
#region Success Scenarios
// Happy path tests here
#endregion

#region Validation Scenarios
// Input validation tests here
#endregion

#region Given Methods (Setup)
// Test setup methods here
#endregion

#region When Methods (Actions)
// Action methods here
#endregion

#region Then Methods (Assertions)
// Assertion methods here
#endregion
```

### 4. Use FluentAssertions
Prefer FluentAssertions for better readability:
```csharp
// Good
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Amount.Should().Be(100);
result.ConvertedAmount.Should().BeGreaterThan(0);

// Avoid
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.Equal(100, result.Amount);
Assert.True(result.ConvertedAmount > 0);
```

### 5. Create Test Data Builders
Use builders for complex test data:
```csharp
var request = TestDataBuilder.ConversionRequest()
    .From("EUR")
    .To("USD")
    .WithAmount(100)
    .UsingProvider("frankfurter")
    .Build();
```

## Running Tests

### Command Line
```bash
# Run all behavioral tests
dotnet test --filter "Category=Behavioral"

# Run specific test class
dotnet test --filter "FullyQualifiedName~CurrencyConversionSpecifications"

# Run with specific output
dotnet test --logger "console;verbosity=detailed"
```

### Test Explorer
Tests are organized by traits and can be filtered by:
- Test name
- Category
- DisplayName

## Best Practices

### DO:
- Write tests that read like specifications
- Focus on user scenarios, not implementation details
- Use meaningful test names and descriptions
- Group related assertions
- Test both success and failure scenarios
- Use the Given-When-Then pattern consistently
- Mock external dependencies

### DON'T:
- Test implementation details
- Create tests that are brittle to refactoring
- Use magic numbers or strings without explanation
- Write tests that depend on other tests
- Skip testing error scenarios
- Mix unit testing concerns with behavioral testing

## Test Configuration

The project includes optimized xUnit configuration in `xunit.runner.json`:
- Parallel test execution enabled
- Detailed method display
- Optimized for CI/CD pipelines

## Troubleshooting

### Common Issues

1. **Tests fail randomly**: Check for shared state between tests
2. **Slow test execution**: Verify parallel execution is enabled
3. **Authentication issues**: Ensure test tokens are properly configured
4. **Mock setup problems**: Verify mocks are reset between tests

### Debugging Tests
- Use the Test Explorer debugger
- Add breakpoints in Given-When-Then methods
- Check the test output for detailed error information
- Verify test data setup is correct

## Contributing

When adding new behavioral tests:
1. Follow the established patterns
2. Add comprehensive test coverage for new features
3. Update this README if adding new patterns or conventions
4. Ensure tests are reliable and fast
5. Add appropriate test documentation