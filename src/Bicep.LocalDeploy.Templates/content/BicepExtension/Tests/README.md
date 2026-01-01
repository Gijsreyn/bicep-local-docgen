# Unit Tests

This directory contains unit tests for the Bicep Local Deploy extension.

## Overview

The tests follow the patterns recommended in the [Bicep Local Deploy Unit Testing Guide](https://github.com/Azure/bicep/blob/main/docs/experimental/local-deploy-dotnet-unittesting-guide.md).

## Test structure

- **Handlers/**: Tests for resource handlers
  - Uses mocking to isolate handler logic
  - Follows Arrange-Act-Assert pattern
  - Tests CreateOrUpdate, Get, and Delete operations

## References

- [Bicep Local Deploy Unit Testing Guide](https://github.com/Azure/bicep/blob/main/docs/experimental/local-deploy-dotnet-unittesting-guide.md)
- [Microsoft Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
