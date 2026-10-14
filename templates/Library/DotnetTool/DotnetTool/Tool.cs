namespace DotnetTool;

/// <summary>
/// Core tool logic, kept separate from Program.cs for testability.
/// </summary>
public static class Tool
{
    /// <summary>
    /// Entry point for the tool's logic.
    /// </summary>
    public static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CHANGEME-toolname <name>");
            return;
        }

        Console.WriteLine(Greet(args[0]));
    }

    /// <summary>
    /// Returns a greeting for the given name.
    /// </summary>
    public static string Greet(string name) => $"Hello, {name}!";
}
