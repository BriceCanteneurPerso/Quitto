using System.Text.Json;
using Microsoft.JSInterop;

namespace Quitto.Lib;

public record RecentGroup(Guid Id, string Name, DateTime LastVisit);

/// <summary>
/// Liste locale (localStorage) des groupes visités sur cet appareil.
/// Pas de fetch réseau côté Home : on affiche directement ce que cet appareil
/// a déjà vu. Si l'utilisateur ouvre un nouveau lien `/g/{id}`, on l'ajoute ici.
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

    public async Task RememberAsync(Guid id, string name)
    {
        var list = await GetAllAsync();
        list.RemoveAll(r => r.Id == id);
        list.Insert(0, new RecentGroup(id, name, DateTime.UtcNow));
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
