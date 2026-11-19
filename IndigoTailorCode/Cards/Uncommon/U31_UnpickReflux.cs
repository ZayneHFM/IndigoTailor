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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U31 - 拆线回流 (Unpick Reflux)
/// 【拆线】1 张手牌，每剥离 1 枚【布贴】便获得 1 点能量（升级：并抽 1 张牌）。消耗。
/// </summary>
public sealed class UnpickReflux() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH", "INDIGOTAILOR_UNPICK");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null && Owner != null)
        {
            var candidates = hand.Where(c => LoomSeamManager.GetPatchCount(c) > 0).ToList();
            if (candidates.Count > 0)
            {
                var chosen = await CommonActions.SelectSingleCard(
                    this,
                    RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_UNPICK", CardSelectorPrefs.DiscardSelectionPrompt),
                    choiceContext,
                    PileType.Hand,
                    c => LoomSeamManager.GetPatchCount(c) > 0);

                if (chosen != null && LoomSeamManager.GetPatchCount(chosen) > 0)
                {
                    int stripped = LoomSeamManager.RemoveAllPatches(chosen);
                    await LoomSeamManager.OnUnpicked(choiceContext, Owner, chosen, stripped);

                    if (stripped > 0)
                    {
                        await PlayerCmd.GainEnergy(stripped, Owner);
                        if (IsUpgraded)
                        {
                            await CardPileCmd.Draw(choiceContext, stripped, Owner, false);
                        }
                    }
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
