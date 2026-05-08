using Microsoft.JSInterop;

namespace Quitto.Lib;

/// <summary>
/// Préférence de thème (clair / sombre / auto). Persisté dans localStorage,
/// avec écoute de `prefers-color-scheme` pour le mode auto.
/// </summary>
public class ThemeService
{
    public enum Mode { Auto, Light, Dark }

    private const string StorageKey = "quitto.theme";
    private readonly IJSRuntime _js;
    private bool _systemDark;

    public Mode Preference { get; private set; } = Mode.Auto;

    /// <summary>État effectif (résout Auto via la pref système).</summary>
    public bool IsDarkMode => Preference switch
    {
        Mode.Light => false,
        Mode.Dark  => true,
        _          => _systemDark
    };

    public event Action? Changed;

    public ThemeService(IJSRuntime js) { _js = js; }

    public async Task InitializeAsync()
    {
        var raw = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (Enum.TryParse<Mode>(raw, ignoreCase: true, out var m))
        {
            Preference = m;
        }
        try { _systemDark = await _js.InvokeAsync<bool>("quitto.prefersDark"); }
        catch { _systemDark = false; }

        Changed?.Invoke();
    }

    public async Task SetAsync(Mode mode)
    {
        Preference = mode;
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, mode.ToString());
        Changed?.Invoke();
    }

    public Task ToggleAsync()
    {
        // Cycle : Auto → Light → Dark → Auto
        var next = Preference switch
        {
            Mode.Auto  => Mode.Light,
            Mode.Light => Mode.Dark,
            Mode.Dark  => Mode.Auto,
            _          => Mode.Auto
        };
        return SetAsync(next);
    }
}
