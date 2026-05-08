# Quitto

Petit Tricount-équivalent — répartition de dépenses entre amis, sans compte.
Blazor WebAssembly + MudBlazor, backend Supabase free tier, déployé sur GitHub Pages.

## Modèle de sécu

Pas d'auth utilisateur. L'`id` UUID du groupe est le secret : qui connaît le lien
(`/g/<uuid>`) peut lire et écrire ce groupe. La RLS Supabase gate chaque table
sur le header `x-group-id` envoyé par le client.

C'est le modèle que Tricount lui-même utilise — un UUID v4, c'est 122 bits
d'entropie, donc inguessable en pratique.

## Démarrer en local

### 1. Créer le projet Supabase

1. Aller sur https://supabase.com → Sign in → **New project**.
2. Région : Europe (Frankfurt) ou la plus proche.
3. Schéma : deux options
   - **Auto** (recommandé) : activer la GitHub integration depuis le dashboard Supabase
     puis pusher la branche. Les fichiers de [`supabase/migrations/`](supabase/migrations/)
     sont appliqués automatiquement.
   - **Manuel** : copier-coller le dernier fichier de
     [`supabase/migrations/`](supabase/migrations/) dans **SQL Editor → Run**.
4. **Settings → API** → noter le **Project URL** et la **publishable key**
   (`sb_publishable_…`). La publishable key est faite pour être en clair côté client,
   la sécurité réelle vient des policies RLS.

### 2. Configurer Quitto

Éditer [`wwwroot/appsettings.json`](wwwroot/appsettings.json) avec les deux
valeurs ci-dessus. La clé `anon` est faite pour être publique côté client —
la sécu réelle est portée par les policies RLS.

### 3. Lancer

```pwsh
dotnet run
```

Ouvre `https://localhost:5173`.

## Déploiement GitHub Pages

Le workflow [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml)
publie automatiquement à chaque push sur `main` :

- `dotnet publish` en standalone Blazor WASM
- patch du `<base href>` pour `/<repo>/`
- fallback SPA (`404.html` → `index.html`)
- `.nojekyll` pour préserver `_framework/`

Avant le premier déploiement : **Settings → Pages → Source = GitHub Actions**.

## Structure

```
supabase/
  config.toml             # project_id pour la GitHub integration
  migrations/             # appliquées auto par la GitHub integration
    20260508120000_init.sql
Models/               # POCOs sérialisés vers/depuis PostgREST
Lib/
  SupabaseClient.cs   # wrapper HttpClient autour de PostgREST + x-group-id
  GroupSession.cs     # état du groupe courant (id du groupe = header)
  BalanceService.cs   # calcul des soldes + remboursements simplifiés
  RecentGroupsService.cs  # localStorage : groupes vus sur cet appareil
Pages/
  Home.razor          # /
  CreateGroup.razor   # /new
  GroupPage.razor     # /g/{id}  (renommé pour éviter la collision avec Models.Group)
  AddExpense.razor    # /g/{id}/expense/new
  AddTransfer.razor   # /g/{id}/transfer/new
Layout/MainLayout.razor
```

## Périmètre v1

- Création de groupes
- Membres saisis librement (pas de compte)
- Dépenses avec split égal entre les participants choisis
- Soldes par membre
- Suggestion de remboursements simplifiés (algo glouton)
- Remboursements manuels enregistrés

## Hors v1 (idées)

- Parts custom (pourcentages, parts inégales)
- Multi-devises avec conversion
- Realtime (Supabase Realtime channel) pour MAJ live entre potes
- Photos de tickets
- PWA installable (manifest présent mais pas de service worker pour l'instant)
