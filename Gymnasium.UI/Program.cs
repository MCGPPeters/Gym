using Avalonia;
using System;
using System.Diagnostics;

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
            // Log out available platform backends
            Console.WriteLine("Available Avalonia backends:");
            var backends = AvaloniaLocator.Current.GetService<Avalonia.Platform.IPlatformManager>()?.GetAvailablePlatforms();
            if (backends != null)
            {
                foreach (var backend in backends)
                {
                    Console.WriteLine($" - {backend}");
                }
            }
            else
            {
                Console.WriteLine(" - None detected");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error detecting Avalonia backends: {ex.Message}");
        }
        
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .LogToConsole(); // Add console logging for better troubleshooting
    }
}
