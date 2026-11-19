using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;
using IndigoTailor.Mechanics;

namespace IndigoTailor.UI;

/// <summary>
/// Hand Spatial Renderer & Interactive Seam Node Selector:
/// 1. Renders active cyan seam connectors between adjacent connected hand cards.
/// 2. Renders a translucent dashed cyan preview line when hovering a middle card to show
///    whether its left and right neighbors will form a new seam after playing it.
/// 3. Allows 1-click selection on a glowing seam node when >=2 seam pairs exist in hand.
/// 4. Renders up to 6 physical stitch badges around the card border (matching R02's 6-patch cap).
/// </summary>
public partial class PatchworkStitchingOverlay : Control
{
    public static readonly Vector2[] SixStitchNodeOffsets =
    [
        new Vector2(-92f, -125f), // Slot 1: Top-Left Indigo Patch
        new Vector2( 92f, -125f), // Slot 2: Top-Right Cyan Overlock
        new Vector2(  0f,  132f), // Slot 3: Bottom-Center Brass Snap
        new Vector2(-92f,  125f), // Slot 4: Bottom-Left Gold Lace
        new Vector2( 92f,  125f), // Slot 5: Bottom-Right Gold Lace
        new Vector2(  0f, -135f)  // Slot 6: Top-Crown Masterwork Crest
    ];

    public static void RegisterOverlay()
    {
        // Hooked into NHandCardHolder visual tree during combat
    }
}
