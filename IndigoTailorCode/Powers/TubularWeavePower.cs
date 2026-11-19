using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Powers;

/// <summary>
/// 环状吻合 (Tubular Weave - U12):
/// 位于【幅边】的牌视为互相【相邻】。
/// </summary>
public sealed class TubularWeavePower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
