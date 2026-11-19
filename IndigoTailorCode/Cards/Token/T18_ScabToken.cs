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
/// T18 - 结痂 (Scab)
/// 0 费技能，消耗。回复 1 点生命，获得 1 点能量。
/// 缝合：作为副牌被缝入主牌时，主牌每打出一次就回复 1 点生命（**不回能量**）。
///
/// 设计意图：这是「裁剪《伤口》」的产出物 —— 唯一的正面产出。
/// 《伤口》本身是纯负面状态牌，裁掉它应该给一点回报，但回报很小（1 血 1 费），
/// 而且要消耗掉，避免循环白嫖能量。缝进主牌时只留「回血」这一半，
/// 因为能量是每回合的硬约束，如果补丁也能回能量会破坏出牌节奏。
///
/// 卡图：v11 资产包 `scab_token.png`（1000x760 + 250x190）。
/// </summary>
public sealed class ScabToken() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Token,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.Heal(Owner.Creature, 1m, true);
        await PlayerCmd.GainEnergy(1m, Owner);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
