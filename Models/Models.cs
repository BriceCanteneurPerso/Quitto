using System.Text.Json.Serialization;

namespace Quitto.Models;

// Naming policy = snake_case (PostgREST returns snake_case columns).
// We keep C# PascalCase + [JsonPropertyName] to map both ways.

public class Group
{
    [JsonPropertyName("id")]         public Guid Id { get; set; }
    [JsonPropertyName("name")]       public string Name { get; set; } = "";
    [JsonPropertyName("currency")]   public string Currency { get; set; } = "EUR";
    [JsonPropertyName("share_pin")]  public string? SharePin { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
}

public class Member
{
    [JsonPropertyName("id")]         public Guid Id { get; set; }
    [JsonPropertyName("group_id")]   public Guid GroupId { get; set; }
    [JsonPropertyName("name")]       public string Name { get; set; } = "";
    [JsonPropertyName("color")]      public string? Color { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
}

public class Expense
{
    [JsonPropertyName("id")]          public Guid Id { get; set; }
    [JsonPropertyName("group_id")]    public Guid GroupId { get; set; }
    [JsonPropertyName("payer_id")]    public Guid PayerId { get; set; }
    [JsonPropertyName("amount")]      public decimal Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("category")]    public string? Category { get; set; }
    [JsonPropertyName("notes")]       public string? Notes { get; set; }
    [JsonPropertyName("paid_at")]     public DateOnly PaidAt { get; set; }
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
    [JsonPropertyName("deleted_at")]  public DateTime? DeletedAt { get; set; }
}

public class ExpenseParticipant
{
    [JsonPropertyName("expense_id")] public Guid ExpenseId { get; set; }
    [JsonPropertyName("member_id")]  public Guid MemberId { get; set; }
}

public class Transfer
{
    [JsonPropertyName("id")]              public Guid Id { get; set; }
    [JsonPropertyName("group_id")]        public Guid GroupId { get; set; }
    [JsonPropertyName("from_member_id")]  public Guid FromMemberId { get; set; }
    [JsonPropertyName("to_member_id")]    public Guid ToMemberId { get; set; }
    [JsonPropertyName("amount")]          public decimal Amount { get; set; }
    [JsonPropertyName("paid_at")]         public DateOnly PaidAt { get; set; }
    [JsonPropertyName("created_at")]      public DateTime CreatedAt { get; set; }
    [JsonPropertyName("deleted_at")]      public DateTime? DeletedAt { get; set; }
}
