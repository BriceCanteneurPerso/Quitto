using System.Text.Json.Serialization;
using Quitto.Models;

namespace Quitto.Lib;

/// <summary>
/// Snapshot complet d'un tricount, sérialisable en JSON. Sert d'export local
/// (sauvegarde) et d'entrée pour le clonage non-destructif (CloneFromBackupAsync).
///
/// Versionné pour qu'on puisse muter le format plus tard sans casser les anciens
/// fichiers exportés.
/// </summary>
public class QuittoBackup
{
    [JsonPropertyName("version")]   public int Version { get; set; } = 1;
    [JsonPropertyName("exported_at")] public DateTime ExportedAt { get; set; }

    [JsonPropertyName("group")]        public Group Group { get; set; } = new();
    [JsonPropertyName("members")]      public List<Member> Members { get; set; } = new();
    [JsonPropertyName("expenses")]     public List<Expense> Expenses { get; set; } = new();
    [JsonPropertyName("participants")] public List<ExpenseParticipant> Participants { get; set; } = new();
    [JsonPropertyName("transfers")]    public List<Transfer> Transfers { get; set; } = new();
}
