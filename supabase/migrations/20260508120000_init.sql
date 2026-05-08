-- =============================================================================
-- Quitto — initial schema migration.
-- Auth model: "secret URL". The group's UUID id IS the secret token.
-- Clients must send header `x-group-id: <id>` on every PostgREST request;
-- RLS enforces match on every table.
--
-- Idempotent — safe to re-apply. The Supabase GitHub integration will record
-- this file in `supabase_migrations.schema_migrations` once applied.
-- =============================================================================

create extension if not exists pgcrypto;

-- -----------------------------------------------------------------------------
-- Tables
-- -----------------------------------------------------------------------------

create table if not exists public.groups (
  id         uuid primary key default gen_random_uuid(),
  name       text not null check (length(trim(name)) between 1 and 80),
  currency   text not null default 'EUR' check (length(currency) between 1 and 8),
  created_at timestamptz not null default now()
);

create table if not exists public.members (
  id         uuid primary key default gen_random_uuid(),
  group_id   uuid not null references public.groups(id) on delete cascade,
  name       text not null check (length(trim(name)) between 1 and 40),
  color      text,
  created_at timestamptz not null default now()
);
create index if not exists members_group_idx on public.members(group_id);

create table if not exists public.expenses (
  id          uuid primary key default gen_random_uuid(),
  group_id    uuid not null references public.groups(id) on delete cascade,
  payer_id    uuid not null references public.members(id),
  amount      numeric(12,2) not null check (amount > 0),
  description text not null check (length(trim(description)) between 1 and 120),
  category    text,
  paid_at     date not null default current_date,
  created_at  timestamptz not null default now()
);
create index if not exists expenses_group_idx on public.expenses(group_id);

create table if not exists public.expense_participants (
  expense_id uuid not null references public.expenses(id) on delete cascade,
  member_id  uuid not null references public.members(id),
  primary key (expense_id, member_id)
);

create table if not exists public.transfers (
  id              uuid primary key default gen_random_uuid(),
  group_id        uuid not null references public.groups(id) on delete cascade,
  from_member_id  uuid not null references public.members(id),
  to_member_id    uuid not null references public.members(id),
  amount          numeric(12,2) not null check (amount > 0),
  paid_at         date not null default current_date,
  created_at      timestamptz not null default now(),
  check (from_member_id <> to_member_id)
);
create index if not exists transfers_group_idx on public.transfers(group_id);

-- -----------------------------------------------------------------------------
-- RLS — every operation must carry header `x-group-id` matching the row's group.
-- -----------------------------------------------------------------------------

alter table public.groups                enable row level security;
alter table public.members               enable row level security;
alter table public.expenses              enable row level security;
alter table public.expense_participants  enable row level security;
alter table public.transfers             enable row level security;

create or replace function public.current_group_id()
returns uuid
language sql
stable
as $$
  select nullif(
    coalesce(
      (current_setting('request.headers', true)::json) ->> 'x-group-id',
      ''
    ),
    ''
  )::uuid;
$$;

grant execute on function public.current_group_id() to anon;

-- groups: id must match header for INSERT/SELECT/UPDATE.
drop policy if exists groups_select_by_header on public.groups;
drop policy if exists groups_insert_by_header on public.groups;
drop policy if exists groups_update_by_header on public.groups;
create policy groups_select_by_header on public.groups
  for select to anon
  using (id = public.current_group_id());
create policy groups_insert_by_header on public.groups
  for insert to anon
  with check (id = public.current_group_id());
create policy groups_update_by_header on public.groups
  for update to anon
  using (id = public.current_group_id())
  with check (id = public.current_group_id());

drop policy if exists members_all_by_header on public.members;
create policy members_all_by_header on public.members
  for all to anon
  using (group_id = public.current_group_id())
  with check (group_id = public.current_group_id());

drop policy if exists expenses_all_by_header on public.expenses;
create policy expenses_all_by_header on public.expenses
  for all to anon
  using (group_id = public.current_group_id())
  with check (group_id = public.current_group_id());

drop policy if exists transfers_all_by_header on public.transfers;
create policy transfers_all_by_header on public.transfers
  for all to anon
  using (group_id = public.current_group_id())
  with check (group_id = public.current_group_id());

drop policy if exists expense_participants_all_by_header on public.expense_participants;
create policy expense_participants_all_by_header on public.expense_participants
  for all to anon
  using (
    exists (select 1 from public.expenses e
            where e.id = expense_participants.expense_id
              and e.group_id = public.current_group_id())
  )
  with check (
    exists (select 1 from public.expenses e
            where e.id = expense_participants.expense_id
              and e.group_id = public.current_group_id())
  );

