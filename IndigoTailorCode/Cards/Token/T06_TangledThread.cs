using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Token;

/// <summary>
/// T06 - 乱麻 (Tangled Thread)
/// 不能被打出。保留。
/// 在手牌中时，每张使你失去 1 点力量与 1 点敏捷。
/// 缝合：打出时抽 1 张牌。
///
/// 实现说明（2026-11-15 第三次修订）：
///
/// 第一版：`ModifyDamageAdditive` / `ModifyBlockAdditive` 各返回 -1。
///   错在两点：状态栏不显示 debuff；语义是「伤害-1/格挡-1」而非「力量-1/敏捷-1」。
///
/// 第二版：进手牌施加 `<see cref="TenderPower"/>`（柔嫩）。
///   错在柔嫩是「每打出一张牌就掉 X 力敏」，与卡面「每张乱麻使你 -1 力 -1 敏」不符。
///
/// 第三版（当前）：直接用原版 <see cref="StrengthPower"/> / <see cref="DexterityPower"/> 施加 -1，
///   离开手牌时把这一份还回去。这样：
///   1. 状态栏如实显示「力量 -1」「敏捷 -1」，玩家一眼能看到；
///   2. 数值恰好是固定 -1，不随出牌次数增长；
///   3. 乱麻离手（打出/弃掉/消耗）时立刻归还，属于「临时」。
///
/// 不叠加：同一张乱麻只施加一次，用 <c>_applied</c> 防重入。
/// </summary>
public sealed class TangledThread() : IndigoTailorCard(
    baseCost: -1,
    type: CardType.Status,
    rarity: CardRarity.Token,
    target: TargetType.None)
{
    public override bool IsScrapToken => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
    protected override bool IsPlayable => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_STITCH");

    private bool IsInHand => Owner?.PlayerCombatState?.Hand?.Cards.Contains(this) == true;

    /// <summary>
    /// 本牌是否已经在对拥有者施加 -1 力 / -1 敏。防止重复施加或重复移除。
    /// </summary>
    private bool _applied;

    /// <summary>
    /// 卡牌在牌堆之间移动时触发。只关心「进出手牌」两种情况。
    /// </summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel clonedBy)
    {
        await base.AfterCardChangedPiles(card, oldPileType, clonedBy);

        if (!ReferenceEquals(card, this)) return;
        if (Owner?.Creature == null) return;

        bool nowInHand = IsInHand;

        if (nowInHand && !_applied)
        {
            await ApplyStatLoss();
        }
        else if (!nowInHand && _applied)
        {
            await RemoveStatLoss();
        }
    }

    private async Task ApplyStatLoss()
    {
        var owner = Owner;
        if (owner?.Creature == null) return;

        _applied = true;

        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, owner.Creature, -1m, owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(ctx, owner.Creature, -1m, owner.Creature, this, false);
    }

    private async Task RemoveStatLoss()
    {
        var owner = Owner;
        if (owner?.Creature == null) return;

        _applied = false;

        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, owner.Creature, 1m, owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(ctx, owner.Creature, 1m, owner.Creature, this, false);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }

    /// <summary>
    /// 克隆时重置施加标记 —— 否则复制出来的乱麻会以为自己已经施加过，进场后不再扣力敏。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _applied = false;
    }
}
