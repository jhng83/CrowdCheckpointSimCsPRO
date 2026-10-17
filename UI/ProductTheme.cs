using System.Numerics;
using CrowdCheckpointSim.Visual;
using ImGuiNET;

namespace CrowdCheckpointSim.UI;

/// <summary>Professional operations-console theme for the product UI.</summary>
public static class ProductTheme
{
    private const float BoldFontSizePixels = 14f;
    private static ImFontPtr _boldFont;
    private static bool _hasBoldFont;
    private static float _appliedUiScale = 1f;

    /// <summary>
    /// Add readable platform-appropriate regular and bold UI fonts before the
    /// ImGui controller builds its font atlas.
    /// </summary>
    public static void ConfigureFonts()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        string? regularFontPath = FindRegularFontPath();
        ImFontPtr defaultFont = regularFontPath is null
            ? io.Fonts.AddFontDefault()
            : io.Fonts.AddFontFromFileTTF(regularFontPath, BoldFontSizePixels);

        string? boldFontPath = FindBoldFontPath();
        if (boldFontPath is null)
        {
            _boldFont = defaultFont;
            _hasBoldFont = false;
            return;
        }

        _boldFont = io.Fonts.AddFontFromFileTTF(boldFontPath, BoldFontSizePixels);
        _hasBoldFont = true;
    }

    /// <summary>Push the bold UI font when one is available on the current platform.</summary>
    public static bool PushBoldFont()
    {
        if (!_hasBoldFont)
        {
            return false;
        }

        ImGui.PushFont(_boldFont);
        return true;
    }

    /// <summary>Pop a font previously pushed by <see cref="PushBoldFont"/>.</summary>
    public static void PopBoldFont(bool wasPushed)
    {
        if (wasPushed)
        {
            ImGui.PopFont();
        }
    }

    /// <summary>Apply the product-wide ImGui style once after the controller is created.</summary>
    public static void Apply()
    {
        ImGui.StyleColorsDark();
        ImGuiStylePtr style = ImGui.GetStyle();

        style.WindowRounding = 0f;
        style.ChildRounding = 7f;
        style.FrameRounding = 5f;
        style.PopupRounding = 6f;
        style.ScrollbarRounding = 7f;
        style.GrabRounding = 5f;
        style.WindowPadding = new Vector2(14f, 14f);
        style.FramePadding = new Vector2(10f, 6f);
        style.ItemSpacing = new Vector2(8f, 8f);
        style.ItemInnerSpacing = new Vector2(8f, 6f);
        style.ScrollbarSize = 14f;
        style.FrameBorderSize = 1f;
        style.ChildBorderSize = 1f;

        style.Colors[(int)ImGuiCol.Text] = ProfessionalPalette.PrimaryText;
        style.Colors[(int)ImGuiCol.TextDisabled] = ProfessionalPalette.SecondaryText;
        style.Colors[(int)ImGuiCol.WindowBg] = ProfessionalPalette.AppBackground;
        style.Colors[(int)ImGuiCol.ChildBg] = ProfessionalPalette.SidebarBackground;
        style.Colors[(int)ImGuiCol.PopupBg] = ProfessionalPalette.SidebarBackground;
        style.Colors[(int)ImGuiCol.Border] = ProfessionalPalette.Border;
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);

        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.10f, 0.14f, 0.21f, 1f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.13f, 0.19f, 0.29f, 1f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.15f, 0.23f, 0.36f, 1f);

        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.11f, 0.16f, 0.24f, 1f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.16f, 0.23f, 0.34f, 1f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.19f, 0.28f, 0.41f, 1f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.13f, 0.19f, 0.29f, 1f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.17f, 0.26f, 0.39f, 1f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.20f, 0.31f, 0.47f, 1f);

        style.Colors[(int)ImGuiCol.CheckMark] = ProfessionalPalette.Accent;
        style.Colors[(int)ImGuiCol.SliderGrab] = ProfessionalPalette.Accent;
        style.Colors[(int)ImGuiCol.SliderGrabActive] = ProfessionalPalette.AccentHover;
        style.Colors[(int)ImGuiCol.Separator] = ProfessionalPalette.Border;
        style.Colors[(int)ImGuiCol.SeparatorHovered] = ProfessionalPalette.AccentHover;
        style.Colors[(int)ImGuiCol.SeparatorActive] = ProfessionalPalette.Accent;

        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.06f, 0.09f, 0.14f, 1f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.22f, 0.29f, 0.39f, 1f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.30f, 0.39f, 0.51f, 1f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = ProfessionalPalette.Accent;

        style.Colors[(int)ImGuiCol.PlotHistogram] = ProfessionalPalette.Success;
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = ProfessionalPalette.AccentHover;
        style.Colors[(int)ImGuiCol.TextSelectedBg] = ProfessionalPalette.Alpha(ProfessionalPalette.Accent, 0.35f);
        style.Colors[(int)ImGuiCol.NavCursor] = ProfessionalPalette.Accent;
        _appliedUiScale = 1f;
        ImGui.GetIO().FontGlobalScale = 1f;
    }

    /// <summary>Scale fonts and ImGui style dimensions proportionally after a window resize.</summary>
    public static void ApplyUiScale(float uiScale)
    {
        if (!float.IsFinite(uiScale) || uiScale <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale));
        }

        if (Math.Abs(uiScale - _appliedUiScale) < 0.001f)
        {
            return;
        }

        float relativeScale = uiScale / _appliedUiScale;
        ImGui.GetStyle().ScaleAllSizes(relativeScale);
        ImGui.GetIO().FontGlobalScale = uiScale;
        _appliedUiScale = uiScale;
    }

    private static string? FindBoldFontPath()
    {
        string systemFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] candidates =
        {
            Path.Combine(systemFonts, "segoeuib.ttf"),
            Path.Combine(systemFonts, "consolab.ttf"),
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? FindRegularFontPath()
    {
        string systemFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] candidates =
        {
            Path.Combine(systemFonts, "segoeui.ttf"),
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
