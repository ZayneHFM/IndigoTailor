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
using IndigoTailor.Cards;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Powers;

/// <summary>
/// 【脱线】(UnravelPower):
/// - 减敌方护甲获取：每层使敌人获得的格挡减少 5%（最高减少 50%）。
/// - 受击额外易伤并消层：受到攻击时，额外承受 +2 点伤害，并减少 1 层【脱线】。
/// </summary>
public sealed class UnravelPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyBlockMultiplicative(Creature? target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || Amount <= 0) return 1m;
        return Math.Max(0.50m, 1.0m - 0.05m * Amount);
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || Amount <= 0 || dealer == null || dealer == Owner || !props.IsPoweredAttack()) return 0m;
        return 2m;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0 || dealer == null || dealer == Owner || !props.IsPoweredAttack()) return;

        // 受到攻击时减少 1 层脱线
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, dealer, cardSource, false);
    }
}
