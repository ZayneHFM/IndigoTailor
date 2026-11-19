using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Powers;

/// <summary>
/// 经纬天衣 (A01 - MyriadThreadsPower):
/// 【布贴】的效果触发 2 次。
/// </summary>
public sealed class MyriadThreadsPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;
}
