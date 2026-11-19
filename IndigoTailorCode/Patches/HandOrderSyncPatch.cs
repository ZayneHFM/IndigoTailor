using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndigoTailor.Patches;

/// <summary>
/// 让手牌的**视觉顺序**始终等于底层 <c>CardPile.Cards</c> 的顺序。
///
/// 为什么需要：
/// 原版 UI 顺序和底层顺序会永久错位，来源有三处 ——
/// 1. <c>NPlayerHand.OnSelectModeSourceFinished</c> 里选牌结束走 `Add(cardNode)`（无 index，追加到末尾）；
/// 2. `ReturnHolderToHand` 用 `HandLayoutHelper.GetInsertIndex` 换算插入位，该算法统计「当前在容器里的牌」，
///    拖拽期间若有牌不在容器里就会算错；
/// 3. 本 Mod 多处直接 `RemoveInternal + AddInternal` 改底层顺序，没同步 holder 节点。
///
/// 后果：玩家看到的两张「相邻」牌，在底层索引里可能差 2 位以上，
/// 于是【缝合】判定「未处于相邻缝位」，玩家必须遍历所有组合才偶尔选中系统认可的一对。
///
/// 修法：在 <c>RefreshLayout</c> 执行**之前**，把 <c>CardHolderContainer</c> 的子节点按底层顺序重排一次。
/// 这样位置计算用的就是正确顺序，视觉与判定天然一致。
///
/// 安全前提：只在「不在出牌中 / 不在拖拽 / 不在选牌」时重排，
/// 这三种状态下 holder 会被临时 reparent 到别处，重排会打架。
/// </summary>
[HarmonyPatch(typeof(NPlayerHand))]
public static class HandOrderSyncPatch
{
    private static bool TryGetBoolFlag(object hand, string propName, out bool value)
    {
        value = false;
        var t = hand.GetType();
        var p = AccessTools.Property(t, propName);
        if (p == null) return false;
        var raw = p.GetValue(hand);
        value = raw is bool b && b;
        return true;
    }

    [HarmonyPatch("RefreshLayout")]
    [HarmonyPrefix]
    public static void RefreshLayoutPrefix(NPlayerHand __instance)
    {
        if (__instance == null) return;

        // 出牌中 / 拖拽中 / 选牌中：holder 会被临时移到别的容器，此时重排会打断原版动画
        if (TryGetBoolFlag(__instance, "InCardPlay", out var inPlay) && inPlay) return;
        if (TryGetBoolFlag(__instance, "HasDraggedHolder", out var dragging) && dragging) return;
        if (TryGetBoolFlag(__instance, "IsInCardSelection", out var selecting) && selecting) return;

        var holders = __instance.ActiveHolders;
        if (holders == null || holders.Count < 2) return;

        var container = __instance.CardHolderContainer;
        if (container == null) return;

        // 只处理「确实挂在 CardHolderContainer 下」的 holder，其它（拖拽/排队）跳过
        var inContainer = new List<NHandCardHolder>();
        foreach (var h in holders)
        {
            if (h != null && h.GetParent() == container) inContainer.Add(h);
        }
        if (inContainer.Count < 2) return;

        var first = inContainer[0].CardModel;
        var owner = first?.Owner;
        if (owner == null) return;

        var pile = PileType.Hand.GetPile(owner)?.Cards;
        if (pile == null) return;

        int IndexOf(MegaCrit.Sts2.Core.Models.CardModel? m)
        {
            if (m == null) return int.MaxValue;
            for (int i = 0; i < pile.Count; i++)
            {
                if (ReferenceEquals(pile[i], m)) return i;
            }
            return int.MaxValue;
        }

        var ordered = inContainer.OrderBy(h => IndexOf(h.CardModel)).ToList();

        bool alreadySorted = true;
        for (int i = 0; i < ordered.Count; i++)
        {
            if (!ReferenceEquals(ordered[i], inContainer[i])) { alreadySorted = false; break; }
        }
        if (alreadySorted) return;

        for (int i = 0; i < ordered.Count; i++)
        {
            container.MoveChild(ordered[i], i);
        }
    }
}
