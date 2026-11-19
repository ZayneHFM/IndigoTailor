using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Patches;

/// <summary>
/// 给**原版**的诅咒 / 状态牌补两条浮窗说明：
///   【裁剪】：<裁掉它产出什么>
///   【缝合】：<把它当副牌缝入主牌时给什么>
///
/// 为什么只给原版牌加、模组牌不加：
/// 模组牌（《代价》《结痂》《乱麻》）本身就在 <c>IndigoTailorCard</c> 体系里，
/// 它们的卡面 / 缝合词条已经表达了缝合效果，再加会重复。
/// 原版牌（《伤口》以及其它诅咒、状态）没有【缝合】词条，
/// 玩家根本不知道把它们当素材会发生什么，所以必须在浮窗里说明。
///
/// 实现方式：postfix 挂在 <see cref="CardModel.ExtraHoverTips"/> 这个 protected virtual getter 上。
/// 模组牌各自 override 了该属性、不会回调基类实现，所以本补丁天然只作用于原版牌。
/// </summary>
[HarmonyPatch(typeof(CardModel), "ExtraHoverTips", MethodType.Getter)]
public static class OriginalCardMaterialTipsPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance == null) return;

        // 模组牌不加（正常情况下不会走到这里，双保险）
        if (__instance is IndigoTailor.Cards.IndigoTailorCard) return;

        // 只处理诅咒 / 状态
        if (__instance.Type is not (CardType.Curse or CardType.Status)) return;

        var tips = new List<IHoverTip>();
        if (__result != null) tips.AddRange(__result);

        string entry = __instance.Id.Entry;
        string cutBody = RawScrapHelper.CutProductTipFor(entry);
        string stitchBody = RawScrapHelper.StitchProductTipFor(entry);

        tips.Add(MakeTip("【裁剪】", cutBody));
        tips.Add(MakeTip("【缝合】", stitchBody));

        __result = tips;
    }

    private static IHoverTip MakeTip(string head, string body)
    {
        // 用 card_keywords 那张【缝合】图标做统一视觉，标题直接用关键词标题
        var title = new LocString("card_keywords", "INDIGOTAILOR_STITCH.title");
        return new HoverTip(title, $"{head}：{body}", null);
    }
}
