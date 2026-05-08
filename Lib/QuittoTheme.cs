using MudBlazor;

namespace Quitto.Lib;

/// <summary>
/// Palette MudBlazor light + dark de l'app, alignée avec les variables CSS de
/// `wwwroot/css/app.css`. Toute modification de couleurs ici doit se refléter
/// dans le CSS pour garder la cohérence.
///
/// Direction : moderne minimal-chaleureux. Vert émeraude profond comme primaire
/// (cohérent avec l'icône Q et le branding), or chaud comme accent secondaire,
/// breathing room généreux via border-radius 12px, typo Inter partout.
/// </summary>
public static class QuittoTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary : vert émeraude. Utilisé pour AppBar, FAB, boutons primaires,
            // chips actives, focus rings.
            Primary             = "#1f6f54",
            PrimaryContrastText = "#ffffff",

            // Secondary : or chaud, pour accents (montants forts, badges).
            Secondary             = "#c8901c",
            SecondaryContrastText = "#ffffff",

            // Tertiary : brun sablé, pour avatars de membres dénués de couleur custom.
            Tertiary             = "#6d4c41",
            TertiaryContrastText = "#ffffff",

            Info    = "#1976d2",
            Success = "#1f6f54",
            Warning = "#e09614",
            Error   = "#c2333d",

            AppbarBackground = "#1f6f54",
            AppbarText       = "#ffffff",

            Background     = "#f6faf8",
            Surface        = "#ffffff",
            DrawerBackground = "#ffffff",

            TextPrimary    = "#1a2f25",
            TextSecondary  = "#5a6b62",
            TextDisabled   = "#9aaaa3",
            ActionDefault  = "#5a6b62",
            ActionDisabled = "#c5d3cc",

            Divider      = "#e5efea",
            DividerLight = "#f0f6f3",
            LinesDefault = "#dde8e2",
            LinesInputs  = "#c8d6cf",

            // Hover/focus subtils sur accent vert.
            HoverOpacity = 0.06
        },
        PaletteDark = new PaletteDark
        {
            Primary             = "#4caf85",
            PrimaryContrastText = "#0e1a14",

            Secondary             = "#f0c552",
            SecondaryContrastText = "#0e1a14",

            Tertiary             = "#bcaaa4",
            TertiaryContrastText = "#0e1a14",

            Info    = "#64b5f6",
            Success = "#4caf85",
            Warning = "#ffb74d",
            Error   = "#ef6b6b",

            AppbarBackground = "#0f231a",
            AppbarText       = "#ffffff",

            Background     = "#0c1612",
            Surface        = "#16261e",
            DrawerBackground = "#0c1612",

            TextPrimary    = "#ffffff",
            TextSecondary  = "#a8bfb5",
            TextDisabled   = "#6c7e76",
            ActionDefault  = "#a8bfb5",
            ActionDisabled = "#3d4f47",

            Divider      = "#1d3127",
            DividerLight = "#172620",
            LinesDefault = "#1d3127",
            LinesInputs  = "#28403a",

            Dark         = "#06120c",
            DarkLighten  = "#1f3a30",

            HoverOpacity = 0.10
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "sans-serif" },
                FontSize   = "0.95rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            H1 = new H1Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "2.5rem", FontWeight = "800", LineHeight = "1.15", LetterSpacing = "-0.02em" },
            H2 = new H2Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "2rem",   FontWeight = "800", LineHeight = "1.2",  LetterSpacing = "-0.02em" },
            H3 = new H3Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "1.6rem", FontWeight = "700", LineHeight = "1.25" },
            H4 = new H4Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "1.4rem", FontWeight = "700", LineHeight = "1.3" },
            H5 = new H5Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "1.2rem", FontWeight = "700", LineHeight = "1.35" },
            H6 = new H6Typography { FontFamily = new[] { "Inter", "system-ui", "sans-serif" }, FontSize = "1.05rem", FontWeight = "700", LineHeight = "1.4" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "600", FontSize = "1rem", LineHeight = "1.5" },
            Subtitle2 = new Subtitle2Typography { FontWeight = "600", FontSize = "0.875rem", LineHeight = "1.5" },
            Body1 = new Body1Typography { FontSize = "0.95rem", LineHeight = "1.5" },
            Body2 = new Body2Typography { FontSize = "0.85rem", LineHeight = "1.5" },
            Button = new ButtonTypography { FontWeight = "600", LetterSpacing = "0.01em", TextTransform = "none" },
            Caption = new CaptionTypography { FontSize = "0.78rem", LineHeight = "1.4" },
            Overline = new OverlineTypography { FontSize = "0.72rem", LineHeight = "1.4", LetterSpacing = "0.08em" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px"
        }
    };
}
