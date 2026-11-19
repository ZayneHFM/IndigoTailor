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
/// R13 - 逆裁 (Reverse Pattern Cut)
/// 【裁剪】1 张手牌，将原牌上的【布贴】复制到生成的分件或【胚件】上。消耗。
/// </summary>
public sealed class ReversePatternCut() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (Owner != null && hand != null && hand.Count > 1)
        {
            var prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_CUT", CardSelectorPrefs.ExhaustSelectionPrompt);
            var chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Hand, c => c != this);
            if (chosen != null)
            {
                var copiedPatches = (chosen is IndigoTailorCard tc)
                    ? tc.AttachedPatches.Select(p => new PatchworkSlotData
                    {
                        Kind = p.Kind,
                        Magnitude = p.Magnitude,
                        IsUpgraded = p.IsUpgraded,
                        IsPermanent = false
                    }).ToList()
                    : new List<PatchworkSlotData>();

                var beforeCards = hand.ToList();
                await RawScrapHelper.CutSpecificCardAndSpawnScraps(choiceContext, Owner, this, chosen, IsUpgraded);

                var afterHand = Owner?.PlayerCombatState?.Hand?.Cards;
                if (afterHand != null)
                {
                    var newCards = afterHand.Where(c => !beforeCards.Contains(c)).ToList();
                    foreach (var nc in newCards)
                    {
                        foreach (var p in copiedPatches)
                        {
                            // 必须走 AddPatchDirectly，否则只写 AttachedPatches 不建附魔，
                            // 补丁会显示在卡上但完全不生效（详见 R20_RunawayBobbin 的注释）。
                            LoomSeamManager.AddPatchDirectly(nc, new PatchworkSlotData
                            {
                                Kind = p.Kind,
                                Magnitude = p.Magnitude,
                                IsUpgraded = p.IsUpgraded,
                                IsPermanent = false
                            });
                        }
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
