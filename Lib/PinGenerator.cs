using System.Security.Cryptography;

namespace Quitto.Lib;

/// <summary>
/// Génère des PIN URL-safe pour le partage de groupes. 10 caractères dans un
/// alphabet de 32 symboles (sans 0/O/1/I/L pour ambiguïté visuelle) =
/// ~ 50 bits d'entropie, soit ~10^15 combinaisons. Combiné avec l'UUID du
/// groupe (122 bits), bloque tout scraping opportuniste de la publishable key.
/// </summary>
public static class PinGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 10)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }
        return new string(chars);
    }
}
