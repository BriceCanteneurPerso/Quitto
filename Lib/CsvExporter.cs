using System.Globalization;
using System.Text;
using Quitto.Models;

namespace Quitto.Lib;

/// <summary>
/// Génère un CSV des dépenses (séparateur virgule, encodage UTF-8 BOM pour
/// compat Excel FR). Tous les champs sont quotés et les guillemets internes
/// doublés (RFC 4180).
/// </summary>
public static class CsvExporter
{
    public static string ExportExpenses(
        Group group,
        IReadOnlyList<Member> members,
        IReadOnlyList<Expense> expenses,
        IReadOnlyDictionary<Guid, List<Guid>> participantsByExpense)
    {
        var membersById = members.ToDictionary(m => m.Id, m => m.Name);
        var sb = new StringBuilder();
        // BOM UTF-8 : Excel FR détecte alors le bon encodage.
        sb.Append('﻿');

        sb.AppendLine(string.Join(",", new[]
        {
            "Date", "Description", "Catégorie", "Payeur", "Montant", "Devise",
            "Participants", "Part par personne", "Notes"
        }));

        foreach (var e in expenses.OrderBy(e => e.PaidAt).ThenBy(e => e.CreatedAt))
        {
            var participants = participantsByExpense.GetValueOrDefault(e.Id) ?? new();
            var participantNames = string.Join(" + ", participants
                .Select(pid => membersById.GetValueOrDefault(pid, "?")));
            var perPerson = participants.Count > 0
                ? Math.Round(e.Amount / participants.Count, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var category = CategoryDetector.FromKey(e.Category).Label;
            var payer = membersById.GetValueOrDefault(e.PayerId, "?");

            sb.AppendLine(string.Join(",", new[]
            {
                Quote(e.PaidAt.ToString("yyyy-MM-dd")),
                Quote(e.Description),
                Quote(category),
                Quote(payer),
                Quote(e.Amount.ToString("F2", CultureInfo.InvariantCulture)),
                Quote(group.Currency),
                Quote(participantNames),
                Quote(perPerson.ToString("F2", CultureInfo.InvariantCulture)),
                Quote(e.Notes ?? "")
            }));
        }

        return sb.ToString();
    }

    private static string Quote(string s)
        => "\"" + s.Replace("\"", "\"\"") + "\"";
}
