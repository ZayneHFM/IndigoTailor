using Godot;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;

namespace IndigoTailor.Character;

/// <summary>
/// 靛染裁缝的卡池。
///
/// 卡框方案（使用原版原生卡框）：
///   原版卡框 = 「一张带多层明暗浮雕的暖红底图」× 「hsv.gdshader 染色」。
///   保持原版浮雕质感，通过调节 H / S / V 参数呈现独有的「亮青金色」（偏青，区别于故障机器人的深蓝）：
///   - 色相 H = 0.49f (约 176°，偏青/绿松石青，故障机器人偏向 210°~220° 纯蓝)
///   - 饱和度 S = 1.15f (色彩鲜艳饱满)
///   - 亮度 V = 1.12f (提亮受光面，保留浮雕阴影与金属微光)
/// </summary>
public sealed class IndigoTailorCardPool : CustomCardPoolModel
{
    public override string Title => "indigotailor";

    // 能量图标走自绘 PNG，不走 EnergyColorName：
    public override string BigEnergyIconPath => "res://IndigoTailor/images/charui/big_energy.png";
    public override string TextEnergyIconPath => "res://IndigoTailor/images/charui/text_energy.png";

    /// <summary>亮青色相 176.4° / 360 ≈ 0.49f，明显偏青，区别于故障机器人的蓝。</summary>
    public override float H => 0.49f;

    /// <summary>饱和度 1.15f，色彩鲜艳纯正。</summary>
    public override float S => 1.15f;

    /// <summary>明度 1.12f，微提亮浮雕高光，保持凹槽阴影层次。</summary>
    public override float V => 1.12f;

    public override Color DeckEntryCardColor => new("38EFDF");
    public override Color EnergyOutlineColor => new("0C2A38");
    public override bool IsColorless => false;

    /// <summary>
    /// 返回 null 表示「不使用自定义卡框贴图，使用原版原生卡框」，
    /// 由上面的 H/S/V 决定原生浮雕卡框的材质颜色。
    /// </summary>
    public override Texture2D? CustomFrame(CustomCardModel card) => null;

    /// <summary>图鉴全解锁，不用逐张打出过一次才显示。</summary>
    public override bool SeenByDefault => true;
}

public sealed class IndigoTailorRelicPool : CustomRelicPoolModel
{
    public override string BigEnergyIconPath => "res://IndigoTailor/images/charui/big_energy.png";
    public override string TextEnergyIconPath => "res://IndigoTailor/images/charui/text_energy.png";
    public override bool SeenByDefault => true;
}

public sealed class IndigoTailorPotionPool : CustomPotionPoolModel
{
    public override string BigEnergyIconPath => "res://IndigoTailor/images/charui/big_energy.png";
    public override string TextEnergyIconPath => "res://IndigoTailor/images/charui/text_energy.png";
    public override bool SeenByDefault => true;
}
