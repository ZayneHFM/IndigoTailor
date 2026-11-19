using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace IndigoTailor.Visuals;

/// <summary>
/// 挂载在战斗角色场景 Head 骨骼跟随器下的轻量待机呼吸眨眼控制器。
/// 由于 v10 将眼骨合并入 head.png，本控制器通过 NSpineSpriteBoneFollower 跟随 head 骨骼，
/// 以 2.5~3.8s 的随机生理节律驱动极短的眼部微暗与闭合（140ms），完美还原生动的待机呼吸眨眼。
/// </summary>
public partial class TailorBlinkController : NSpineSpriteBoneFollower
{
    private double _timer;
    private double _nextBlinkInterval = 2.8;
    private bool _isBlinking;
    private double _blinkProgress;
    private const double BlinkDuration = 0.14; // 140ms 极简微眨眼

    // 兜帽内部深色阴影色
    private static readonly Color HoodShadowColor = new("141A28");

    // head 骨骼局部坐标系下的两颗纽扣眼中心与半径（已按 head mesh 实测值校准）
    private static readonly Vector2 LeftEyeCenter = new(53f, -103f);
    private static readonly Vector2 RightEyeCenter = new(97f, -113f);
    private const float LeftEyeRadiusX = 14.5f;
    private const float LeftEyeRadiusY = 15.5f;
    private const float RightEyeRadiusX = 10f;
    private const float RightEyeRadiusY = 14f;

    public override void _Ready()
    {
        base._Ready();
        _timer = 1.0;
        var visuals = GetParent()?.GetNodeOrNull<Node2D>("Visuals") ?? GetNodeOrNull<Node2D>("../Visuals");
        if (visuals != null)
        {
            SetSpineSprite(visuals, "head");
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_isBlinking)
        {
            _timer += delta;
            if (_timer >= _nextBlinkInterval)
            {
                _isBlinking = true;
                _blinkProgress = 0.0;
                _timer = 0.0;
                _nextBlinkInterval = 2.5 + GD.Randf() * 1.3;
                QueueRedraw();
            }
        }
        else
        {
            _blinkProgress += delta;
            if (_blinkProgress >= BlinkDuration)
            {
                _isBlinking = false;
                _blinkProgress = 0.0;
                QueueRedraw();
            }
            else
            {
                QueueRedraw();
            }
        }
    }

    public override void _Draw()
    {
        if (!_isBlinking) return;

        float t = (float)(_blinkProgress / BlinkDuration);
        float open = Mathf.Sin(t * Mathf.Pi); // 0 -> 1 -> 0 顺滑闭合展开

        // 位于 head 骨骼局部坐标下的两颗纽扣眼位置，垂直半径随 open 收缩模拟闭合
        DrawColoredPolygon(
            GetEllipsePoints(LeftEyeCenter, LeftEyeRadiusX, LeftEyeRadiusY * open, 16),
            HoodShadowColor);
        DrawColoredPolygon(
            GetEllipsePoints(RightEyeCenter, RightEyeRadiusX, RightEyeRadiusY * open, 16),
            HoodShadowColor);
    }

    private static Vector2[] GetEllipsePoints(Vector2 center, float rx, float ry, int segments)
    {
        var pts = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float theta = i * Mathf.Tau / segments;
            pts[i] = center + new Vector2(Mathf.Cos(theta) * rx, Mathf.Sin(theta) * ry);
        }
        return pts;
    }
}
