-- =============================================================================
-- Hardening : PIN optionnel par groupe + soft-delete pour dépenses et transferts
--
-- Modèle d'auth durci :
--  - Groupe peut avoir un `share_pin` non-null. Si présent, RLS exige aussi
--    un header `x-share-pin` matching pour SELECT/UPDATE.
--  - Si `share_pin is null`, comportement v1 inchangé (rétro-compat).
--  - Le client envoie les deux headers (`x-group-id` + `x-share-pin` si connu).
--
-- Soft-delete : `expenses.deleted_at` et `transfers.deleted_at`. Le client filtre
-- côté requête (`?deleted_at=is.null` ou `not.is.null`). RLS ne fait pas le filtre
-- pour permettre à la corbeille de lire ce qui est marqué deleted.
--
-- Idempotent.
-- =============================================================================

-- 1) Schéma : nouvelles colonnes
alter table public.groups    add column if not exists share_pin   text;
alter table public.expenses  add column if not exists deleted_at  timestamptz;
alter table public.transfers add column if not exists deleted_at  timestamptz;

create index if not exists expenses_deleted_idx  on public.expenses(deleted_at);
create index if not exists transfers_deleted_idx on public.transfers(deleted_at);

-- 2) Helpers RLS

create or replace function public.current_share_pin()
returns text
language sql stable
as $$
  select nullif(
    coalesce(
      (current_setting('request.headers', true)::json) ->> 'x-share-pin',
      ''
    ),
    ''
  );
$$;
grant execute on function public.current_share_pin() to anon;

-- True si le caller a le bon group_id ET (groupe sans pin OR pin matche).
create or replace function public.is_authorized_for(p_group_id uuid)
returns boolean
language sql stable
as $$
  select exists (
    select 1 from public.groups g
    where g.id = p_group_id
      and g.id = public.current_group_id()
      and (g.share_pin is null or g.share_pin = public.current_share_pin())
  );
$$;
grant execute on function public.is_authorized_for(uuid) to anon;

-- 3) Update policies sur groups (introduit la check du pin sur SELECT/UPDATE)

drop policy if exists groups_select_by_header on public.groups;
drop policy if exists groups_insert_by_header on public.groups;
drop policy if exists groups_update_by_header on public.groups;

create policy groups_select_by_header on public.groups
  for select to anon
  using (
    id = public.current_group_id()
    and (share_pin is null or share_pin = public.current_share_pin())
  );

-- INSERT : on ne peut pas vérifier le pin (le row est en train d'être créé).
-- L'id doit matcher le header, comme avant. Le pin créé est ce que le client
-- met dans le row (ou null s'il n'en active pas).
create policy groups_insert_by_header on public.groups
  for insert to anon
  with check (id = public.current_group_id());

-- UPDATE :
--   USING : il faut connaître l'ancien pin pour pouvoir update (sinon n'importe
--           qui pourrait reset le pin et bypass).
--   WITH CHECK : le nouveau row n'a pas de contrainte sur le pin (sinon on
--                ne pourrait JAMAIS changer le pin — le nouveau diffère du header).
create policy groups_update_by_header on public.groups
  for update to anon
  using (
    id = public.current_group_id()
    and (share_pin is null or share_pin = public.current_share_pin())
  )
  with check (id = public.current_group_id());

-- 4) Update policies sur les tables enfants : utilisent is_authorized_for pour
--    propager la check du pin.

drop policy if exists members_all_by_header on public.members;
create policy members_all_by_header on public.members
  for all to anon
  using (public.is_authorized_for(group_id))
  with check (public.is_authorized_for(group_id));

drop policy if exists expenses_all_by_header on public.expenses;
create policy expenses_all_by_header on public.expenses
  for all to anon
  using (public.is_authorized_for(group_id))
  with check (public.is_authorized_for(group_id));

drop policy if exists transfers_all_by_header on public.transfers;
create policy transfers_all_by_header on public.transfers
  for all to anon
  using (public.is_authorized_for(group_id))
  with check (public.is_authorized_for(group_id));

drop policy if exists expense_participants_all_by_header on public.expense_participants;
create policy expense_participants_all_by_header on public.expense_participants
  for all to anon
  using (
    exists (select 1 from public.expenses e
            where e.id = expense_participants.expense_id
              and public.is_authorized_for(e.group_id))
  )
  with check (
    exists (select 1 from public.expenses e
            where e.id = expense_participants.expense_id
              and public.is_authorized_for(e.group_id))
  );
