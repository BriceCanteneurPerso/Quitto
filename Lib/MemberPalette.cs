namespace Quitto.Lib;

/// <summary>
/// Palette de couleurs pour les avatars de membres. Choisie pour offrir un
/// contraste blanc lisible (textes/initiales).
/// </summary>
public static class MemberPalette
{
    public static readonly string[] Colors =
    {
        "#1976d2", // blue
        "#388e3c", // green
        "#f57c00", // orange
        "#7b1fa2", // purple
        "#d32f2f", // red
        "#00897b", // teal
        "#c2185b", // pink
        "#6d4c41", // brown
    };

    /// <summary>Indexe modulo dans la palette (utile pour assignation par position).</summary>
    public static string Pick(int index)
    {
        var i = ((index % Colors.Length) + Colors.Length) % Colors.Length;
        return Colors[i];
    }

    /// <summary>
    /// Choisit une couleur non encore utilisée par les membres existants.
    /// Si toutes sont prises, retombe sur la première (cycle).
    /// </summary>
    public static string PickNext(IEnumerable<string?> alreadyUsed)
    {
        var used = alreadyUsed.Where(c => !string.IsNullOrEmpty(c)).ToHashSet();
        foreach (var c in Colors)
        {
            if (!used.Contains(c)) return c;
        }
        return Colors[0];
    }
}
