namespace ProfileService.Application.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Base URL of file storage service, e.g. https://storage.sabzevar.ir</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
