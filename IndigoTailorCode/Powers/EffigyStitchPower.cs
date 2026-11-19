using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndigoTailor.Powers;

/// <summary>
/// 同命偶 (Effigy Stitch - U34):
/// 本回合中，目标承受的伤害同步传递给其他具有【脱线】的敌人。回合结束时移除。
/// </summary>
public sealed class EffigyStitchPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldReceiveCombatHooks => true;

    private bool _isPropagating;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_isPropagating || Owner == null || target != Owner) return;
        if (result.TotalDamage <= 0) return;

        var otherEnemies = Owner.CombatState?.HittableEnemies
            .Where(e => e != Owner && e.IsAlive && e.GetPower<UnravelPower>() != null)
            .ToList();

        if (otherEnemies != null && otherEnemies.Count > 0)
        {
            _isPropagating = true;
            try
            {
                Flash();
                foreach (var enemy in otherEnemies)
                {
                    await CreatureCmd.Damage(choiceContext, enemy, result.TotalDamage, ValueProp.Unpowered, dealer ?? Owner);
                }
            }
            finally
            {
                _isPropagating = false;
            }
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
