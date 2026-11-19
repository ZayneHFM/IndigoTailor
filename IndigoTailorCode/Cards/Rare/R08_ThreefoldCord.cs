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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R08 - 三叠缝 (Threefold Cord)
/// 选择 1 张手牌，将其两侧相邻手牌同时【缝合】入该牌，使其本回合费用 -1。消耗。
/// </summary>
public sealed class ThreefoldCord() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner == null) return;
        var hand = Owner.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count > 1 && Owner != null)
        {
            var chosen = await CommonActions.SelectSingleCard(
                this,
                RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_STITCH", CardSelectorPrefs.DiscardSelectionPrompt),
                choiceContext,
                PileType.Hand,
                c => !ReferenceEquals(c, this));

            if (chosen is IndigoTailorCard host)
            {
                int idx = LoomSeamManager.FindHandCardIndex(hand, host);
                var toStitch = new List<CardModel>();

                if (idx > 0) toStitch.Add(hand[idx - 1]);
                if (idx >= 0 && idx < hand.Count - 1) toStitch.Add(hand[idx + 1]);

                if (Owner?.Creature?.GetPower<TubularWeavePower>() != null && hand.Count > 2)
                {
                    if (idx == 0) toStitch.Add(hand[^1]);
                    else if (idx == hand.Count - 1) toStitch.Add(hand[0]);
                }

                foreach (var sub in toStitch.Distinct())
                {
                    if (ReferenceEquals(sub, host) || ReferenceEquals(sub, this)) continue;
                    if (!LoomSeamManager.CanCardAcceptPatch(host, Owner)) break;

                    await LoomSeamManager.ApplyStitch(choiceContext, Owner!, host, sub);
                }

                host.EnergyCost.AddThisTurn(-1, true);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
