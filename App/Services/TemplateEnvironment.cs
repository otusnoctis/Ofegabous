namespace App.Services;

public sealed class TemplateEnvironment
{
    public string ExecutableDirectory { get; }
    public string? SolutionRoot { get; }
    public string ContentRootDirectory { get; }
    public string StorageRootDirectory { get; }
    public string DataDirectory { get; }
    public string BundledDataDirectory { get; }
    public string PersistenceFilePath { get; }
    public string BundledPersistenceFilePath { get; }
    public string DocumentationDirectory { get; }
    public bool IsDevelopmentMode { get; }

    public TemplateEnvironment(TemplateMetadata metadata)
    {
        ExecutableDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        SolutionRoot = FindSolutionRoot(ExecutableDirectory);
        IsDevelopmentMode = SolutionRoot is not null;
        ContentRootDirectory = ResolveContentRootDirectory();
        StorageRootDirectory = ResolveStorageRootDirectory();
        DataDirectory = Path.Combine(StorageRootDirectory, metadata.DataDirectoryName);
        BundledDataDirectory = Path.Combine(ContentRootDirectory, metadata.DataDirectoryName);
        PersistenceFilePath = Path.Combine(DataDirectory, metadata.PersistenceFileName);
        BundledPersistenceFilePath = Path.Combine(BundledDataDirectory, metadata.PersistenceFileName);
        DocumentationDirectory = Path.Combine(ContentRootDirectory, metadata.DocumentationDirectoryName);
    }

    private string ResolveContentRootDirectory()
    {
        if (SolutionRoot is not null)
        {
            return SolutionRoot;
        }

        return ExecutableDirectory;
    }

    private string ResolveStorageRootDirectory()
    {
        if (SolutionRoot is not null)
        {
            return SolutionRoot;
        }

        var currentDirectory = new DirectoryInfo(ExecutableDirectory);
        if (string.Equals(currentDirectory.Name, "current", StringComparison.OrdinalIgnoreCase) &&
            currentDirectory.Parent is not null)
        {
            return currentDirectory.Parent.FullName;
        }

        return ExecutableDirectory;
    }

    private static string? FindSolutionRoot(string startDirectory)
    {
        var currentDirectory = new DirectoryInfo(startDirectory);
        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Directory.Build.props")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}
