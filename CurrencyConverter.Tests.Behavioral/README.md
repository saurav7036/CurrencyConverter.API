# Behavioral Tests Improvements

This document outlines the improvements made to the behavioral testing setup for the Currency Converter API.

## What was improved

### 1. Enhanced Package Dependencies
- Added **FluentAssertions** for better, more readable test assertions
- Updated **Microsoft.AspNetCore.Mvc.Testing** to version 8.0.17 for consistency
- Added global using statements for common test libraries

### 2. Better Test Structure
- Implemented clear **Given-When-Then** patterns
- Created `ImprovedCurrencyConversionSpecifications.cs` as an example of best practices
- Organized test methods into logical regions (Given, When, Then)

### 3. Improved Assertions
Before (basic xUnit assertions):
```csharp
Assert.NotNull(result);
Assert.Equal("EUR", result!.BaseCurrency);
Assert.Equal("USD", result.TargetCurrency);
```

After (FluentAssertions):
```csharp
result.Should().NotBeNull(because: "response should contain conversion result");
result!.BaseCurrency.Should().Be(expectedFromCurrency, 
    because: "the base currency should match the request");
result.TargetCurrency.Should().Be(expectedToCurrency, 
    because: "the target currency should match the request");
```

### 4. Better Test Names
- Descriptive test names using `DisplayName` attribute
- Clear Given-When-Then format in test names
- Meaningful parameter descriptions in Theory tests

## Benefits

1. **Better Readability**: Tests read like specifications
2. **Clearer Error Messages**: FluentAssertions provides context when tests fail
3. **Maintainable**: Clear separation of concerns with Given-When-Then methods
4. **Self-Documenting**: Tests serve as living documentation

## Running the Tests

```bash
# Run all behavioral tests
dotnet test CurrencyConverter.Tests.Behavioral

# Run with detailed output
dotnet test CurrencyConverter.Tests.Behavioral --logger "console;verbosity=detailed"
```

## Example Test Structure

```csharp
[Fact(DisplayName = "Given valid request, when converting currency, then should return converted amount")]
public async Task ConvertCurrency_ShouldReturnConvertedAmount_WhenValidRequest()
{
    // Given - Set up test context
    await GivenUserHasConversionPermissions();
    var request = GivenConversionRequest("EUR", "USD", 100);

    // When - Execute the action
    var response = await WhenRequestingCurrencyConversion(request);

    // Then - Verify the outcome
    await ThenShouldReturnSuccessfulConversion(response, "EUR", "USD", 100, 125);
}
```

This approach makes tests more maintainable and easier to understand for both developers and stakeholders.