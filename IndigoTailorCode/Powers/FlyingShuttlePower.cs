using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Powers;

/// <summary>
/// 飞梭 (Flying Shuttle - U32):
/// 【缝合】时可选择任意 2 张手牌。
/// </summary>
public sealed class FlyingShuttlePower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
