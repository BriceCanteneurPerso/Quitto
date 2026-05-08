// Petit helper JS pour Quitto :
// - dessin d'un QR code dans un <canvas> via la lib `qrious` (chargée dans index.html)
// - bouton de partage natif via Web Share API (mobile iOS/Android)
// - copie presse-papier (fallback pour Web Share absent)

window.quitto = {
    qrDraw: (canvas, value, size) => {
        if (!canvas || !window.QRious) return;
        new QRious({ element: canvas, value: value, size: size || 240, padding: 16 });
    },

    canShare: () => !!(navigator.share),

    share: async (title, text, url) => {
        if (!navigator.share) return false;
        try {
            await navigator.share({ title, text, url });
            return true;
        } catch (err) {
            // L'utilisateur a cancellé ou erreur (origin non sécurisé) : on ne propage pas.
            return false;
        }
    },

    copyToClipboard: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },

    // Déclenche un click sur un élément par id (pour ouvrir un input file caché).
    clickElement: (id) => {
        const el = document.getElementById(id);
        if (el) el.click();
    },

    // Préférence système : utilisateur en mode sombre ?
    prefersDark: () => window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches,

    // Déclenche le téléchargement d'un fichier depuis une chaîne (CSV, JSON, etc.)
    // côté client. Pas de roundtrip serveur, idéal pour Blazor WASM.
    downloadFile: (filename, content, mimeType) => {
        const blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        // setTimeout pour laisser le navigateur démarrer le DL avant de révoquer l'URL.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};
