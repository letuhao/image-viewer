using ImageViewer.IntegrationTests.Tools;

namespace ImageViewer.IntegrationTests.Tools;

/// <summary>
/// Console program to clear database for integration tests
/// </summary>
public class ClearDatabaseProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🛠️  ImageViewer Integration Test Database Tool");
        Console.WriteLine("==============================================");
        Console.WriteLine();
        
        if (args.Length > 0 && args[0].ToLowerInvariant() == "status")
        {
            await ClearDatabaseTool.ShowDatabaseStatusAsync();
        }
        else
        {
            await ClearDatabaseTool.ClearDatabaseAsync();
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
