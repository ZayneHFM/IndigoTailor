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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U24 - 拓印 (Batik Stencil Rubbing)
/// 选择 1 张手牌，将其所有【布贴】复制到相邻手牌上。消耗。
/// </summary>
public sealed class BatikStencilRubbing() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        IReadOnlyList<CardModel> hand = Owner.PlayerCombatState.Hand.Cards;
        LocString prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_COPY_PATCH", CardSelectorPrefs.DiscardSelectionPrompt);
        CardModel? source = null;
        try
        {
            source = await CommonActions.SelectSingleCard(
                this, prompt, choiceContext, PileType.Hand,
                c => c is IndigoTailorCard tc && tc.AttachedPatches.Count > 0);
        }
        catch (OperationCanceledException) { }

        if (source is IndigoTailorCard donor && donor.AttachedPatches.Count > 0)
        {
            int idx = LoomSeamManager.FindHandCardIndex(hand, source);
            var targets = new List<CardModel>();
            if (idx > 0) targets.Add(hand[idx - 1]);
            if (idx >= 0 && idx < hand.Count - 1) targets.Add(hand[idx + 1]);

            // 环状吻合支持两端相邻
            if (Owner.Creature.GetPower<TubularWeavePower>() != null && hand.Count > 2)
            {
                if (idx == 0) targets.Add(hand[^1]);
                else if (idx == hand.Count - 1) targets.Add(hand[0]);
            }

            foreach (var target in targets.Distinct())
            {
                if (LoomSeamManager.CanCardAcceptPatch(target, Owner))
                {
                    foreach (var pd in donor.AttachedPatches)
                    {
                        if (!LoomSeamManager.CanCardAcceptPatch(target, Owner))
                            break;
                        LoomSeamManager.AddPatchDirectly(target, new PatchworkSlotData
                        {
                            Kind = pd.Kind,
                            Magnitude = pd.Magnitude,
                            IsUpgraded = pd.IsUpgraded,
                            IsPermanent = false
                        });
                    }
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
