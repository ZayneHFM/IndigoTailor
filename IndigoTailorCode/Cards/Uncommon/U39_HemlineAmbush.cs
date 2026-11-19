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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U39 - 藏针 (Hemline Ambush)
/// 抽 1 张牌，将 1(2) 张【针胚(+)】置入手牌。
/// </summary>
public sealed class HemlineAmbush() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Needles", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner != null)
            await CardPileCmd.Draw(choiceContext, 1m, Owner, false);

        int count = IsUpgraded ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_NEEDLE", IsUpgraded);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Needles"].UpgradeValueBy(1m);
    }
}
