using System;
using System.Collections.Generic;
using System.Linq;
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
/// C08 - 抽纱 (Drawnwork Dart)
/// 1(0)费技能：【裁剪】1 张手牌，使其相邻手牌本回合费用 -1。
/// </summary>
public sealed class DrawnworkDart() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_ADJACENT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (Owner != null && hand != null && hand.Count > 1)
        {
            var prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_CUT", CardSelectorPrefs.ExhaustSelectionPrompt);
            var chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Hand, c => c != this);
            if (chosen != null)
            {
                int idx = LoomSeamManager.FindHandCardIndex(hand, chosen);
                if (idx > 0 && hand[idx - 1] != this)
                {
                    hand[idx - 1].EnergyCost.AddThisTurn(-1, true);
                }
                if (idx < hand.Count - 1 && hand[idx + 1] != this)
                {
                    hand[idx + 1].EnergyCost.AddThisTurn(-1, true);
                }

                await RawScrapHelper.CutSpecificCardAndSpawnScraps(choiceContext, Owner, this, chosen, IsUpgraded);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
