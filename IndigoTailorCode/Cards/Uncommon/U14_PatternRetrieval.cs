using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U14 - 取样 (Pattern Retrieval)
/// 从抽牌堆选择 1 张牌置于手牌左端。
/// </summary>
public sealed class PatternRetrieval() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [

    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        LocString prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_RETRIEVE", CardSelectorPrefs.DiscardSelectionPrompt);
        CardModel? chosen = null;
        try
        {
            chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Draw, c => c != this);
        }
        catch (OperationCanceledException) { }
        if (chosen != null)
        {
            await CardPileCmd.Add(chosen, PileType.Hand, CardPilePosition.Top, null, false);
            CardPile hand = Owner.PlayerCombatState.Hand;
            if (hand.Cards.Contains(chosen))
            {
                hand.RemoveInternal(chosen, true);
                hand.AddInternal(chosen, 0, true);
                hand.InvokeContentsChanged();
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
