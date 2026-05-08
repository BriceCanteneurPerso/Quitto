namespace Quitto.Lib;

/// <summary>
/// Détient l'identifiant du groupe courant. SupabaseClient le lit pour poser
/// le header `x-group-id` sur chaque requête PostgREST.
///
/// Le pattern : la page /g/{id} appelle <see cref="Set"/> au démarrage et
/// <see cref="Clear"/> au Dispose. À l'INSERT d'un nouveau groupe, on l'appelle
/// AVEC l'id généré côté client AVANT le POST, sinon RLS rejette
/// (cf. db/schema.sql, policy groups_insert_by_header).
/// </summary>
public class GroupSession
{
    public Guid? GroupId { get; private set; }

    public void Set(Guid id) => GroupId = id;
    public void Clear() => GroupId = null;
}
