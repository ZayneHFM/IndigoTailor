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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R25 - 龙骨胸架 (Thoracic Cage)
/// 获得 30(40) 点格挡，你的格挡在下回合开始时不会消失；为随机手牌缝合等同于此卡费用数的【布胚(+)】，并触发一次【布贴】效果。【裁剪】。
/// </summary>
public sealed class ThoracicCage() : IndigoTailorCard(
    baseCost: 6,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(30m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<BlurPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);

        int times = EnergyCost.GetResolved();
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null)
        {
            for (int i = 0; i < times; i++)
            {
                var candidates = hand.Where(c => LoomSeamManager.CanCardAcceptPatch(c, Owner)).ToList();

                if (candidates.Count == 0) break;

                var chosen = candidates[Random.Shared.Next(candidates.Count)];
                LoomSeamManager.AddPatchDirectly(chosen, new PatchworkSlotData
                {
                    Kind = PatchworkKind.IndigoLining,
                    Magnitude = IsUpgraded ? 6 : 4,
                    IsUpgraded = IsUpgraded
                });
            }

            // 触发一次布贴效果：结算所有手牌布贴格挡加成
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
        DynamicVars.Block.UpgradeValueBy(10m);
    }
}
