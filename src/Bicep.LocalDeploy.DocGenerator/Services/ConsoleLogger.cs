using Microsoft.Extensions.Logging;

namespace Bicep.LocalDeploy.DocGenerator.Services
{
    /// <summary>
    /// Log level enumeration for command line options.
    /// </summary>
    public enum BicepLogLevel
    {
        /// <summary>Critical log level.</summary>
        Critical = LogLevel.Critical,

        /// <summary>Debug log level.</summary>
        Debug = LogLevel.Debug,

        /// <summary>Error log level.</summary>
        Error = LogLevel.Error,

        /// <summary>Information log level.</summary>
        Information = LogLevel.Information,

        /// <summary>No logging.</summary>
        None = LogLevel.None,

        /// <summary>Trace log level.</summary>
        Trace = LogLevel.Trace,

        /// <summary>Warning log level.</summary>
        Warning = LogLevel.Warning,
    }

    /// <summary>
    /// Console logger implementation for the check command.
    /// </summary>
    internal sealed class ConsoleLogger(LogLevel minLogLevel = LogLevel.Information) : ILogger
    {
        private static readonly Lock ConsoleLock = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            _ = state;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minLogLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            _ = eventId;

            if (!IsEnabled(logLevel))
            {
                return;
            }

            lock (ConsoleLock)
            {
                string message = formatter(state, exception);

                if (logLevel >= LogLevel.Warning)
                {
                    Console.ForegroundColor = GetColorLevel(logLevel);
                    Console.Write($"{logLevel} ");
                    Console.ResetColor();
                }

                if (logLevel >= LogLevel.Error)
                {
                    Console.Error.WriteLine(message);
                }
                else
                {
                    Console.WriteLine(message);
                }

                if (exception is not null)
                {
                    Console.Error.WriteLine($"  {exception.Message}");
                    if (exception.StackTrace is not null)
                    {
                        Console.Error.WriteLine($"  {exception.StackTrace}");
                    }
                }
            }
        }

        private static ConsoleColor GetColorLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Critical => ConsoleColor.DarkRed,
                LogLevel.Error => ConsoleColor.DarkRed,
                LogLevel.Warning => ConsoleColor.DarkYellow,
                _ => ConsoleColor.White,
            };
        }
    }
}
