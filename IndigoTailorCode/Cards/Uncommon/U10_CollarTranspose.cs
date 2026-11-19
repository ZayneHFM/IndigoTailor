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
/// U10 - 换扣 (Collar Transpose)
/// 1(0)费技能：本回合中，交换 1 对相邻手牌的费用。
/// </summary>
public sealed class CollarTranspose() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_ADJACENT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        CardPile hand = Owner.PlayerCombatState.Hand;
        if (hand.Cards.Count < 2)
        {
            await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
            return;
        }

        LocString prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_TRANSPOSE", CardSelectorPrefs.DiscardSelectionPrompt);
        CardModel? first = null;
        try
        {
            first = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Hand, c => c != this);
        }
        catch (OperationCanceledException) { }
        if (first == null)
        {
            await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
            return;
        }

        IReadOnlyList<CardModel> cards = hand.Cards;
        int idx = LoomSeamManager.FindHandCardIndex(cards, first);
        if (idx < 0)
        {
            await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
            return;
        }

        // 与右侧相邻交换；若已是最右端则改与左侧交换
        int otherIdx = idx < cards.Count - 1 ? idx + 1 : idx - 1;
        if (otherIdx < 0 || otherIdx >= cards.Count)
        {
            await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
            return;
        }

        CardModel other = cards[otherIdx];
        int costA = first.EnergyCost.GetWithModifiers(CostModifiers.All);
        int costB = other.EnergyCost.GetWithModifiers(CostModifiers.All);
        first.EnergyCost.SetThisTurn(costB, false);
        other.EnergyCost.SetThisTurn(costA, false);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
