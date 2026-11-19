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

namespace IndigoTailor.Cards.Token;

/// <summary>
/// T17 - 代价 (Blood Price)
/// 0 费，消耗。失去 3 点生命，将 1 张《伤口》加入你的手牌。
/// 缝合：作为副牌被缝入主牌时，主牌每打出一次就失去 3 点生命并塞 1 张《伤口》。
///
/// 设计意图：这是「裁剪诅咒 / 状态牌」的产出物 —— 裁掉一张负面牌不是白赚，
/// 而是把负担换成一张更直接的代价。它既能被打出（付 3 血把这张牌消耗掉），
/// 也能被缝进主牌（每次打出主牌都付一次）。
///
/// 卡图：v11 原版笔触重绘资产包 `blood_price.png`（1000x760 + 250x190）。
/// 命名与资产对齐，PortraitPath / CustomPortraitPath 会自动按 ID 找 `blood_price.png`。
/// </summary>
public sealed class BloodPrice() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Status,
    rarity: CardRarity.Token,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH", "INDIGOTAILOR_CUT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            3m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature,
            this,
            play);

        await RawScrapHelper.AddWoundToHandSafe(Owner);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
