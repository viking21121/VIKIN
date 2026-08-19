namespace AsistenciaNatacion.Infrastructure.Updates;

public sealed class GitHubRelease
{
    public string TagName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Body { get; set; } = "";
    public List<GitHubAsset> Assets { get; set; } = new();
}

public sealed class GitHubAsset
{
    public string Name { get; set; } = "";
    public string BrowserDownloadUrl { get; set; } = "";
}
