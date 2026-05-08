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
    }
};
