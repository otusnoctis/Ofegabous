namespace App.Services;

public sealed class UpdateStartupState
{
    public UpdateStartupState()
    {
        UpdatedFromVersion = ReadArg("--updated-from");
        UpdatedToVersion = ReadArg("--updated-to");
        UpdatedPackage = ReadArg("--updated-package");
    }

    public string? FirstRunVersion { get; set; }
    public string? RestartedVersion { get; set; }
    public string? UpdatedFromVersion { get; }
    public string? UpdatedToVersion { get; }
    public string? UpdatedPackage { get; }

    private static string? ReadArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
