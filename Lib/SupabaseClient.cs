using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Quitto.Models;

namespace Quitto.Lib;

/// <summary>
/// Wrapper minimaliste autour de PostgREST (Supabase).
/// Aucune dépendance SDK : on signe les requêtes manuellement avec
/// `apikey` + `Authorization: Bearer <anon>` + notre header maison `x-group-id`.
///
/// Les méthodes lèvent en cas d'échec HTTP — c'est aux pages d'attraper.
/// </summary>
public class SupabaseClient
{
    private readonly SupabaseConfig _cfg;
    private readonly GroupSession _session;
    private readonly HttpClient _http = new();

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new DateOnlyJsonConverter() }
    };

    public SupabaseClient(SupabaseConfig cfg, GroupSession session)
    {
        _cfg = cfg;
        _session = session;
    }

    public bool IsConfigured => _cfg.IsConfigured;

    private string Rest(string path) => $"{_cfg.Url.TrimEnd('/')}/rest/v1/{path.TrimStart('/')}";

    private HttpRequestMessage Build(HttpMethod method, string path, object? body = null, string? prefer = null, Guid? overrideGroupId = null)
    {
        var req = new HttpRequestMessage(method, Rest(path));
        req.Headers.TryAddWithoutValidation("apikey", _cfg.AnonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.AnonKey);
        // Le header maison qui pilote la RLS. Un override est possible pour le cas
        // INSERT du tout premier groupe (où GroupSession n'est pas encore positionnée).
        var groupId = overrideGroupId ?? _session.GroupId;
        if (groupId.HasValue)
        {
            req.Headers.TryAddWithoutValidation("x-group-id", groupId.Value.ToString());
        }
        if (!string.IsNullOrEmpty(prefer))
        {
            req.Headers.TryAddWithoutValidation("Prefer", prefer);
        }
        if (body is not null)
        {
            req.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        }
        return req;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req)
    {
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var detail = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Supabase {req.Method} {req.RequestUri?.PathAndQuery} → {(int)resp.StatusCode} {resp.ReasonPhrase}: {detail}");
        }
        return resp;
    }

    // ---------- API publique ----------

    public async Task<List<T>> SelectAsync<T>(string path)
    {
        using var req = Build(HttpMethod.Get, path);
        using var resp = await SendAsync(req);
        var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, Json) ?? new();
    }

    public async Task<T?> SelectSingleAsync<T>(string path)
    {
        var list = await SelectAsync<T>(path);
        return list.Count == 0 ? default : list[0];
    }

    /// <summary>Insert + return de la (les) ligne(s) insérée(s).</summary>
    public async Task<List<T>> InsertAsync<T>(string table, object body, Guid? overrideGroupId = null)
    {
        using var req = Build(HttpMethod.Post, table, body, prefer: "return=representation", overrideGroupId);
        using var resp = await SendAsync(req);
        var stream = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, Json) ?? new();
    }

    public async Task<T> InsertOneAsync<T>(string table, object body, Guid? overrideGroupId = null)
    {
        var list = await InsertAsync<T>(table, body, overrideGroupId);
        if (list.Count == 0) throw new InvalidOperationException($"INSERT into {table} returned no rows");
        return list[0];
    }

    public async Task UpdateAsync(string path, object body)
    {
        using var req = Build(HttpMethod.Patch, path, body);
        using var resp = await SendAsync(req);
    }

    public async Task DeleteAsync(string path)
    {
        using var req = Build(HttpMethod.Delete, path);
        using var resp = await SendAsync(req);
    }

    // ---------- Helpers spécifiques Quitto ----------

    public async Task<Group?> GetGroupAsync(Guid id)
        => await SelectSingleAsync<Group>($"groups?id=eq.{id}&select=*");

    public Task<Group> CreateGroupAsync(Guid id, string name, string currency)
        => InsertOneAsync<Group>("groups",
            new { id, name, currency },
            overrideGroupId: id);

    public Task<List<Member>> GetMembersAsync()
        => SelectAsync<Member>("members?select=*&order=created_at.asc");

    public Task<Member> AddMemberAsync(Guid groupId, string name, string? color = null)
        => InsertOneAsync<Member>("members", new { group_id = groupId, name, color });

    public Task<List<Expense>> GetExpensesAsync()
        => SelectAsync<Expense>("expenses?select=*&order=paid_at.desc,created_at.desc");

    public Task<Expense?> GetExpenseAsync(Guid id)
        => SelectSingleAsync<Expense>($"expenses?id=eq.{id}&select=*");

    public Task<List<ExpenseParticipant>> GetExpenseParticipantsAsync()
        => SelectAsync<ExpenseParticipant>("expense_participants?select=*");

    public Task<List<ExpenseParticipant>> GetExpenseParticipantsForAsync(Guid expenseId)
        => SelectAsync<ExpenseParticipant>($"expense_participants?expense_id=eq.{expenseId}&select=*");

    public async Task<Expense> AddExpenseAsync(Guid groupId, Guid payerId, decimal amount, string description, DateOnly paidAt, IEnumerable<Guid> participantIds, string? category = null)
    {
        var expense = await InsertOneAsync<Expense>("expenses", new
        {
            group_id = groupId,
            payer_id = payerId,
            amount,
            description,
            category,
            paid_at = paidAt
        });
        var participants = participantIds
            .Select(mid => new { expense_id = expense.Id, member_id = mid })
            .ToArray();
        if (participants.Length > 0)
        {
            await InsertAsync<ExpenseParticipant>("expense_participants", participants);
        }
        return expense;
    }

    public Task DeleteExpenseAsync(Guid expenseId)
        => DeleteAsync($"expenses?id=eq.{expenseId}");

    /// <summary>
    /// Met à jour les colonnes éditables de la dépense puis remplace la liste des
    /// participants (delete-all + re-insert). Plus simple qu'un diff INSERT/DELETE,
    /// volume négligeable.
    /// </summary>
    public async Task UpdateExpenseAsync(Guid expenseId, Guid payerId, decimal amount, string description, DateOnly paidAt, IEnumerable<Guid> participantIds, string? category = null)
    {
        await UpdateAsync($"expenses?id=eq.{expenseId}", new
        {
            payer_id = payerId,
            amount,
            description,
            category,
            paid_at = paidAt
        });

        await DeleteAsync($"expense_participants?expense_id=eq.{expenseId}");
        var participants = participantIds
            .Select(mid => new { expense_id = expenseId, member_id = mid })
            .ToArray();
        if (participants.Length > 0)
        {
            await InsertAsync<ExpenseParticipant>("expense_participants", participants);
        }
    }

    public Task<List<Transfer>> GetTransfersAsync()
        => SelectAsync<Transfer>("transfers?select=*&order=paid_at.desc,created_at.desc");

    public Task<Transfer> AddTransferAsync(Guid groupId, Guid fromMemberId, Guid toMemberId, decimal amount, DateOnly paidAt)
        => InsertOneAsync<Transfer>("transfers", new
        {
            group_id = groupId,
            from_member_id = fromMemberId,
            to_member_id = toMemberId,
            amount,
            paid_at = paidAt
        });

    public Task DeleteTransferAsync(Guid transferId)
        => DeleteAsync($"transfers?id=eq.{transferId}");
}

internal class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateOnly.ParseExact(reader.GetString()!, Format);
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
