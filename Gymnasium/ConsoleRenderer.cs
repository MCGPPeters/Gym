using System;

namespace Gymnasium;

/// <summary>
/// Utility methods for rendering simple ASCII graphics to the console.
/// This keeps console output consistent across environments while avoiding
/// any external rendering dependencies.
/// </summary>
public static class ConsoleRenderer
{
    /// <summary>
    /// Writes a title header for an environment.
    /// </summary>
    public static void RenderHeader(string envName)
    {
        Console.WriteLine($"== {envName} ==");
    }

    /// <summary>
    /// Renders a 2D grid of characters to the console.
    /// </summary>
    public static void RenderGrid(char[,] grid, bool spaced = true)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Console.Write(grid[r, c]);
                if (spaced && c < cols - 1)
                    Console.Write(' ');
            }
            Console.WriteLine();
        }
    }
}
