namespace LegacyMigrationPrep;

internal static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(current);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "BankInsight.API")) &&
                Directory.Exists(Path.Combine(directory.FullName, "CoreBankerWeb")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
