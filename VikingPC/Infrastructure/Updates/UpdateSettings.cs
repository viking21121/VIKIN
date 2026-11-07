namespace AsistenciaNatacion.Infrastructure.Updates;

public static class UpdateSettings
{
    // Repositorio oficial de actualizaciones de VIKING.
    public const string GitHubOwner = "viking21121";
    public const string GitHubRepository = "VIKIN";

    public const string ProductName = "VIKING";
    public const string UpdaterFileName = "VikingUpdater.exe";
    public const string ReleaseAssetName = "VIKING.zip";

    public static string LatestReleaseUrl => $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/releases/latest";
}
