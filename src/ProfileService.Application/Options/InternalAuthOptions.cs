namespace ProfileService.Application.Options;

public sealed class InternalAuthOptions
{
    public const string SectionName = "InternalAuth";

    public string HeaderName { get; set; } = "X-Api-Key";
    public List<ApiKeyEntry> ApiKeys { get; set; } = new();

    public sealed class ApiKeyEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
