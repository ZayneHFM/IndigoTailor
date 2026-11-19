using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Powers;

/// <summary>
/// 百衲天衣 (Hundred-Patch Masterwork - R02):
/// 所有【布贴】的伤害与格挡效果 +Amount。
/// </summary>
public sealed class HundredPatchMasterworkPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
