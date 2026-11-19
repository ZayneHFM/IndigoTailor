using System;
using System.Collections.Generic;
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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R12 - 万针幕 (Canopy of Needles)
/// 对所有敌人造成 7(10) 点伤害 X 次，给予 X 层【脱线】。
/// </summary>
public sealed class CanopyOfNeedles() : IndigoTailorCard(
    baseCost: -1,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        int x = play.Resources.EnergyValue;
        for (int i = 0; i < x; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        if (x > 0)
        {
            foreach (var enemy in combatState.HittableEnemies)
            {
                await PowerCmd.Apply<UnravelPower>(choiceContext, enemy, x, Owner?.Creature, this, false);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
