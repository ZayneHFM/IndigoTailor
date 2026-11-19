using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace IndigoTailor.Powers;

/// <summary>
/// 靛染裁缝所有状态（Power）的基类。
///
/// 为什么必须要有这一层：
///   STS2 的模型 ID 默认不带 mod 前缀，直接继承 PowerModel 会吃 STS003 警告，
///   而且本地化表里我们用的是 INDIGOTAILOR-XXX_POWER，
///   前缀对不上就变成「界面显示原始 key」。
///   继承 CustomPowerModel（它实现 ICustomModel）后，BaseLib 的 PrefixIdPatch
///   会自动加上 "INDIGOTAILOR-" 前缀，本地化键才能对上。
///
/// 图标路径规则（与 images/powers/ 下现有文件名一致）：
///   类名 → 去掉 "Power" 后缀 → 下划线小写 → .png
///   例：UnravelPower       → UNRAVEL_POWER → 去后缀 UNRAVEL → unravel.png
///       RunningStitchPower → RUNNING_STITCH_POWER → RUNNING_STITCH → running_stitch.png
///   而美术给的文件名是 running_stitch_power.png，
///   所以两种写法都试一遍，谁先存在用谁。
/// </summary>
public abstract class IndigoTailorPower : CustomPowerModel
{
    private const string IconDir = "res://IndigoTailor/images/powers";

    /// <summary>BELLKEEPER-GUARD_POWER → GUARD_POWER → guard_power / guard</summary>
    private string IconStem
    {
        get
        {
            string entry = Id.Entry;
            int dash = entry.IndexOf('-');
            if (dash >= 0) entry = entry[(dash + 1)..];
            return entry.ToLowerInvariant();
        }
    }

    public override string CustomPackedIconPath
    {
        get
        {
            string stem = IconStem;
            string noSuffix = stem.EndsWith("_power") ? stem[..^"_power".Length] : stem;
            foreach (string candidate in new[]
                     {
                         $"{IconDir}/{stem}.png",
                         $"{IconDir}/{noSuffix}.png",
                         $"{IconDir}/{noSuffix}_power.png",
                     })
            {
                if (Godot.ResourceLoader.Exists(candidate)) return candidate;
            }
            return $"{IconDir}/unravel.png";
        }
    }

    public override string CustomBigIconPath
    {
        get
        {
            string stem = IconStem;
            string noSuffix = stem.EndsWith("_power") ? stem[..^"_power".Length] : stem;
            foreach (string candidate in new[]
                     {
                         $"{IconDir}/big/{stem}.png",
                         $"{IconDir}/big/{noSuffix}.png",
                         $"{IconDir}/big/{noSuffix}_power.png",
                     })
            {
                if (Godot.ResourceLoader.Exists(candidate)) return candidate;
            }
            return $"{IconDir}/big/unravel.png";
        }
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;
}
