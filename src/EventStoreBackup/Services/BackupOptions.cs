namespace EventStoreBackup.Services;

public class BackupOptions
{
    public string DataDirectory { get; set; } = null!;

    public string TempDirectory { get; set; } = "/tmp";

    /// <summary>
    /// Map of Hostnames to pods
    /// </summary>
    public Dictionary<string, string> Pods { get; set; } = new ();

    public void Validate()
    {
        if (string.IsNullOrEmpty(DataDirectory))
            throw new InvalidOperationException("Data directory is required");

        if (string.IsNullOrEmpty(TempDirectory))
            throw new InvalidOperationException("Temp directory is required");
    }
}