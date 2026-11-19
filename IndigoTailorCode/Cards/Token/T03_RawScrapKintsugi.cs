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
/// T03 - 线胚 (Thread Embryo)
/// 抽 1(2) 张牌。消耗。
/// 缝合：打出主牌时抽 1 张牌。
/// </summary>
public sealed class RawScrapKintsugi() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Token,
    target: TargetType.Self)
{
    public override bool IsScrapToken => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CardPileCmd.Draw(choiceContext, IsUpgraded ? 2m : 1m, Owner, false);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
