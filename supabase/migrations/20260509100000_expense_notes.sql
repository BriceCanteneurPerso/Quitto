-- Ajoute une colonne `notes` (texte libre, optionnel) sur les dépenses pour
-- noter le contexte ("dîner avec les voisins", liste des plats, justificatif…).
-- Idempotent.

alter table public.expenses
    add column if not exists notes text;
