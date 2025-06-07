using System.Reflection;

namespace Tests;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TestClassAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class ClassInitializeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestMethodAttribute : Attribute { }

public class TestContext
{
    // Empty for now, but could contain test-specific information
}

public static class Assert
{
    public static bool Debug { get; set; } = false;

    public static void AreEqual<T>(T expected, T actual, string message = null)
    {
        bool areEqual;

        // Special handling for string comparisons
        if (expected is string expectedStr && actual is string actualStr)
        {
            // Normalize strings: trim whitespace and normalize line endings
            var normalizedExpected = NormalizeString(expectedStr);
            var normalizedActual = NormalizeString(actualStr);
            areEqual = normalizedExpected.Equals(normalizedActual);
        }
        else
        {
            areEqual = Equals(expected, actual);
        }

        if (!areEqual)
        {
            Fail($"Expected: {expected}\nActual: {actual}\n{message}");
        }
        else if (Debug)
        {
            Console.WriteLine($"✓ AreEqual passed: {actual}");
        }
    }

    private static string NormalizeString(string input)
    {
        if (input == null)
            return null;

        // Trim whitespace and normalize line endings to LF
        return input.Trim().Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static void IsInstanceOfType(object value, Type expectedType, string message = null)
    {
        if (value == null || !expectedType.IsAssignableFrom(value.GetType()))
        {
            Fail(
                $"Object of type {value?.GetType()?.Name ?? "null"} is not assignable to {expectedType.Name}\n{message}"
            );
        }
        else if (Debug)
        {
            Console.WriteLine($"✓ IsInstanceOfType passed: {value} is of type {expectedType.Name}");
        }
    }

    public static void IsTrue(bool condition, string message = null)
    {
        if (!condition)
        {
            Fail($"Expected true but got false\n{message}");
        }
        else if (Debug)
        {
            Console.WriteLine($"✓ IsTrue passed: {condition}");
        }
    }

    public static void Fail(string message)
    {
        throw new Exception($"Assertion failed: {message}");
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        var assembly = typeof(Program).Assembly;
        var testClasses = assembly.GetTypes().Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

        var totalTests = 0;
        var passedTests = 0;
        var failedTests = new List<string>();

        // Check if we have search terms from command line arguments
        var hasSearchTerms = args != null && args.Length > 0;
        var searchTerms = hasSearchTerms ? args.Select(arg => arg.ToLowerInvariant()).ToArray() : null;

        // Enable debug mode when filtering tests
        if (hasSearchTerms)
        {
            Assert.Debug = true;
        }

        foreach (var testClass in testClasses)
        {
            // Create instance of test class
            var instance = Activator.CreateInstance(testClass);

            // Find and run class initializer if present
            var classInit = testClass
                .GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<ClassInitializeAttribute>() != null);
            if (classInit != null)
            {
                try
                {
                    classInit.Invoke(instance, new object[] { new TestContext() });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Class initializer failed: {ex.InnerException?.Message ?? ex.Message}");
                    continue;
                }
            }

            // Find and run test methods
            var testMethods = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);

            foreach (var testMethod in testMethods)
            {
                // If we have search terms, filter tests based on TestClassName.TestMethodName
                if (hasSearchTerms)
                {
                    var testFullName = $"{testClass.Name}.{testMethod.Name}".ToLowerInvariant();
                    var shouldRunTest = searchTerms.Any(searchTerm => testFullName.Contains(searchTerm));

                    if (!shouldRunTest)
                    {
                        continue; // Skip this test
                    }
                }

                totalTests++;
                try
                {
                    testMethod.Invoke(instance, null);
                    passedTests++;
                }
                catch (Exception ex)
                {
                    failedTests.Add($"{testClass.Name}.{testMethod.Name}: {ex.InnerException?.Message ?? ex.Message}");
                    Console.WriteLine($"  ✗ {testMethod.Name}");
                }
            }
        }

        // Print summary
        Console.WriteLine("\nTest Summary:");
        Console.WriteLine($"Total tests: {totalTests}");
        Console.WriteLine($"Passed: {passedTests}");
        Console.WriteLine($"Failed: {failedTests.Count}");

        if (failedTests.Any())
        {
            Console.WriteLine("\nFailed tests:");
            foreach (var failure in failedTests)
            {
                Console.WriteLine($"  {failure}");
            }
            Environment.Exit(1);
        }
    }
}
