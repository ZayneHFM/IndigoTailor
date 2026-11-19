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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U28 - 寻丝 (Salvage from Ashes)
/// 从消耗堆中选择 1 张牌置入手牌。消耗。
/// </summary>
public sealed class SalvageFromAshes() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var exhaust = Owner?.PlayerCombatState?.ExhaustPile;
        if (exhaust != null && exhaust.Cards.Count > 0)
        {
            LocString prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_FROM_EXHAUST", CardSelectorPrefs.DiscardSelectionPrompt);
            CardModel? chosen = null;
            try
            {
                chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Exhaust, _ => true);
            }
            catch (OperationCanceledException) { }

            if (chosen != null)
            {
                await CardPileCmd.Add(chosen, PileType.Hand, CardPilePosition.Top, null, false);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
