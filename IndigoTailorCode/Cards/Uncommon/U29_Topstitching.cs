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
/// U29 - 压明线 (Topstitching)
/// 获得 8(11) 点格挡。选择 1 张手牌，使其【布贴】效果翻倍。消耗。
/// </summary>
public sealed class Topstitching() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null)
        {
            var candidates = hand.Where(c => LoomSeamManager.GetPatchCount(c) > 0).ToList();
            if (candidates.Count > 0)
            {
                var chosen = await CommonActions.SelectSingleCard(
                    this,
                    RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_DOUBLE_PATCH", CardSelectorPrefs.DiscardSelectionPrompt),
                    choiceContext,
                    PileType.Hand,
                    c => LoomSeamManager.GetPatchCount(c) > 0);

                if (chosen != null && LoomSeamManager.GetPatchCount(chosen) > 0)
                {
                    LoomSeamManager.DoublePatches(chosen);
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
