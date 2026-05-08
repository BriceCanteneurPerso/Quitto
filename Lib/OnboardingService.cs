using Microsoft.JSInterop;

namespace Quitto.Lib;

/// <summary>
/// Mémorise (en localStorage) si l'utilisateur a vu le mini-onboarding sur sa
/// première visite d'un tricount. La bannière ne s'affiche qu'une fois par
/// appareil — pas par tricount, pas par session.
/// </summary>
public class OnboardingService
{
    private const string Key = "quitto.onboarded";
    private readonly IJSRuntime _js;

    public OnboardingService(IJSRuntime js) { _js = js; }

    public async Task<bool> HasSeenAsync()
    {
        var v = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
        return v == "true";
    }

    public Task MarkSeenAsync()
        => _js.InvokeVoidAsync("localStorage.setItem", Key, "true").AsTask();
}
