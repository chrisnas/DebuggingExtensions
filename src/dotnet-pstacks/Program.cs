using System.Reflection;
using ParallelStacks.Runtime;

namespace ParallelStacks
{
    class Program
    {
        static void Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            ConsoleApp.Run("dotnet-pstacks", $"{version.Major}.{version.Minor}.{version.Build}", args);
            return;
        }
    }
}
