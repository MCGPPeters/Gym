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
            
            // Check if we're running in a Codespace or other headless environment
            bool isHeadless = Environment.GetEnvironmentVariable("GITHUB_CODESPACE_NAME") != null ||
                             Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null;
            
            if (isHeadless)
            {
                Console.WriteLine("Detected headless environment (GitHub Codespace).");
                Console.WriteLine("The Avalonia UI cannot display in this environment.");
                Console.WriteLine("To run the UI application with a graphical interface:");
                Console.WriteLine("1. Clone the repository to a local machine");
                Console.WriteLine("2. Run 'dotnet run --project Gymnasium.UI'");
                return; // Exit gracefully without starting UI
            }
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Console.WriteLine("Application completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
            }
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        Console.WriteLine("Configuring Avalonia application...");
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
