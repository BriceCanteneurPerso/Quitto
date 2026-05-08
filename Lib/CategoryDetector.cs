using System.Globalization;
using System.Text;
using MudBlazor;

namespace Quitto.Lib;

/// <summary>
/// Catégorie d'une dépense. Stockée en DB sous forme de clé courte (string),
/// résolue en métadonnées (label, icône MudBlazor, couleur) au rendu.
/// </summary>
public record QuittoCategory(string Key, string Label, string Icon, string Color);

/// <summary>
/// Détection automatique de catégorie depuis le texte de la description.
/// Pas de ML : juste une table de mots-clés normalisés (lower + sans accents).
/// Si plusieurs catégories matchent, la première occurrence dans l'ordre des
/// mots gagne (cohérent avec la perception humaine "il a écrit X en premier").
/// </summary>
public static class CategoryDetector
{
    public static readonly QuittoCategory Other =
        new("other", "Autre", Icons.Material.Filled.Receipt, "#90a4ae");

    public static readonly QuittoCategory[] All =
    {
        new("restaurant", "Restaurant", Icons.Material.Filled.Restaurant,    "#388e3c"),
        new("groceries",  "Courses",    Icons.Material.Filled.ShoppingCart,  "#f57c00"),
        new("transport",  "Transport",  Icons.Material.Filled.DirectionsCar, "#1976d2"),
        new("travel",     "Voyage",     Icons.Material.Filled.Flight,        "#0097a7"),
        new("lodging",    "Logement",   Icons.Material.Filled.Hotel,         "#7b1fa2"),
        new("leisure",    "Loisirs",    Icons.Material.Filled.Celebration,   "#c2185b"),
        new("coffee",     "Café",       Icons.Material.Filled.Coffee,        "#6d4c41"),
        new("drinks",     "Boissons",   Icons.Material.Filled.WineBar,       "#d32f2f"),
        new("health",     "Santé",      Icons.Material.Filled.LocalHospital, "#00897b"),
        new("gifts",      "Cadeaux",    Icons.Material.Filled.CardGiftcard,  "#e91e63"),
        Other
    };

    // Mot-clé normalisé (sans accents, lowercase) → clé de catégorie.
    private static readonly Dictionary<string, string> KeywordToKey = new()
    {
        // restaurant
        ["resto"]=        "restaurant", ["restaurant"]= "restaurant", ["restau"]=    "restaurant",
        ["pizza"]=        "restaurant", ["pizzeria"]=   "restaurant", ["sushi"]=     "restaurant",
        ["kebab"]=        "restaurant", ["burger"]=     "restaurant", ["brunch"]=    "restaurant",
        ["diner"]=        "restaurant", ["dejeuner"]=   "restaurant", ["midi"]=      "restaurant",
        ["brasserie"]=    "restaurant", ["bistrot"]=    "restaurant", ["cantine"]=   "restaurant",
        ["mcdo"]=         "restaurant", ["mcdonald"]=   "restaurant", ["mcdonalds"]= "restaurant",
        ["tacos"]=        "restaurant", ["thai"]=       "restaurant", ["chinois"]=   "restaurant",
        ["italien"]=      "restaurant", ["japonais"]=   "restaurant",
        // groceries
        ["courses"]=      "groceries",  ["course"]=     "groceries",  ["supermarche"]="groceries",
        ["carrefour"]=    "groceries",  ["leclerc"]=    "groceries",  ["monoprix"]=  "groceries",
        ["auchan"]=       "groceries",  ["lidl"]=       "groceries",  ["intermarche"]="groceries",
        ["picard"]=       "groceries",  ["casino"]=     "groceries",  ["drive"]=     "groceries",
        ["franprix"]=     "groceries",  ["super"]=      "groceries",  ["epicerie"]=  "groceries",
        // transport
        ["uber"]=         "transport",  ["taxi"]=       "transport",  ["train"]=     "transport",
        ["sncf"]=         "transport",  ["metro"]=      "transport",  ["bus"]=       "transport",
        ["essence"]=      "transport",  ["gazole"]=     "transport",  ["peage"]=     "transport",
        ["parking"]=      "transport",  ["voiture"]=    "transport",  ["velo"]=      "transport",
        ["blabla"]=       "transport",  ["blablacar"]=  "transport",  ["ratp"]=      "transport",
        ["scooter"]=      "transport",  ["tgv"]=        "transport",  ["ouigo"]=     "transport",
        // travel
        ["vol"]=          "travel",     ["avion"]=      "travel",     ["airfrance"]= "travel",
        ["ryanair"]=      "travel",     ["easyjet"]=    "travel",     ["aeroport"]=  "travel",
        // lodging
        ["hotel"]=        "lodging",    ["airbnb"]=     "lodging",    ["hostel"]=    "lodging",
        ["gite"]=         "lodging",    ["location"]=   "lodging",    ["loyer"]=     "lodging",
        ["appart"]=       "lodging",    ["chambre"]=    "lodging",    ["auberge"]=   "lodging",
        // leisure
        ["bar"]=          "leisure",    ["cinema"]=     "leisure",    ["theatre"]=   "leisure",
        ["concert"]=      "leisure",    ["musee"]=      "leisure",    ["billet"]=    "leisure",
        ["festival"]=     "leisure",    ["escape"]=     "leisure",    ["bowling"]=   "leisure",
        ["karaoke"]=      "leisure",    ["soiree"]=     "leisure",    ["club"]=      "leisure",
        ["jeu"]=          "leisure",    ["jeux"]=       "leisure",
        // coffee
        ["cafe"]=         "coffee",     ["starbucks"]=  "coffee",     ["expresso"]=  "coffee",
        ["latte"]=        "coffee",     ["cappuccino"]= "coffee",     ["frappuccino"]="coffee",
        // drinks
        ["vin"]=          "drinks",     ["biere"]=      "drinks",     ["cocktail"]=  "drinks",
        ["alcool"]=       "drinks",     ["champagne"]=  "drinks",     ["apero"]=     "drinks",
        ["aperitif"]=     "drinks",     ["bouteille"]=  "drinks",     ["pinte"]=     "drinks",
        // health
        ["pharmacie"]=    "health",     ["medecin"]=    "health",     ["dentiste"]=  "health",
        ["doctolib"]=     "health",     ["kine"]=       "health",     ["medicament"]="health",
        // gifts
        ["cadeau"]=       "gifts",      ["cadeaux"]=    "gifts",      ["anniversaire"]="gifts",
        ["fleurs"]=       "gifts",      ["bouquet"]=    "gifts",
    };

    /// <summary>Détecte la catégorie depuis la description. Retourne <see cref="Other"/> si rien ne matche.</summary>
    public static QuittoCategory Detect(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return Other;
        var normalized = Normalize(description);
        var words = normalized.Split(new[] { ' ', '-', '\'', '\t', '\n', ',', '.', '!', '?', '(', ')', ':', ';', '"' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var w in words)
        {
            if (KeywordToKey.TryGetValue(w, out var key))
            {
                return All.First(c => c.Key == key);
            }
        }
        return Other;
    }

    public static QuittoCategory FromKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return Other;
        return All.FirstOrDefault(c => c.Key == key) ?? Other;
    }

    private static string Normalize(string s)
    {
        var lower = s.ToLowerInvariant();
        var sb = new StringBuilder(capacity: lower.Length);
        foreach (var c in lower.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
