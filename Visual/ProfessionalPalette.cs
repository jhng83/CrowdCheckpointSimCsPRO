using System.Numerics;

namespace CrowdCheckpointSim.Visual;

/// <summary>
/// Product visual palette. Strong colours are reserved for operational meaning while
/// application chrome remains neutral navy/slate.
/// </summary>
public static class ProfessionalPalette
{
    public static readonly Vector4 AppBackground = Rgb(11, 18, 32);       // #0B1220
    public static readonly Vector4 SidebarBackground = Rgb(17, 24, 39);   // #111827
    public static readonly Vector4 CardBackground = Rgb(23, 32, 51);      // #172033
    public static readonly Vector4 Border = Rgb(41, 53, 72);              // #293548
    public static readonly Vector4 PrimaryText = Rgb(243, 244, 246);      // #F3F4F6
    public static readonly Vector4 SecondaryText = Rgb(156, 163, 175);    // #9CA3AF
    public static readonly Vector4 Accent = Rgb(59, 130, 246);            // #3B82F6
    public static readonly Vector4 AccentHover = Rgb(96, 165, 250);       // #60A5FA
    public static readonly Vector4 AccentActive = Rgb(37, 99, 235);       // #2563EB

    public static readonly Vector4 NeutralPassenger = Rgb(213, 216, 220); // #D5D8DC
    public static readonly Vector4 Cleared = Rgb(34, 197, 94);            // #22C55E
    public static readonly Vector4 Undetected = Rgb(245, 158, 11);        // #F59E0B
    public static readonly Vector4 Uncounted = Rgb(239, 68, 68);          // #EF4444
    public static readonly Vector4 Operator = Rgb(139, 92, 246);          // #8B5CF6
    public static readonly Vector4 Robot = Rgb(166, 180, 195);           // Metallic silver body
    public static readonly Vector4 RobotAccent = Rgb(20, 184, 166);     // Teal visor, post and status
    public static readonly Vector4 Dasher = Rgb(34, 211, 238);            // #22D3EE

    public static readonly Vector4 Warning = Rgb(245, 158, 11);
    public static readonly Vector4 Danger = Rgb(239, 68, 68);
    public static readonly Vector4 Success = Rgb(34, 197, 94);

    public static readonly Vector4 Floor = Rgb(243, 240, 232);           // Warm off-white passenger hall
    public static readonly Vector4 FloorText = Rgb(51, 65, 85);          // Legible on the light hall floor
    public static readonly Vector4 FunnelFloor = Rgb(34, 47, 65);
    public static readonly Vector4 Wall = Rgb(71, 85, 105);               // #475569
    public static readonly Vector4 Checkpoint = Rgb(100, 116, 139);       // #64748B
    public static readonly Vector4 Exit = Rgb(96, 165, 250);              // #60A5FA
    public static readonly Vector4 LaneDivider = Rgb(148, 163, 184);      // #94A3B8
    public static readonly Vector4 GateClosed = Rgb(100, 116, 139);       // #64748B
    public static readonly Vector4 GateProcessing = Rgb(245, 158, 11);    // #F59E0B
    public static readonly Vector4 GateOpen = Rgb(34, 197, 94);           // #22C55E
    public static readonly Vector4 NeutralHead = Rgb(205, 190, 171);
    public static readonly Vector4 NeutralSkinShadow = Rgb(178, 160, 140); // #B2A08C
    public static readonly Vector4 Trousers = Rgb(43, 52, 66);             // #2B3442
    public static readonly Vector4 TrousersHighlight = Rgb(57, 67, 82);    // #394352
    public static readonly Vector4 Shoes = Rgb(22, 28, 38);                // #161C26
    public static readonly Vector4 UniformTrim = Rgb(203, 213, 225);       // #CBD5E1
    public static readonly Vector4 DarkTrack = Rgb(31, 41, 55);            // #1F2937

    /// <summary>Return the supplied colour with a different alpha.</summary>
    public static Vector4 Alpha(Vector4 color, float alpha) =>
        new(color.X, color.Y, color.Z, alpha);

    private static Vector4 Rgb(int red, int green, int blue) =>
        new(red / 255f, green / 255f, blue / 255f, 1f);
}
