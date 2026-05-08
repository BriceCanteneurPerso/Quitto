using System.Text.Json;
using Quitto.Models;

namespace Quitto.Lib;

/// <summary>
/// Construction et restauration de snapshots <see cref="QuittoBackup"/>.
///
/// Restauration = **non-destructive** par défaut : on crée un nouveau tricount
/// (nouveau group_id) et on insère tout dedans en gardant les ids enfants
/// (membres, dépenses, transferts) inchangés. L'ancien tricount source reste
/// intact côté serveur. Évite tout risque d'écrasement accidentel.
/// </summary>
public class BackupService
{
    private readonly SupabaseClient _sb;
    private readonly GroupSession _session;

    public BackupService(SupabaseClient sb, GroupSession session)
    {
        _sb = sb;
        _session = session;
    }

    /// <summary>
    /// Construit un snapshot du groupe courant (lit toutes les tables liées
    /// via le header `x-group-id` déjà positionné par la page appelante).
    /// </summary>
    public async Task<QuittoBackup> ExportAsync(Group group)
    {
        var members      = await _sb.GetMembersAsync();
        var expenses     = await _sb.GetExpensesAsync();
        var participants = await _sb.GetExpenseParticipantsAsync();
        var transfers    = await _sb.GetTransfersAsync();

        return new QuittoBackup
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Group = group,
            Members = members,
            Expenses = expenses,
            Participants = participants,
            Transfers = transfers,
        };
    }

    public string Serialize(QuittoBackup backup)
        => JsonSerializer.Serialize(backup, SupabaseClient.Json);

    public QuittoBackup? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<QuittoBackup>(json, SupabaseClient.Json); }
        catch { return null; }
    }

    /// <summary>
    /// Clone non-destructive : crée un nouveau groupe (nouveau group_id) avec le
    /// nom donné (ou celui du backup), puis ré-insère membres/dépenses/participants/
    /// transferts en remappant uniquement le group_id sur le nouveau. Les ids des
    /// enfants sont conservés (UUID ⇒ pas de collision).
    ///
    /// Renvoie le nouveau group_id pour navigation.
    /// </summary>
    public async Task<Guid> CloneAsync(QuittoBackup backup, string? newName = null)
    {
        if (backup.Group is null) throw new InvalidOperationException("Backup sans group");

        var newGroupId = Guid.NewGuid();
        _session.Set(newGroupId);

        // 1) Création du groupe (override pour passer la policy d'INSERT).
        var groupName = !string.IsNullOrWhiteSpace(newName)
            ? newName
            : $"{backup.Group.Name} (copie)";
        await _sb.CreateGroupAsync(newGroupId, groupName, backup.Group.Currency);

        // 2) Membres : mêmes ids, group_id remappé.
        foreach (var m in backup.Members)
        {
            await _sb.InsertOneAsync<Member>("members", new
            {
                id = m.Id,
                group_id = newGroupId,
                name = m.Name,
                color = m.Color
            });
        }

        // 3) Dépenses + participants. On réutilise InsertOneAsync<Expense> pour
        //    bénéficier de la sérialisation snake_case + DateOnly converter.
        foreach (var e in backup.Expenses)
        {
            await _sb.InsertOneAsync<Expense>("expenses", new
            {
                id = e.Id,
                group_id = newGroupId,
                payer_id = e.PayerId,
                amount = e.Amount,
                description = e.Description,
                category = e.Category,
                notes = e.Notes,
                paid_at = e.PaidAt
            });
        }

        if (backup.Participants.Count > 0)
        {
            // INSERT batch (PostgREST l'accepte si on envoie un array).
            var rows = backup.Participants
                .Select(p => new { expense_id = p.ExpenseId, member_id = p.MemberId })
                .ToArray();
            await _sb.InsertAsync<ExpenseParticipant>("expense_participants", rows);
        }

        // 4) Transferts.
        foreach (var t in backup.Transfers)
        {
            await _sb.InsertOneAsync<Transfer>("transfers", new
            {
                id = t.Id,
                group_id = newGroupId,
                from_member_id = t.FromMemberId,
                to_member_id = t.ToMemberId,
                amount = t.Amount,
                paid_at = t.PaidAt
            });
        }

        return newGroupId;
    }
}
