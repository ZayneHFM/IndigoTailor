using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Token;

/// <summary>
/// T16 - 龙骨胸架·右 (Thoracic Cage: Right)
/// 为随机手牌缝合等同于此卡费用数的【布胚(+)】，并触发一次【布贴】效果。消耗。
/// </summary>
public sealed class ThoracicCageRight() : IndigoTailorCard(
    baseCost: 3,
    type: CardType.Skill,
    rarity: CardRarity.Token,
    target: TargetType.Self)
{
    public override bool IsScrapToken => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int times = Math.Max(1, EnergyCost.GetResolved());
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null)
        {
            for (int i = 0; i < times; i++)
            {
                var candidates = hand.OfType<IndigoTailorCard>()
                    .Where(c => LoomSeamManager.CanCardAcceptPatch(c, Owner))
                    .ToList();

                if (candidates.Count == 0) break;

                var chosen = candidates[Random.Shared.Next(candidates.Count)];
                LoomSeamManager.AddPatchDirectly(chosen, new PatchworkSlotData
                {
                    Kind = PatchworkKind.IndigoLining,
                    Magnitude = IsUpgraded ? 6 : 4,
                    IsUpgraded = IsUpgraded
                });
            }

            int patchBlock = hand.Sum(c => LoomSeamManager.GetTotalBlockFromAttachedPatches(c));
            if (patchBlock > 0 && Owner?.Creature != null)
            {
                await CreatureCmd.GainBlock(Owner.Creature, patchBlock, ValueProp.Unpowered, null);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
