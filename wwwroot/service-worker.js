// Dev mode : aucun caching. On laisse le navigateur fetch normalement.
// En production, c'est `service-worker.published.js` (généré au publish) qui
// prend la place et active la mise en cache offline.
self.addEventListener('fetch', () => { });
