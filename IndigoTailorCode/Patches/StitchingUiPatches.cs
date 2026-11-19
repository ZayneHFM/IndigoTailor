using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using IndigoTailor.Mechanics;
using IndigoTailor.UI;

namespace IndigoTailor.Patches;

/// <summary>
/// 负责把卡牌立绘上已废弃的方形三栏位拼布覆盖层（PatchworkRightSlotsOverlay）清理掉，
/// 并把原生手牌双选缝合覆盖层（StitchingNativeOverlay）接入 STS2 原生渲染管线的 Harmony 补丁群。
/// 补丁状态统一改由原版附魔图标（卡牌左上角紫色小图）表达，右侧栏位框方案已停用。
/// </summary>
public static class CardPatchworkOverlayPatch
{
    /// <summary>
    /// 移除卡牌立绘上的三栏位拼布覆盖层（该 UI 方案已废弃，统一改用原版附魔图标）。
    /// </summary>
    public static void RemoveOverlay(NCard card)
    {
        if (card == null || !card.IsInsideTree()) return;

        foreach (string nodeName in new[] { "%Portrait", "%AncientPortrait" })
        {
            var portrait = card.GetNodeOrNull<Control>(nodeName);
            if (portrait == null) continue;
            var overlay = portrait.GetNodeOrNull<PatchworkRightSlotsOverlay>("PatchworkRightSlotsOverlay");
            if (overlay != null && !overlay.IsQueuedForDeletion())
            {
                overlay.QueueFree();
            }
        }
    }
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class NCardUpdatePortraitPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        CardPatchworkOverlayPatch.RemoveOverlay(__instance);
    }
}

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals), new[] { typeof(PileType), typeof(CardPreviewMode) })]
public static class NCardUpdateVisualsPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        CardPatchworkOverlayPatch.RemoveOverlay(__instance);
    }
}

[HarmonyPatch(typeof(NPlayerHand), "_Ready")]
public static class NPlayerHandReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPlayerHand __instance)
    {
        StitchingNativeOverlay.EnsureAttached(__instance);
    }
}

[HarmonyPatch(typeof(NPlayerHand), "AfterCardsSelected")]
public static class NPlayerHandAfterCardsSelectedPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPlayerHand __instance)
    {
        StitchingNativeOverlay.Instance?.Deactivate();
    }
}

[HarmonyPatch(typeof(NConfirmButton), "OnFocus")]
public static class NConfirmButtonOnFocusPatch
{
    [HarmonyPostfix]
    public static void Postfix(NConfirmButton __instance)
    {
        if (LoomSeamManager.IsInStitchSelectionMode)
        {
            StitchingNativeOverlay.Instance?.OnConfirmButtonHoverChanged(__instance, true);
        }
    }
}

[HarmonyPatch(typeof(NConfirmButton), "OnUnfocus")]
public static class NConfirmButtonOnUnfocusPatch
{
    [HarmonyPostfix]
    public static void Postfix(NConfirmButton __instance)
    {
        if (LoomSeamManager.IsInStitchSelectionMode)
        {
            StitchingNativeOverlay.Instance?.OnConfirmButtonHoverChanged(__instance, false);
        }
    }
}
