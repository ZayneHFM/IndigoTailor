using System;
using Godot;

namespace IndigoTailor.Character;

/// <summary>
/// 靛染裁缝选人界面大背景：
/// 使用 48 帧高清序列帧（30fps 循环，1.6s 一轮）播放背景动效，
/// 彻底解决 Spine 4.2 极值网格形变导致的波纹果冻撕裂 Bug。
/// 配合 TextureRect 的 KeepAspectCovered 模式自动居中铺满全屏，适配所有主流分辨率。
/// </summary>
public partial class TailorSelectBgPlayer : Control
{
    private const int FrameCount = 48;
    private const double Fps = 30.0;
    private const double FrameDuration = 1.0 / Fps;

    private readonly Texture2D?[] _frames = new Texture2D?[FrameCount];
    private TextureRect? _frameDisplay;
    private double _timer;
    private int _currentFrameIndex;

    public override void _Ready()
    {
        _frameDisplay = GetNodeOrNull<TextureRect>("FrameDisplay");

        for (int i = 0; i < FrameCount; i++)
        {
            string path = $"res://IndigoTailor/images/charui/char_select_bg_frames/frame_{i:D2}.png";
            try
            {
                _frames[i] = ResourceLoader.Load<Texture2D>(path);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"[TailorSelectBgPlayer] 加载序列帧失败: {path}, 错误: {ex.Message}");
            }
        }

        if (_frameDisplay != null && _frames[0] != null)
        {
            _frameDisplay.Texture = _frames[0];
        }
    }

    public override void _Process(double delta)
    {
        if (_frameDisplay == null) return;

        _timer += delta;
        if (_timer >= FrameDuration)
        {
            int step = (int)(_timer / FrameDuration);
            _timer -= step * FrameDuration;
            _currentFrameIndex = (_currentFrameIndex + step) % FrameCount;

            var tex = _frames[_currentFrameIndex];
            if (tex != null)
            {
                _frameDisplay.Texture = tex;
            }
        }
    }
}
