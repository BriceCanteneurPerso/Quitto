// Composition root du host Blazor WebAssembly Quitto.
// Tout est Scoped : en WASM mono-utilisateur, Scoped == Singleton à l'échelle de l'onglet.

using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Quitto;
using Quitto.Lib;

// Format des nombres et dates côté FR ("1 234,56 €", dates dd/MM/yyyy).
// On ne touche PAS à la culture invariant pour les sérialisations Supabase :
// SupabaseClient utilise toujours InvariantCulture explicitement pour les
// décimaux et DateOnly (cf. JsonSerializerOptions).
var fr = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture   = fr;
CultureInfo.DefaultThreadCurrentUICulture = fr;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient pointe sur la base href (pour fetch des assets statiques uniquement).
// SupabaseClient instancie son propre HttpClient (cf. Lib/SupabaseClient.cs).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

// Lecture de la config Supabase (URL + anon key publique). Les valeurs sont
// committed in clear in wwwroot/appsettings.json — c'est la convention Supabase
// pour la clé "anon", la sécurité réelle est portée par les policies RLS.
var supabaseConfig = builder.Configuration.GetSection("Supabase").Get<SupabaseConfig>()
    ?? new SupabaseConfig();
builder.Services.AddSingleton(supabaseConfig);

// Services applicatifs.
builder.Services.AddScoped<GroupSession>();          // Détient le group id courant (header x-group-id).
builder.Services.AddScoped<SupabaseClient>();        // Wrapper PostgREST.
builder.Services.AddScoped<RecentGroupsService>();   // localStorage : liste des groupes visités.
builder.Services.AddScoped<BalanceService>();        // Calcul des soldes + remboursements simplifiés.

await builder.Build().RunAsync();
