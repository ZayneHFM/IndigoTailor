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
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Common;

/// <summary>
/// C16 - 借寸 (Seam Allowance)
/// 0费技能：使 1 张手牌本回合费用 -1。将 1 张【乱麻】置入手牌。消耗。（升级：保留）
/// </summary>
public sealed class SeamAllowance() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (Owner != null && hand != null && hand.Count > 1)
        {
            var prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_CARD_TO_REDUCE_COST", CardSelectorPrefs.ExhaustSelectionPrompt);
            var chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Hand, c => c != this);
            if (chosen != null)
            {
                chosen.EnergyCost.AddThisTurn(-1, true);
            }
        }

        await RawScrapHelper.AddTangledThreadToHandSafe(Owner);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
