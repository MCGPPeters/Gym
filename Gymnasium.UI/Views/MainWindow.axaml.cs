using Avalonia.Controls;
using Avalonia.Interactivity;
using Gymnasium.UI.ViewModels;
using System;

namespace Gymnasium.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartTrainingButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: Button click detected!\n");
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: DataContext found, calling StartTraining\n");
                
                // Call the StartTraining method directly
                await viewModel.CallStartTraining();
            }
            else
            {
                System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: ERROR - DataContext is null or wrong type: {DataContext?.GetType().Name ?? "null"}\n");
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("button_debug.log", $"{DateTime.Now}: ERROR in button click: {ex.Message}\n{ex.StackTrace}\n");
        }
    }
}