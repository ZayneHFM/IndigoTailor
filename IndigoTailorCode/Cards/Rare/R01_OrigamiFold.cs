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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R01 - 对折缝 (Origami Fold)
/// (保留。)【缝合】位于【幅边】的两张手牌，使获得的【布贴】效果翻倍。消耗。
/// </summary>
public sealed class OrigamiFold() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH", "INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await LoomSeamManager.StitchSelvedgePairDoubled(choiceContext, Owner);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
