using System.Reflection;

namespace dstrings
{
    class Program
    {
        static void Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            ConsoleApp.Run("dotnet-dstrings", $"{version.Major}.{version.Minor}.{version.Build}", args);
        }
    }
}
