namespace Santy.Web.Services;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly bool _append;
    private readonly object _lock = new();

    public FileLoggerProvider(string filePath, bool append = true)
    {
        _filePath = filePath;
        _append = append;
        
        if (!append && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePath, _lock);
    }

    public void Dispose()
    {
    }

    private class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _filePath;
        private readonly object _lock;

        public FileLogger(string categoryName, string filePath, object lockObj)
        {
            _categoryName = categoryName;
            _filePath = filePath;
            _lock = lockObj;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_categoryName}: {formatter(state, exception)}";
            
            if (exception != null)
            {
                message += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                File.AppendAllText(_filePath, message + Environment.NewLine);
            }
        }
    }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath, bool append = true)
    {
        builder.AddProvider(new FileLoggerProvider(filePath, append));
        return builder;
    }
}
