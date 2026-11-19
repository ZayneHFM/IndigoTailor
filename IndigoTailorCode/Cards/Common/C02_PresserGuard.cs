using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Common;

/// <summary>
/// C02 - 止血结 (Presser Guard / Tourniquet Knot)
/// 获得 7(10) 点格挡。本回合中，使相邻的两张手牌获得保留。
///
/// 注意：OnPlay 执行时本牌已被移出手牌，所以不能用
/// <c>LoomSeamManager.FindHandCardIndex(hand, this)</c> 找自己的位置（永远返回 -1）。
/// 改用 <see cref="IndigoTailorCard.HandIndexWhenPlayed"/>：这是出牌前由
/// <see cref="Patches.SelvedgePlayPatches"/> 快照下来的索引。
/// </summary>
public sealed class PresserGuard() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_ADJACENT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        GrantRetainToNeighbors();

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    /// <summary>
    /// 让打出前紧挨着本牌的左右两张手牌**本回合**获得【保留】。
    ///
    /// 两个历史 bug 都在这块：
    ///
    /// 1. **只给了左边一张**：原实现无条件假设「本牌已离手」，用 `rightIdx = idx`。
    ///    但打出瞬间本牌有时仍在手牌里，`rightIdx = idx` 就指向自己，
    ///    右邻居永远拿不到。修法：先查本牌是否还在手牌，分两种情况处理。
    ///
    /// 2. **变成永久保留**：原实现用 `AddKeyword(CardKeyword.Retain)`，
    ///    那是**永久**关键词，回合结束不会消失，与卡面「本回合中」不符。
    ///    原版有专门的单回合接口 <see cref="CardModel.GiveSingleTurnRetain"/>，
    ///    它只设 `HasSingleTurnRetain`，回合结束后自动失效。
    /// </summary>
    private void GrantRetainToNeighbors()
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count == 0) return;

        int liveIdx = LoomSeamManager.FindHandCardIndex(hand, this);

        if (liveIdx >= 0)
        {
            // 本牌仍在手牌：左右邻居就是 idx±1
            if (liveIdx > 0) hand[liveIdx - 1].GiveSingleTurnRetain();
            if (liveIdx < hand.Count - 1) hand[liveIdx + 1].GiveSingleTurnRetain();
            return;
        }

        // 本牌已离手：右邻居前移一位，落到 idx
        int idx = HandIndexWhenPlayed;
        if (idx < 0) return;

        if (idx - 1 >= 0 && idx - 1 < hand.Count) hand[idx - 1].GiveSingleTurnRetain();
        if (idx >= 0 && idx < hand.Count) hand[idx].GiveSingleTurnRetain();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
