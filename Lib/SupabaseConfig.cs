namespace Quitto.Lib;

public class SupabaseConfig
{
    public string Url { get; set; } = "";
    public string AnonKey { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url)
        && !Url.Contains("YOUR-PROJECT", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(AnonKey)
        && !AnonKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
