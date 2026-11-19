using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndigoTailor.Powers;

/// <summary>
/// 缓裂衬布 (Delayed Tear Lining - U37):
/// 本回合你受到的未被格挡伤害减半，减免值转化为飞针反击伤害于下回合初射向随机敌人。
/// </summary>
public sealed class DelayedTearLiningPower : IndigoTailorPower
{
    public decimal StoredMitigatedDamage { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageMultiplicativeLate(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == Owner && amount > 0)
        {
            decimal mitigated = Math.Ceiling(amount * 0.5m);
            StoredMitigatedDamage += mitigated;
            return 0.5m;
        }
        return 1m;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player.Creature == Owner && StoredMitigatedDamage > 0)
        {
            Flash();
            var enemies = Owner.CombatState?.HittableEnemies.ToList();
            if (enemies != null && enemies.Count > 0)
            {
                var target = enemies[Random.Shared.Next(enemies.Count)];
                await CreatureCmd.Damage(choiceContext, target, StoredMitigatedDamage, ValueProp.Unpowered, Owner);
            }
            StoredMitigatedDamage = 0;
            await PowerCmd.Remove(this);
        }
    }
}
