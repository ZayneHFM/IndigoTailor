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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R24 - 截骨锯 (Amputation Saw)
/// 对所有敌人造成 24(32) 点伤害，将 2 张《伤口》置入手牌；消耗手牌中最多 3(4) 张状态牌，每消耗 1 张便获得 5(7) 点格挡与 1 点能量。【裁剪】。
/// </summary>
public sealed class AmputationSaw() : IndigoTailorCard(
    baseCost: 5,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move),
        new IntVar("ExhaustCount", 3m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (Owner != null && combatState != null)
        {
            for (int i = 0; i < 2; i++)
            {
                CardModel wound = combatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);
                await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, Owner, CardPilePosition.Top);
            }

            var hand = Owner.PlayerCombatState?.Hand?.Cards;
            if (hand != null)
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
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["ExhaustCount"].UpgradeValueBy(1m);
    }
}
