using Quitto.Models;

namespace Quitto.Lib;

public record Balance(Guid MemberId, decimal Net);
public record Settlement(Guid FromMemberId, Guid ToMemberId, decimal Amount);

public class BalanceService
{
    /// <summary>
    /// Calcule le solde net de chaque membre. > 0 = on lui doit, &lt; 0 = il doit.
    ///
    /// Règle de partage v1 : split égal entre les participants d'une dépense.
    /// Le payeur est crédité du montant total, chaque participant (incluant le payeur
    /// s'il fait partie des participants) est débité de amount/count.
    ///
    /// Les transferts (remboursements manuels) : `from` est crédité, `to` est débité,
    /// car un transfert de A vers B *résorbe* la dette de A envers B.
    /// </summary>
    public List<Balance> Compute(
        IReadOnlyList<Member> members,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<ExpenseParticipant> participants,
        IReadOnlyList<Transfer> transfers)
    {
        var net = members.ToDictionary(m => m.Id, _ => 0m);

        var participantsByExpense = participants
            .GroupBy(p => p.ExpenseId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.MemberId).ToList());

        foreach (var e in expenses)
        {
            if (!participantsByExpense.TryGetValue(e.Id, out var ps) || ps.Count == 0)
                continue; // dépense sans participants : on ignore

            var share = Math.Round(e.Amount / ps.Count, 2, MidpointRounding.AwayFromZero);

            if (net.ContainsKey(e.PayerId)) net[e.PayerId] += e.Amount;
            foreach (var memberId in ps)
            {
                if (net.ContainsKey(memberId)) net[memberId] -= share;
            }

            // Correction d'arrondi : si la somme des shares ≠ amount, on ajuste sur le payeur.
            var totalShares = share * ps.Count;
            var diff = e.Amount - totalShares;
            if (diff != 0m && net.ContainsKey(e.PayerId))
            {
                net[e.PayerId] -= diff;
            }
        }

        foreach (var t in transfers)
        {
            if (net.ContainsKey(t.FromMemberId)) net[t.FromMemberId] += t.Amount;
            if (net.ContainsKey(t.ToMemberId))   net[t.ToMemberId]   -= t.Amount;
        }

        return net.Select(kv => new Balance(kv.Key, Math.Round(kv.Value, 2, MidpointRounding.AwayFromZero))).ToList();
    }

    /// <summary>
    /// Variante avec filtre temporel : on ne prend que les dépenses et les transferts
    /// dont <c>paid_at</c> tombe dans la fenêtre [from, to] (bornes inclusives, null = ouvert).
    /// Les participants des dépenses retenues suivent automatiquement.
    /// </summary>
    public List<Balance> ComputeFiltered(
        IReadOnlyList<Member> members,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<ExpenseParticipant> participants,
        IReadOnlyList<Transfer> transfers,
        DateOnly? from, DateOnly? to)
    {
        bool InRange(DateOnly d) =>
            (from is null || d >= from) && (to is null || d <= to);

        var filteredExpenses     = expenses.Where(e => InRange(e.PaidAt)).ToList();
        var keptExpenseIds       = filteredExpenses.Select(e => e.Id).ToHashSet();
        var filteredParticipants = participants.Where(p => keptExpenseIds.Contains(p.ExpenseId)).ToList();
        var filteredTransfers    = transfers.Where(t => InRange(t.PaidAt)).ToList();

        return Compute(members, filteredExpenses, filteredParticipants, filteredTransfers);
    }

    /// <summary>
    /// Algorithme glouton : on matche le plus gros créditeur avec le plus gros débiteur,
    /// on transfère min(|cred|, |deb|), on répète jusqu'à zéro. Produit un nombre minimal
    /// de transferts (ou très proche du minimum) pour solder.
    /// </summary>
    public List<Settlement> Simplify(IReadOnlyList<Balance> balances)
    {
        var creditors = balances.Where(b => b.Net > 0.005m)
            .Select(b => (Id: b.MemberId, Amount: b.Net))
            .OrderByDescending(x => x.Amount)
            .ToList();
        var debtors = balances.Where(b => b.Net < -0.005m)
            .Select(b => (Id: b.MemberId, Amount: -b.Net))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var result = new List<Settlement>();
        int i = 0, j = 0;
        while (i < debtors.Count && j < creditors.Count)
        {
            var d = debtors[i];
            var c = creditors[j];
            var pay = Math.Round(Math.Min(d.Amount, c.Amount), 2, MidpointRounding.AwayFromZero);
            if (pay > 0m)
            {
                result.Add(new Settlement(d.Id, c.Id, pay));
            }
            d.Amount -= pay;
            c.Amount -= pay;
            debtors[i] = d;
            creditors[j] = c;
            if (d.Amount < 0.005m) i++;
            if (c.Amount < 0.005m) j++;
        }
        return result;
    }
}
