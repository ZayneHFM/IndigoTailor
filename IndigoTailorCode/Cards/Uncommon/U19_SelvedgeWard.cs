using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U19 - 引流结 (Selvedge Ward / Drainage Knot)
/// 抽 2(3) 张牌，选择一张置于手牌左端。
/// </summary>
public sealed class SelvedgeWard() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("DrawCount", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        decimal drawCount = IsUpgraded ? 3m : 2m;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner, false);

        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count > 1)
        {
            var chosen = await CommonActions.SelectSingleCard(
                this,
                RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_MOVE_LEFT", CardSelectorPrefs.ExhaustSelectionPrompt),
                choiceContext,
                PileType.Hand,
                _ => true);

            if (chosen != null && hand is System.Collections.IList list)
            {
                int idx = LoomSeamManager.FindHandCardIndex(hand, chosen);
                if (idx > 0)
                {
                    list.RemoveAt(idx);
                    list.Insert(0, chosen);
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DrawCount"].UpgradeValueBy(1m);
    }
}
