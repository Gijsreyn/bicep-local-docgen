using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Formats and displays check results in a style similar to CSharpier.
    /// </summary>
    internal sealed class CheckFormatter(ILogger logger)
    {
        /// <summary>
        /// Formats and displays the check results, returning the exit code.
        /// </summary>
        /// <param name="results">List of check results to format.</param>
        /// <param name="stopwatch">Stopwatch measuring the operation duration.</param>
        /// <returns>Exit code: 0 for success, 1 for validation errors.</returns>
        public int FormatResults(List<CheckResult> results, Stopwatch stopwatch)
        {
            List<CheckResult> errorResults = [.. results.Where(r => r.HasErrors)];
            int totalFiles = results.Count;
            int errorCount = errorResults.Count;

            // Display errors
            foreach (CheckResult result in errorResults)
            {
                string relativePath = GetRelativePath(result.FilePath);
                Console.Error.WriteLine(
                    $"Error {relativePath} - Missing required documentation attributes."
                );

                foreach (ValidationError error in result.Errors)
                {
                    Console.Error.WriteLine(
                        $"  ----------------------------- Expected: Around Line {error.LineNumber} -----------------------------"
                    );
                    Console.Error.WriteLine($"                  {error.ExpectedAttribute}");
                    Console.Error.WriteLine(
                        $"  ----------------------------- Actual: Around Line {error.LineNumber} -----------------------------"
                    );
                    Console.Error.WriteLine($"                  // {error.Message}");
                    Console.Error.WriteLine();
                }
            }

            // Summary
            if (errorCount > 0)
            {
                Console.WriteLine(
                    $"Checked {totalFiles} files in {stopwatch.ElapsedMilliseconds}ms."
                );
                LogErrorFound(logger, errorCount, null);
                return 1;
            }
            else
            {
                Console.WriteLine(
                    $"Checked {totalFiles} files in {stopwatch.ElapsedMilliseconds}ms."
                );
                LogSuccess(logger, null);
                return 0;
            }
        }

        private static string GetRelativePath(string fullPath)
        {
            string currentDir = Directory.GetCurrentDirectory();

            try
            {
                return Path.GetRelativePath(currentDir, fullPath);
            }
            catch
            {
                return fullPath;
            }
        }

        // LoggerMessage delegates for performance
        private static readonly Action<ILogger, int, Exception?> LogErrorFound =
            LoggerMessage.Define<int>(
                LogLevel.Error,
                new EventId(1, nameof(LogErrorFound)),
                "Found {ErrorCount} file(s) with missing documentation attributes."
            );

        private static readonly Action<ILogger, Exception?> LogSuccess = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, nameof(LogSuccess)),
            "All files have the required documentation attributes."
        );
    }
}
