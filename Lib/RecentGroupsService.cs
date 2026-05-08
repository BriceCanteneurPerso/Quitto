using System.Text.Json;
using Microsoft.JSInterop;

namespace Quitto.Lib;

public record RecentGroup(Guid Id, string Name, DateTime LastVisit, string? Pin = null);

/// <summary>
/// Liste locale (localStorage) des groupes visités sur cet appareil.
/// On stocke aussi le PIN éventuel pour qu'à la prochaine visite depuis Home,
/// on puisse re-soumettre le bon header `x-share-pin` sans demander à l'utilisateur.
///
/// Les anciens entries sans pin se désérialisent avec Pin=null grâce au record
/// avec valeur par défaut.
/// </summary>
public class RecentGroupsService
{
    private const string StorageKey = "quitto.recent";
    private readonly IJSRuntime _js;

    public RecentGroupsService(IJSRuntime js) { _js = js; }

    public async Task<List<RecentGroup>> GetAllAsync()
    {
        var raw = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(raw)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<RecentGroup>>(raw) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<RecentGroup?> GetAsync(Guid id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(r => r.Id == id);
    }

    public async Task RememberAsync(Guid id, string name, string? pin = null)
    {
        var list = await GetAllAsync();
        // Si on connaît déjà ce groupe et qu'on n'a pas de pin nouveau, on garde l'ancien
        // (cas : re-visite via /g/{id} sans le pin alors qu'on l'avait stocké).
        var existing = list.FirstOrDefault(r => r.Id == id);
        var resolvedPin = !string.IsNullOrEmpty(pin) ? pin : existing?.Pin;
        list.RemoveAll(r => r.Id == id);
        list.Insert(0, new RecentGroup(id, name, DateTime.UtcNow, resolvedPin));
        if (list.Count > 20) list = list.Take(20).ToList();
        var raw = JsonSerializer.Serialize(list);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, raw);
    }

    public async Task ForgetAsync(Guid id)
    {
        var list = await GetAllAsync();
        list.RemoveAll(r => r.Id == id);
        var raw = JsonSerializer.Serialize(list);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, raw);
    }
}
