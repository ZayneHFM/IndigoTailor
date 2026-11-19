using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;
using IndigoTailor.Mechanics;

namespace IndigoTailor.UI;

/// <summary>
/// 挂载在卡牌立绘 TextureRect (标准 500x380 @2x 或 250x190 @1x) 下的方形三栏位拼布覆盖层。
/// 未缝合时完全透明，100% 保持主卡大图；缝合 1~3 枚副卡时，依次在右上、右中、右下截取副卡中心 66% 方形区域并叠加亮靛蓝十字缝线框。
/// 同时支持在缝合选牌阶段对下一个可用空栏位显示 #38EFDF 脉冲虚线框。
/// </summary>
public partial class PatchworkRightSlotsOverlay : Control
{
    private static Texture2D? _cyanStitchFrame;
    public static Texture2D? CyanStitchFrame
    {
        get
        {
            if (_cyanStitchFrame == null && ResourceLoader.Exists("res://IndigoTailor/images/ui/stitch_frame_square_cyan_124x116.png"))
            {
                _cyanStitchFrame = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/stitch_frame_square_cyan_124x116.png");
            }
            return _cyanStitchFrame;
        }
    }

    private readonly TextureRect[] _subPortraits = new TextureRect[3];
    private readonly TextureRect[] _frameOverlays = new TextureRect[3];
    private SlotPulseIndicator _pulseIndicator = null!;

    private static readonly Vector2 FrameRatio = new(124f / 500f, 116f / 380f);
    private static readonly float[] SlotTopRatios = { 10f / 380f, 132f / 380f, 254f / 380f };
    private const float RightMarginRatio = 10f / 500f;
    private const float InnerPadRatioX = 14f / 124f;
    private const float InnerPadRatioY = 14f / 116f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);

        for (int i = 0; i < 3; i++)
        {
            var sub = new TextureRect
            {
                Name = $"PatchSubPortrait_{i}",
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale
            };
            AddChild(sub);
            _subPortraits[i] = sub;

            var frame = new TextureRect
            {
                Name = $"PatchStitchFrame_{i}",
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale
            };
            AddChild(frame);
            _frameOverlays[i] = frame;
        }

        _pulseIndicator = new SlotPulseIndicator
        {
            Name = "SlotPulseIndicator"
        };
        AddChild(_pulseIndicator);

        Resized += UpdateSlotLayouts;
        UpdateSlotLayouts();
    }

    public void ClearPatches()
    {
        for (int i = 0; i < 3; i++)
        {
            _subPortraits[i].Visible = false;
            _subPortraits[i].Texture = null;
            _frameOverlays[i].Visible = false;
            _frameOverlays[i].Texture = null;
        }
        _pulseIndicator?.Clear();
    }

    public void UpdatePatches(CardModel card)
    {
        IReadOnlyList<PatchworkSlotData>? patches = null;
        if (card.Enchantment is TailorPatchEnchantment pe && pe.Patches.Count > 0)
        {
            patches = pe.Patches;
        }
        else if (card is IndigoTailorCard itc && itc.AttachedPatches.Count > 0)
        {
            patches = itc.AttachedPatches;
        }

        if (patches == null || patches.Count == 0)
        {
            ClearPatches();
            return;
        }

        var frameTex = CyanStitchFrame;
        for (int i = 0; i < 3; i++)
        {
            bool active = i < patches.Count;
            if (!active)
            {
                _subPortraits[i].Visible = false;
                _subPortraits[i].Texture = null;
                _frameOverlays[i].Visible = false;
                _frameOverlays[i].Texture = null;
                continue;
            }

            Texture2D? src = ResolvePatchTexture(patches[i]);
            if (src == null)
            {
                _subPortraits[i].Visible = false;
                _frameOverlays[i].Visible = false;
                continue;
            }

            float sw = src.GetWidth();
            float sh = src.GetHeight();
            float side = Mathf.Min(sw, sh) * 0.66f;

            _subPortraits[i].Texture = new AtlasTexture
            {
                Atlas = src,
                Region = new Rect2((sw - side) * 0.5f, (sh - side) * 0.5f, side, side)
            };
            _frameOverlays[i].Texture = frameTex;
            _subPortraits[i].Visible = true;
            _frameOverlays[i].Visible = true;
        }

        UpdateSlotLayouts();
    }

    public void ShowSlotPulse(int slotIndex)
    {
        int idx = Mathf.Clamp(slotIndex, 0, 2);
        Vector2 sz = Size;
        if (sz.X <= 1f || sz.Y <= 1f) sz = new Vector2(500f, 380f);

        float fw = sz.X * FrameRatio.X;
        float fh = sz.Y * FrameRatio.Y;
        float sx = sz.X - fw - sz.X * RightMarginRatio;
        float sy = sz.Y * SlotTopRatios[idx];

        _pulseIndicator.SetTargetSlot(idx, new Vector2(sx, sy), new Vector2(fw, fh));
    }

    public void HideSlotPulse()
    {
        _pulseIndicator?.Clear();
    }

    /// <summary>
    /// 返回第 slotIndex (0=右上, 1=右中, 2=右下) 在当前节点全局坐标系下的中心点，供选牌丝线连线对齐落点。
    /// </summary>
    public Vector2 GetSlotGlobalCenter(int slotIndex)
    {
        int idx = Mathf.Clamp(slotIndex, 0, 2);
        Vector2 sz = Size;
        if (sz.X <= 1f || sz.Y <= 1f) sz = new Vector2(500f, 380f);

        float fw = sz.X * FrameRatio.X;
        float fh = sz.Y * FrameRatio.Y;
        float sx = sz.X - fw - sz.X * RightMarginRatio;
        float sy = sz.Y * SlotTopRatios[idx];
        return GlobalPosition + new Vector2(sx + fw * 0.5f, sy + fh * 0.5f);
    }

    public static Texture2D? ResolvePatchTexture(PatchworkSlotData patch)
    {
        if (!string.IsNullOrEmpty(patch.SourceCardId))
        {
            try
            {
                var cardModel = ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry.Equals(patch.SourceCardId, StringComparison.OrdinalIgnoreCase));
                if (cardModel?.Portrait != null)
                    return cardModel.Portrait;
            }
            catch { }

            string cleanId = patch.SourceCardId.ToLowerInvariant()
                .Replace("indigotailor-", "")
                .Replace("card_", "")
                .Trim();

            string directPath = $"res://IndigoTailor/images/card_portraits/{cleanId}.png";
            if (ResourceLoader.Exists(directPath))
                return ResourceLoader.Load<Texture2D>(directPath);

            // Strip any leading code like c03_, u14_, r17_
            string stripped = System.Text.RegularExpressions.Regex.Replace(cleanId, @"^[a-z]\d+_", "");
            string strippedPath = $"res://IndigoTailor/images/card_portraits/{stripped}.png";
            if (ResourceLoader.Exists(strippedPath))
                return ResourceLoader.Load<Texture2D>(strippedPath);
        }

        string fallbackPath = patch.Kind switch
        {
            PatchworkKind.CrimsonNeedle => "res://IndigoTailor/images/card_portraits/needle_blank.png",
            PatchworkKind.IndigoLining => "res://IndigoTailor/images/card_portraits/cloth_blank.png",
            PatchworkKind.KintsugiGold => "res://IndigoTailor/images/card_portraits/thread_blank.png",
            PatchworkKind.BarbedUnravel => "res://IndigoTailor/images/card_portraits/tangled_hemp.png",
            _ => "res://IndigoTailor/images/card_portraits/cloth_blank.png"
        };

        if (ResourceLoader.Exists(fallbackPath))
            return ResourceLoader.Load<Texture2D>(fallbackPath);

        return null;
    }

    private void UpdateSlotLayouts()
    {
        Vector2 sz = Size;
        if (sz.X <= 1f || sz.Y <= 1f)
        {
            sz = (GetParent<Control>()?.Size ?? Vector2.Zero);
            if (sz.X <= 1f || sz.Y <= 1f) sz = new Vector2(500f, 380f);
        }

        float fw = sz.X * FrameRatio.X;
        float fh = sz.Y * FrameRatio.Y;
        float sx = sz.X - fw - sz.X * RightMarginRatio;
        float padX = fw * InnerPadRatioX;
        float padY = fh * InnerPadRatioY;

        for (int i = 0; i < 3; i++)
        {
            float sy = sz.Y * SlotTopRatios[i];
            _frameOverlays[i].Position = new Vector2(sx, sy);
            _frameOverlays[i].Size = new Vector2(fw, fh);

            _subPortraits[i].Position = new Vector2(sx + padX, sy + padY);
            _subPortraits[i].Size = new Vector2(fw - 2f * padX, fh - 2f * padY);
        }
    }

    private sealed partial class SlotPulseIndicator : Control
    {
        private int _targetSlot = -1;
        private Rect2 _targetRect;
        private float _timer;
        private static readonly Color CyanColor = new("38EFDF");

        public SlotPulseIndicator()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsPreset(LayoutPreset.FullRect);
            Visible = false;
        }

        public void SetTargetSlot(int slotIndex, Vector2 pos, Vector2 size)
        {
            _targetSlot = Mathf.Clamp(slotIndex, 0, 2);
            _targetRect = new Rect2(pos, size);
            Visible = true;
            QueueRedraw();
        }

        public void Clear()
        {
            _targetSlot = -1;
            Visible = false;
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (!Visible || _targetSlot < 0) return;
            _timer += (float)delta * 5f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_targetSlot < 0 || _targetRect.Size.X <= 1f) return;
            float alpha = 0.55f + 0.45f * Mathf.Sin(_timer);
            Color drawColor = new(CyanColor.R, CyanColor.G, CyanColor.B, alpha);

            DrawDashedRect(this, _targetRect, drawColor, 2.5f, 6f);
        }

        private static void DrawDashedRect(CanvasItem ci, Rect2 rect, Color color, float width, float dashLen)
        {
            DrawDashedSegment(ci, rect.Position, new Vector2(rect.End.X, rect.Position.Y), color, width, dashLen);
            DrawDashedSegment(ci, new Vector2(rect.End.X, rect.Position.Y), rect.End, color, width, dashLen);
            DrawDashedSegment(ci, rect.End, new Vector2(rect.Position.X, rect.End.Y), color, width, dashLen);
            DrawDashedSegment(ci, new Vector2(rect.Position.X, rect.End.Y), rect.Position, color, width, dashLen);
        }

        private static void DrawDashedSegment(CanvasItem ci, Vector2 from, Vector2 to, Color color, float width, float dashLen)
        {
            float totalDist = from.DistanceTo(to);
            if (totalDist <= 0.001f) return;
            Vector2 dir = (to - from).Normalized();
            float cur = 0f;
            while (cur < totalDist)
            {
                float end = Mathf.Min(cur + dashLen, totalDist);
                ci.DrawLine(from + dir * cur, from + dir * end, color, width);
                cur += dashLen * 2f;
            }
        }
    }
}
