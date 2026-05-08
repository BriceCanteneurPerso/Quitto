-- =============================================================================
-- Fix : la policy UPDATE sur groups levait `new row violates RLS policy`
-- au moment d'activer un PIN. Hypothèse : interaction étrange entre la fonction
-- helper `current_group_id()` (STABLE) et le contexte WITH CHECK de PATCH.
-- On recrée les policies sur groups avec des checks INLINE qui contournent
-- la fonction. Les tables enfants gardent `is_authorized_for` car ça marche.
--
-- Idempotent.
-- =============================================================================

drop policy if exists groups_select_by_header on public.groups;
drop policy if exists groups_insert_by_header on public.groups;
drop policy if exists groups_update_by_header on public.groups;

create policy groups_select_by_header on public.groups
  for select to anon
  using (
    id = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-group-id', ''), '')::uuid
    and (
      share_pin is null
      or share_pin = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-share-pin', ''), '')
    )
  );

-- INSERT : on ne peut pas vérifier le pin (le row n'existe pas encore).
create policy groups_insert_by_header on public.groups
  for insert to anon
  with check (
    id = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-group-id', ''), '')::uuid
  );

-- UPDATE :
--   USING : il faut connaître l'ancien pin pour pouvoir update.
--   WITH CHECK : le nouveau row ne contraint que l'id (sinon impossible de
--                changer le pin — le nouveau diffère par essence du header).
create policy groups_update_by_header on public.groups
  for update to anon
  using (
    id = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-group-id', ''), '')::uuid
    and (
      share_pin is null
      or share_pin = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-share-pin', ''), '')
    )
  )
  with check (
    id = nullif(coalesce((current_setting('request.headers', true)::json) ->> 'x-group-id', ''), '')::uuid
  );

-- ----------------------------------------------------------------------------
-- RPC de diagnostic (à conserver en attendant la stabilité — supprimable plus tard).
-- À appeler depuis le client via :
--   fetch(URL+"/rest/v1/rpc/quitto_debug", {
--     method: "POST",
--     headers: { apikey, "x-group-id": "...", "x-share-pin": "...",
--                "Content-Type": "application/json" }
--   }).then(r => r.json()).then(console.log)
-- Pour vérifier ce que PostgREST voit côté headers.
-- ----------------------------------------------------------------------------

create or replace function public.quitto_debug()
returns json
language sql stable
as $$
  select json_build_object(
    'group_id_header',  current_setting('request.headers', true)::json ->> 'x-group-id',
    'share_pin_header', current_setting('request.headers', true)::json ->> 'x-share-pin',
    'group_id_parsed',  public.current_group_id(),
    'share_pin_parsed', public.current_share_pin()
  );
$$;
grant execute on function public.quitto_debug() to anon;
