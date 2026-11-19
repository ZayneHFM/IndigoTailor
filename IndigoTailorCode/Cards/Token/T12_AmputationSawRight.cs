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
/// T12 - 截骨锯·右 (Amputation Saw: Right)
/// 消耗手牌中最多 3(4) 张状态牌，每消耗 1 张便获得 5(7) 点格挡与 1 点能量。消耗。
/// 缝合：打出时获得 1 点能量。
/// </summary>
public sealed class AmputationSawRight() : IndigoTailorCard(
    baseCost: 2,
    type: CardType.Skill,
    rarity: CardRarity.Token,
    target: TargetType.Self)
{
    public override bool IsScrapToken => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new IntVar("ExhaustCount", 3m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (Owner != null && hand != null)
        {
            int maxToExhaust = (int)DynamicVars["ExhaustCount"].BaseValue;
            var statusCards = hand.Where(c => c.Type == CardType.Status || c.Type == CardType.Curse)
                .Take(maxToExhaust)
                .ToList();

            foreach (var sc in statusCards)
            {
                await CardCmd.Exhaust(choiceContext, sc);
                if (Owner.Creature != null)
                {
                    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
                }
                await PlayerCmd.GainEnergy(1m, Owner);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["ExhaustCount"].UpgradeValueBy(1m);
    }
}
