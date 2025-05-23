using Avalonia;
using System;
using System.Diagnostics;
using Avalonia.Logging;

namespace Gymnasium.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting Gymnasium UI application...");
            
            // Check if we're running in a CI/Codespace environment (for GitHub Actions/Codespaces)
            // Only exit if we're definitely in a headless environment
            bool isCodespace = Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME") != null;
            bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;
            
            // DOTNET_RUNNING_IN_CONTAINER might be set on Docker containers that actually have graphics
            // So only consider it if we're also in a known headless CI environment
            bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;
            bool isHeadless = isCodespace || isGitHubActions || (isContainer && (isCodespace || isGitHubActions));
            
            if (isHeadless)
            {
                Console.WriteLine("Detected headless environment (GitHub Codespace/Actions).");
                Console.WriteLine("The Avalonia UI cannot display in this environment.");
                Console.WriteLine("To run the UI application with a graphical interface:");
                Console.WriteLine("1. Clone the repository to a local machine");
                Console.WriteLine("2. Run 'dotnet run --project Gymnasium.UI'");
                return; // Exit gracefully without starting UI
            }
            
            // Add debugging info for local environments having issues
            Console.WriteLine("Environment check passed, starting UI...");
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Console.WriteLine("Application completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Show more details about the environment to help troubleshoot
            Console.WriteLine("Environment Information:");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"64-bit Process: {Environment.Is64BitProcess}");
            Console.WriteLine($".NET Version: {Environment.Version}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
            }
            
            // This is often related to missing graphics drivers or X11 on Linux
            if (ex.Message.Contains("Could not initialize GLX") || 
                ex.Message.Contains("OpenGL") || 
                ex.Message.Contains("X11") ||
                ex.Message.Contains("PlatformNotSupportedException"))
            {
                Console.WriteLine();
                Console.WriteLine("This appears to be a graphics/display issue. Please check:");
                Console.WriteLine("- On Linux: X11 is installed and the DISPLAY environment is set");
                Console.WriteLine("- On WSL: Install an X server like VcXsrv and set DISPLAY=:0");
                Console.WriteLine("- Required graphics drivers are installed");
            }
            
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("Configuring Avalonia application...");
        
        try 
        {
            // In Avalonia 11.x, we can't enumerate backends this way
            // Instead, just log that we're going to use platform detection
            Console.WriteLine("Using Avalonia platform detection...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during initialization: {ex.Message}");
        }
        
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
            
        // Setup console logging manually since LogToConsole may not be available
        Logger.Sink = new CompositeLogSink(
            Logger.Sink,
            new ConsoleLogSink(LogEventLevel.Debug)
        );
            
        return builder;
    }
    
    // Composite log sink to handle multiple log sinks
    private class CompositeLogSink : ILogSink
    {
        private readonly ILogSink[] _sinks;

        public CompositeLogSink(params ILogSink[] sinks)
        {
            _sinks = sinks;
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            foreach (var sink in _sinks)
            {
                if (sink.IsEnabled(level, area))
                {
                    return true;
                }
            }
            return false;
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            foreach (var sink in _sinks)
            {
                if (sink.IsEnabled(level, area))
                {
                    sink.Log(level, area, source, messageTemplate);
                }
            }
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            foreach (var sink in _sinks)
            {
                if (sink.IsEnabled(level, area))
                {
                    sink.Log(level, area, source, messageTemplate, propertyValues);
                }
            }
        }
    }
    
    // Custom console log sink class for Avalonia
    private class ConsoleLogSink : ILogSink
    {
        private readonly LogEventLevel _minLevel;

        public ConsoleLogSink(LogEventLevel minLevel)
        {
            _minLevel = minLevel;
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return level >= _minLevel;
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            if (IsEnabled(level, area))
            {
                Console.WriteLine($"[{level}] {area}: {messageTemplate}");
            }
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            if (IsEnabled(level, area))
            {
                var message = string.Format(messageTemplate, propertyValues);
                Console.WriteLine($"[{level}] {area}: {message}");
            }
        }
    }
}
