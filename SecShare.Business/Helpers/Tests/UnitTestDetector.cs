namespace SecShare.Business.Helpers.Tests;

public static class UnitTestDetector
{
    public static bool IsRunningFromXUnit =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Any(assembly => assembly.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) == true);
}
