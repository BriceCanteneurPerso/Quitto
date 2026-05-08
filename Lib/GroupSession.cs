namespace Quitto.Lib;

/// <summary>
/// Détient l'identifiant du groupe courant et son PIN éventuel. SupabaseClient
/// les lit pour poser les headers `x-group-id` et `x-share-pin` sur chaque
/// requête PostgREST.
///
/// Le pattern : la page /g/{id}[/{pin}] appelle <see cref="Set"/> au démarrage.
/// Pas de Clear automatique : la page suivante écrase la valeur (cf. la note dans
/// GroupPage sur la race Blazor entre new-page-init et old-page-dispose).
/// </summary>
public class GroupSession
{
    public Guid? GroupId { get; private set; }
    public string? SharePin { get; private set; }

    public void Set(Guid id, string? pin = null)
    {
        GroupId = id;
        SharePin = string.IsNullOrWhiteSpace(pin) ? null : pin;
    }

    public void Clear()
    {
        GroupId = null;
        SharePin = null;
    }
}
