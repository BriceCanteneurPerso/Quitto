-- Cleanup : retire la RPC debug `quitto_whoami` qui avait été ajoutée pour
-- diagnostiquer la transmission du header `x-group-id` à PostgREST.
-- Idempotent.

drop function if exists public.quitto_whoami();
