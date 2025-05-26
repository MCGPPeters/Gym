using Avalonia;
using System;
using System.Diagnostics;
using Avalonia.Logging;
using System.IO;

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
            // Write to a file as the very first action to prove Main is entered
            try
            {
                System.IO.File.AppendAllText("main_entry.log", $"Main entered at {DateTime.Now}\n");
            }
            catch (Exception fileEx)
            {
                // If file write fails, try to log to console
                Console.WriteLine($"[LOG] Could not write to main_entry.log: {fileEx.Message}");
            }
            System.IO.File.AppendAllText("main_entry.log", $"Before console output at {DateTime.Now}\n");
            Console.WriteLine("[LOG] Starting Gymnasium UI application...");
            System.IO.File.AppendAllText("main_entry.log", $"After console output at {DateTime.Now}\n");
            Console.WriteLine($"[LOG] Current Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"[LOG] OS Version: {Environment.OSVersion}");
            Console.WriteLine($"[LOG] 64-bit OS: {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"[LOG] 64-bit Process: {Environment.Is64BitProcess}");
            Console.WriteLine($"[LOG] .NET Version: {Environment.Version}");
            Console.WriteLine($"[LOG] Avalonia Version: {typeof(Avalonia.Application).Assembly.GetName().Version}");
            Console.WriteLine($"[LOG] Args: {string.Join(", ", args)}");

            // Log environment variables relevant to headless detection
            Console.WriteLine($"[LOG] GITHUB_CODESPACE_NAME: {Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME")}");
            Console.WriteLine($"[LOG] GITHUB_ACTIONS: {Environment.GetEnvironmentVariable("GITHUB_ACTIONS")}");
            Console.WriteLine($"[LOG] DOTNET_RUNNING_IN_CONTAINER: {Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")}");

            // Check if we're running in a CI/Codespace environment (for GitHub Actions/Codespaces)
            bool isCodespace = Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME") != null;
            bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;
            bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;
            bool isHeadless = isCodespace || isGitHubActions || (isContainer && (isCodespace || isGitHubActions));

            Console.WriteLine($"[LOG] isCodespace: {isCodespace}");
            Console.WriteLine($"[LOG] isGitHubActions: {isGitHubActions}");
            Console.WriteLine($"[LOG] isContainer: {isContainer}");
            Console.WriteLine($"[LOG] isHeadless: {isHeadless}");

            if (isHeadless)
            {
                Console.WriteLine("[LOG] Detected headless environment (GitHub Codespace/Actions).");
                Console.WriteLine("The Avalonia UI cannot display in this environment.");
                Console.WriteLine("To run the UI application with a graphical interface:");
                Console.WriteLine("1. Clone the repository to a local machine");
                Console.WriteLine("2. Run 'dotnet run --project Gymnasium.UI'");
                return; // Exit gracefully without starting UI
            }

            Console.WriteLine("[LOG] Environment check passed, starting UI...");

            System.IO.File.AppendAllText("main_entry.log", $"Before BuildAvaloniaApp at {DateTime.Now}\n");
            Console.WriteLine("[LOG] Calling BuildAvaloniaApp()");
            var appBuilder = BuildAvaloniaApp();
            System.IO.File.AppendAllText("main_entry.log", $"After BuildAvaloniaApp at {DateTime.Now}\n");
            Console.WriteLine("[LOG] BuildAvaloniaApp() returned");
            System.IO.File.AppendAllText("main_entry.log", $"Before StartWithClassicDesktopLifetime at {DateTime.Now}\n");
            Console.WriteLine("[LOG] Starting Avalonia application with StartWithClassicDesktopLifetime");
            appBuilder.StartWithClassicDesktopLifetime(args);
            System.IO.File.AppendAllText("main_entry.log", $"After StartWithClassicDesktopLifetime at {DateTime.Now}\n");
            Console.WriteLine("[LOG] Application completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error starting application: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            Console.WriteLine("[ERROR] Environment Information:");
            Console.WriteLine($"[ERROR] OS: {Environment.OSVersion}");
            Console.WriteLine($"[ERROR] 64-bit OS: {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"[ERROR] 64-bit Process: {Environment.Is64BitProcess}");
            Console.WriteLine($"[ERROR] .NET Version: {Environment.Version}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"[ERROR] Inner stack trace: {ex.InnerException.StackTrace}");
            }
            if (ex.Message.Contains("Could not initialize GLX") || 
                ex.Message.Contains("OpenGL") || 
                ex.Message.Contains("X11") ||
                ex.Message.Contains("PlatformNotSupportedException"))
            {
                Console.WriteLine();
                Console.WriteLine("[ERROR] This appears to be a graphics/display issue. Please check:");
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
                string message;
                try
                {
                    message = string.Format(messageTemplate, propertyValues);
                }
                catch (FormatException)
                {
                    // Fallback: just print the template and the arguments
                    message = messageTemplate + " | Args: " + string.Join(", ", propertyValues ?? Array.Empty<object>());
                }
                Console.WriteLine($"[{level}] {area}: {message}");
            }
        }
    }
}
