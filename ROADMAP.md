# Quitto — Roadmap

Liste de fonctionnalités pour piocher dedans à la demande, pas un ordre de priorité figé. Chaque item :
- **S** = ≤ 2h (quick win)
- **M** = ½ à 1 journée
- **L** = > 1 journée (souvent : design à confirmer avant de coder)

---

## 1. Complétude UX (les manques visibles aujourd'hui)

- **S — Renommer un membre.** Bouton crayon dans l'onglet Membres, PATCH `members?id=eq.X`.
- **S — Supprimer un membre.** Refus si le membre est payeur ou participant d'une dépense / d'un transfert (UI explicite : "il faut d'abord retirer X de ces dépenses").
- **S — Éditer un transfert.** Même pattern que l'édition de dépense déjà en place.
- **S — Oublier un tricount sur la home.** Icône poubelle sur chaque ligne récente → `RecentGroupsService.ForgetAsync`. N'efface rien côté serveur.
- **S — Remplacer les `alert/confirm/prompt` natifs par MudDialog.** Meilleure cohérence visuelle (concerne `Home.razor` et les confirmations de suppression dans `GroupPage.razor`).
- **S — Empty states soignés.** Illustration + CTA quand le groupe n'a aucune dépense.
- **M — Settle-up shortcut.** Clic sur un remboursement suggéré (onglet Soldes) → pré-remplit `AddTransfer` avec from/to/montant. Réduit la friction du remboursement réel.
- **M — Recherche / filtrage des dépenses.** Champ texte qui filtre par description, picker de membre payeur, range de dates.

---

## 2. Saisie de dépense plus riche

- **S — Catégories d'expense.** La colonne `category` est déjà en base. Il faut juste un select dans `AddExpense` + un chip coloré dans la liste. Liste préfixée (Restaurant, Courses, Transport, Logement, Loisirs, Autre).
- **M — Split custom.** Parts inégales (1+1+2), pourcentages, ou montants exacts. Schéma à adapter : ajouter `share` (numeric) ou `share_weight` (numeric) à `expense_participants`. Designer l'UI en premier — c'est là que la complexité se concentre.
- **M — Notes longues sur une dépense.** Champ `notes text` en base, MudTextField multi-line dans le form.
- **L — Photo de ticket.** Stockage dans Supabase Storage (1 GB free), URL dans `expenses.receipt_url`. RLS sur le bucket : même header `x-group-id`. Compression côté client avant upload.
- **L — Dépenses récurrentes.** Une dépense flagguée "récurrence mensuelle" génère automatiquement les occurrences. Logique côté client (au démarrage du groupe : matérialiser les manquantes), ou Postgres function planifiée. Côté client est plus simple.

---

## 3. Multi-devises

- **L — Devise par dépense.** Aujourd'hui le groupe a une seule devise. Permettre `expenses.currency` + `fx_rate` (taux de change manuel saisi à la dépense). Le calcul des soldes se fait dans la devise de base du groupe. Audit OK car on stocke le taux. Réfléchir à l'UI pour ne pas l'imposer aux groupes mono-devise.

---

## 4. Synthèse & export

- **M — Onglet Stats.** Total dépensé du groupe, par membre, par catégorie, par mois. Charts MudBlazor (`MudChart`).
- **M — Filtre temporel sur les soldes.** "Solde sur juillet 2026" — recompute des soldes sur une fenêtre datée.
- **S — Export CSV des dépenses.** Bouton dans GroupPage, génère un CSV côté client (plus de blob URL → download).

---

## 5. Realtime & multi-utilisateur live

- **M — Subscription Realtime.** Supabase Realtime via WebSocket. Quand un pote ajoute une dépense, l'app rafraîchit automatiquement. Schéma déjà compatible (publication par défaut sur les tables `public`).
- **M — Optimistic UI.** Au lieu d'attendre la réponse Supabase, on affiche immédiatement la dépense avec un état "en cours", on rollback si échec. Améliore le ressenti sur réseau lent.
- **L — Présence.** Voir qui est connecté en temps réel sur le groupe (avatars en haut de page). Bonus social, marginal.

---

## 6. Partage & onboarding

- **S — QR code du lien.** Bouton "QR" dans la share-banner → MudDialog avec QR. Phone-to-phone trivial. Lib JS comme `qrious` ou côté C# via SkiaSharp.
- **S — Web Share API.** Sur mobile, bouton "Partager" → menu natif iOS/Android (envoie le lien sur WhatsApp/SMS/Mail).
- **M — Open Graph preview.** Méta-tags dans `index.html` pour qu'un lien partagé s'affiche joliment dans iMessage/WhatsApp/Slack. Tricky avec Blazor WASM (le HTML est statique avant l'hydratation), mais tag de base + image = suffisant.

---

## 7. Hardening sécu (quand tu sentiras le besoin)

- **M — PIN par groupe.** Colonne `share_pin` (10 chars random) dans `groups`. Lien partageable devient `/g/{id}-{pin}`. RLS vérifie `pin = header_pin` en plus de l'id. Empêche le détournement opportuniste de la base par scraping de la publishable key sur GitHub public.
- **L — Captcha sur création de groupe.** Cloudflare Turnstile (gratuit, sans cookie). Bloque le spam automatisé. Pertinent uniquement si tu vois de l'abuse réel.
- **S — Backup JSON.** Bouton "Exporter ce tricount" qui télécharge un JSON complet (groupe + membres + expenses + participants + transfers). Tampon hors-Supabase.
- **S — Restore JSON.** L'inverse, avec un id généré côté client + INSERT massif. Permet de migrer un groupe entre projets Supabase.
- **M — Soft-delete + corbeille.** Plutôt que `DELETE`, ajouter `deleted_at timestamptz`. UI : corbeille dans Settings du groupe avec bouton "Restaurer". Évite les drames quand un pote supprime par erreur.
- **M — Audit log basique.** Table `audit_log(group_id, ts, actor_label, action, target_id)`. "Actor" = un nom libre saisi à la 1re visite ("Tu es : ___"). Pas un user vrai mais ça permet de retrouver qui a fait quoi.

---

## 8. PWA / offline

- **M — Service worker pour les assets.** Cache `_framework/`, MudBlazor, fonts. App ouvrable sans réseau (lecture du cache). Pattern coffee est reproductible.
- **S — Add-to-home-screen.** Manifest est déjà là, ajouter un prompt subtil "installer l'app" sur mobile.
- **L — Écritures offline avec queue.** Sauf scénario forte usage en métro, c'est un gros morceau (replay, conflits, ordering) — probablement pas la peine pour Quitto.

---

## 9. UI polish

- **S — Format locale FR.** `1 234,56 €` au lieu de `1234.56 €`. `CultureInfo.GetCultureInfo("fr-FR")` dans Program.cs.
- **S — Couleur custom par membre.** Le champ `color` existe en base, on le sélectionne au create-member et on l'utilise sur les avatars / chips.
- **M — Dark mode.** Pattern coffee → `ThemeService` qui écoute `prefers-color-scheme`, toggle manuel dans Settings.
- **M — i18n FR/EN.** Pattern coffee (`LocalizationService` + dictionnaires JSON). Pas critique tant qu'on est seuls francophones.
- **M — Skeletons de chargement.** Remplacer les `MudProgressCircular` par des skeletons (`MudSkeleton`). Perçu plus rapide.

---

## 10. Spéculatif (probable-jamais, mais notons-les)

- **L+ — Vrai auth utilisateur.** Comptes Quitto (magic link), invitations explicites, fin de l'anon-via-UUID. Casse l'UX simple actuelle, à n'envisager que si Quitto sort du cercle entre potes.
- **L+ — App native (Capacitor).** Wrapper iOS/Android autour de la PWA. Notif push, intégration partage native. Pertinent uniquement si usage récurrent intense.
- **L+ — Liaison entre tricounts.** Une dépense partagée entre deux groupes. Compliqué côté modèle, casse les invariants RLS — probablement pas.

---

## Méta

- Quand tu veux attaquer un item, dis-moi son numéro / titre, je code.
- Cette roadmap est vivante : je l'édite quand on ajoute, supprime ou réordonne. À toi de m'indiquer si un item devient priorité 1 ou tombe à la trappe.
