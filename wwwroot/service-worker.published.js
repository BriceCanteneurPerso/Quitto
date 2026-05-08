// Service worker production : met en cache les assets listés dans
// `service-worker-assets.js` (généré au publish), permet l'usage offline et
// accélère les chargements suivants. Pattern standard Microsoft pour Blazor WASM.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'quitto-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [
    /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/,
    /\.css$/, /\.woff$/, /\.woff2$/, /\.png$/, /\.jpe?g$/, /\.svg$/,
    /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/
];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall(event) {
    console.info('Quitto SW: install');
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(p => p.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(p => p.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(c => c.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Quitto SW: activate');
    // Purge les anciens caches d'autres versions.
    const keys = await caches.keys();
    await Promise.all(keys
        .filter(k => k.startsWith(cacheNamePrefix) && k !== cacheName)
        .map(k => caches.delete(k)));
}

async function onFetch(event) {
    const request = event.request;

    // Ne JAMAIS cacher les requêtes Supabase / API externes : elles doivent
    // toujours frapper le réseau pour fetch des données fraîches.
    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return fetch(request);
    }

    let cachedResponse = null;
    if (request.method === 'GET') {
        // Pour les navigations (URL changes), on sert toujours index.html depuis
        // le cache → permet au routeur Blazor de prendre le relais offline.
        const shouldServeIndexHtml = request.mode === 'navigate';
        const cacheRequest = shouldServeIndexHtml ? 'index.html' : request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(cacheRequest);
    }
    return cachedResponse || fetch(request);
}
