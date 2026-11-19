using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.UI;

/// <summary>
/// 寄生于 STS2 原生双卡手牌选择界面（NPlayerHand）之上的轻量级缝合覆盖层：
/// 1. 不遮挡战场，直接对齐原生固定左右的 [主牌] 与 [副牌] 节点；
/// 2. 右副牌中心显示 66% 裁切虚线框，并用 Line2D 从右向左拉出亮靛蓝丝线弧落入左主牌立绘中心；
/// 3. 结果胶囊固定在原生确认按钮（NConfirmButton）正上方；
/// 4. 复用并临时换装原生 NConfirmButton（确认缝合），不重复造假弹窗与假按钮。
/// 补丁状态统一由原版附魔图标表达，卡牌右侧栏位框（PatchworkRightSlotsOverlay）已不再挂载。
/// </summary>
public partial class StitchingNativeOverlay : Control
{
    public static StitchingNativeOverlay? Instance { get; private set; }

    private static readonly Color ThreadCyan = new("38EFDF");
    private static readonly Color ThreadGlow = new(0.22f, 0.94f, 0.87f, 0.35f);

    private Line2D _glowLine = null!;
    private Line2D _coreLine = null!;
    private Polygon2D _arrowHead = null!;
    private SubCutReticleControl _subCutReticle = null!;
    private PanelContainer _outcomePill = null!;
    private RichTextLabel _pillLabel = null!;

    private NPlayerHand? _hand;
    private NSelectedHandCardContainer? _selectedContainer;
    private NConfirmButton? _confirmButton;
    private RichTextLabel? _headerLabel;

    private readonly ConfirmButtonReskinner _reskinner = new();
    private bool _isActive;

    public static StitchingNativeOverlay EnsureAttached(NPlayerHand hand)
    {
        var existing = hand.GetNodeOrNull<StitchingNativeOverlay>("StitchingNativeOverlay");
        if (existing != null)
        {
            Instance = existing;
            existing._hand = hand;
            return existing;
        }

        var overlay = new StitchingNativeOverlay
        {
            Name = "StitchingNativeOverlay",
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 50
        };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        hand.AddChild(overlay);
        Instance = overlay;
        overlay._hand = hand;
        return overlay;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (Instance == this) Instance = null;
        Deactivate();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);

        _glowLine = new Line2D
        {
            Width = 7f,
            DefaultColor = ThreadGlow,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            Visible = false
        };
        _coreLine = new Line2D
        {
            Width = 3f,
            DefaultColor = ThreadCyan,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            Visible = false
        };
        _arrowHead = new Polygon2D
        {
            Color = ThreadCyan,
            Polygon = new[] { Vector2.Zero, new Vector2(14, -7), new Vector2(10, 0), new Vector2(14, 7) },
            Visible = false
        };

        AddChild(_glowLine);
        AddChild(_coreLine);
        AddChild(_arrowHead);

        _subCutReticle = new SubCutReticleControl { Visible = false };
        AddChild(_subCutReticle);

        _outcomePill = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };

        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.11f, 0.17f, 0.94f),
            BorderColor = ThreadCyan,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        _outcomePill.AddThemeStyleboxOverride("panel", styleBox);

        _pillLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(280f, 28f)
        };
        _pillLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        _outcomePill.AddChild(_pillLabel);

        AddChild(_outcomePill);
    }

    public override void _Process(double delta)
    {
        // 临时诊断：排查「未缝合的牌数据里有补丁」的架构问题，排查完毕后连文件一起删。

        if (!LoomSeamManager.IsInStitchSelectionMode)
        {
            if (_isActive) Deactivate();
            return;
        }

        if (_hand == null && NPlayerHand.Instance != null)
        {
            _hand = NPlayerHand.Instance;
        }

        if (_hand == null) return;

        if (!_isActive)
        {
            Activate(_hand);
        }

        UpdateState(_hand);
    }

    public void Activate(NPlayerHand hand)
    {
        _hand = hand;
        _isActive = true;

        _selectedContainer = hand.GetNodeOrNull<NSelectedHandCardContainer>("%SelectedHandCardContainer")
            ?? hand.FindChild("SelectedHandCardContainer", true, false) as NSelectedHandCardContainer;
        _confirmButton = hand.GetNodeOrNull<NConfirmButton>("%SelectModeConfirmButton")
            ?? hand.FindChild("SelectModeConfirmButton", true, false) as NConfirmButton;
        _headerLabel = hand.GetNodeOrNull<RichTextLabel>("%SelectionHeader")
            ?? hand.FindChild("SelectionHeader", true, false) as RichTextLabel;

        if (_confirmButton != null)
        {
            _reskinner.Apply(_confirmButton);
        }

        if (_headerLabel != null)
        {
            bool anyPair = LoomSeamManager.StitchSourceCard?.Owner?.Creature?.HasPower<FlyingShuttlePower>() == true;

            string headerKey = anyPair
                ? "INDIGOTAILOR-STITCH_UI_HEADER_ANY"
                : "INDIGOTAILOR-STITCH_UI_HEADER_ADJACENT";
            _headerLabel.Text = $"[center]{Loc(headerKey)}[/center]";
        }
    }

    /// <summary>
    /// 读取 stitches UI 文案。key 在 cards.json 里（带 INDIGOTAILOR- 前缀）。
    /// 读不到时返回 key 本身，便于快速定位缺失。
    ///
    /// 用 <see cref="LocString.GetRawText"/> 而非 GetFormattedText()：
    /// 后者走 SmartFormat，会把文案里的 <c>{0}</c> 误当成变量名去查字典，
    /// 查不到就把整个 Dictionary 对象 ToString() 填进去（曾导致浮窗显示
    /// <c>System.Collections.Generic.Dictionary`2[...]</c>）。
    /// </summary>
    private static string Loc(string key)
    {
        try
        {
            var loc = LocString.KeyPathToLocString($"cards/{key}");
            if (loc.Exists())
            {
                string raw = loc.GetRawText();
                if (!string.IsNullOrEmpty(raw)) return raw;
            }
        }
        catch
        {
            // fall through
        }
        return key;
    }

    /// <summary>
    /// 带单个参数的本地化文本（模板里写 {0}）。同样走 GetRawText，理由见 <see cref="Loc"/>。
    /// </summary>
    private static string Loc2(string key, string arg)
    {
        try
        {
            var loc = LocString.KeyPathToLocString($"cards/{key}");
            if (loc.Exists())
            {
                string raw = loc.GetRawText();
                if (!string.IsNullOrEmpty(raw))
                {
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, raw, arg);
                }
            }
        }
        catch
        {
            // fall through
        }
        return arg;
    }

    public void Deactivate()
    {
        _isActive = false;

        _glowLine.Visible = false;
        _coreLine.Visible = false;
        _arrowHead.Visible = false;
        _subCutReticle.Visible = false;
        _outcomePill.Visible = false;

        if (_confirmButton != null)
        {
            _reskinner.Restore(_confirmButton);
        }
    }

    public void OnConfirmButtonHoverChanged(NConfirmButton button, bool isHovered)
    {
        if (_isActive)
        {
            _reskinner.UpdateState(button, button.IsEnabled, isHovered);
        }
    }

    private void UpdateState(NPlayerHand hand)
    {
        if (_selectedContainer == null)
        {
            _selectedContainer = hand.GetNodeOrNull<NSelectedHandCardContainer>("%SelectedHandCardContainer");
            if (_selectedContainer == null) return;
        }

        var holders = _selectedContainer.Holders;
        if (holders == null || holders.Count == 0)
        {
            _glowLine.Visible = false;
            _coreLine.Visible = false;
            _arrowHead.Visible = false;
            _subCutReticle.Visible = false;
            _outcomePill.Visible = false;
            if (_confirmButton != null)
            {
                _confirmButton.Disable();
                _reskinner.UpdateState(_confirmButton, false, false);
            }
            return;
        }

        if (holders.Count == 1)
        {
            _glowLine.Visible = false;
            _coreLine.Visible = false;
            _arrowHead.Visible = false;
            _subCutReticle.Visible = false;

            var hostHolder = holders[0];
            var hostCardNode = hostHolder.CardNode;
            if (hostCardNode != null && hostCardNode.Model != null)
            {
                _pillLabel.Text = $"[center]{Loc("INDIGOTAILOR-STITCH_UI_PICK_SUB")}[/center]";
                _outcomePill.Visible = true;
                PositionPillAboveConfirm();
            }

            if (_confirmButton != null)
            {
                _confirmButton.Disable();
                _reskinner.UpdateState(_confirmButton, false, false);
            }
            return;
        }

        if (holders.Count >= 2)
        {
            var hostHolder = holders[0];
            var subHolder = holders[1];
            var hostCardNode = hostHolder.CardNode;
            var subCardNode = subHolder.CardNode;

            if (hostCardNode?.Model == null || subCardNode?.Model == null) return;

            // 校验合法性（相邻或飞梭引纬）
            var owner = hostCardNode.Model.Owner ?? LoomSeamManager.StitchSourceCard?.Owner;
            var handCards = owner?.PlayerCombatState?.Hand?.Cards ?? new List<CardModel>();
            bool anyPair = owner?.Creature?.HasPower<FlyingShuttlePower>() == true;
            bool isValid = LoomSeamManager.IsValidMaterialForHost(handCards, hostCardNode.Model, subCardNode.Model, owner, anyPair);
            bool canHostAccept = LoomSeamManager.CanCardAcceptPatch(hostCardNode.Model, owner);

            if (!canHostAccept)
            {
                // 主牌不可容纳更多布贴
                _glowLine.Visible = false;
                _coreLine.Visible = false;
                _arrowHead.Visible = false;
                _subCutReticle.Visible = false;

                _pillLabel.Text = $"[center][color=#ff6b6b]{Loc("INDIGOTAILOR-STITCH_UI_SLOTS_FULL")}[/color][/center]";
                _outcomePill.Visible = true;
                PositionPillAboveConfirm();

                if (_confirmButton != null)
                {
                    _confirmButton.Disable();
                    _reskinner.UpdateState(_confirmButton, false, false);
                }
                return;
            }

            if (!isValid)
            {
                // 未处于相邻缝位
                _glowLine.Visible = false;
                _coreLine.Visible = false;
                _arrowHead.Visible = false;
                _subCutReticle.Visible = false;

                _pillLabel.Text = $"[center][color=#ff6b6b]{Loc("INDIGOTAILOR-STITCH_UI_NOT_ADJACENT")}[/color][/center]";
                _outcomePill.Visible = true;
                PositionPillAboveConfirm();

                if (_confirmButton != null)
                {
                    _confirmButton.Disable();
                    _reskinner.UpdateState(_confirmButton, false, false);
                }
                return;
            }

            // 合法缝合！
            // 1. 左主牌立绘（丝线落点，右侧栏位框方案已废弃，统一落向立绘中心）
            var hostPortrait = hostCardNode.GetNodeOrNull<Control>("%Portrait") ?? hostCardNode.GetNodeOrNull<Control>("%AncientPortrait");

            // 2. 右副牌中心 66% 裁切虚线框
            var subPortrait = subCardNode.GetNodeOrNull<Control>("%Portrait") ?? subCardNode.GetNodeOrNull<Control>("%AncientPortrait");
            Vector2 subCenter = Vector2.Zero;
            if (subPortrait != null)
            {
                Transform2D xform = subPortrait.GetGlobalTransform();
                subCenter = xform * (subPortrait.Size * 0.5f);
                Vector2 scale = xform.Scale;
                float side = Mathf.Min(subPortrait.Size.X * Mathf.Abs(scale.X), subPortrait.Size.Y * Mathf.Abs(scale.Y)) * 0.66f;
                _subCutReticle.TargetGlobalRect = new Rect2(subCenter.X - side * 0.5f, subCenter.Y - side * 0.5f, side, side);
                _subCutReticle.Visible = true;
            }

            // 3. 动态贝塞尔丝线弧 (右副牌中心 -> 左主牌立绘中心)
            if (subPortrait != null && hostPortrait != null)
            {
                Vector2 startPt = subCenter;
                Transform2D hostXform = hostPortrait.GetGlobalTransform();
                Vector2 endPt = hostXform * (hostPortrait.Size * 0.5f);

                const float arcHeight = 48f;
                Vector2 mid = (startPt + endPt) * 0.5f;
                Vector2 ctrl = new(mid.X, Mathf.Min(startPt.Y, endPt.Y) - arcHeight);

                var pts = new Vector2[24];
                for (int i = 0; i < pts.Length; i++)
                {
                    float t = i / (float)(pts.Length - 1);
                    float u = 1f - t;
                    Vector2 gPt = u * u * startPt + 2f * u * t * ctrl + t * t * endPt;
                    pts[i] = gPt - GlobalPosition;
                }

                _glowLine.Points = pts;
                _coreLine.Points = pts;
                _glowLine.Visible = true;
                _coreLine.Visible = true;

                _arrowHead.Position = pts[^1];
                if (pts.Length >= 2)
                {
                    _arrowHead.Rotation = (pts[^1] - pts[^2]).Angle();
                }
                _arrowHead.Visible = true;
            }

            // 4. 结果胶囊文本（乱麻提示不在此浮窗展示）
            var (effectText, _) = LoomSeamManager.GetStitchPreviewText(subCardNode.Model);
            _pillLabel.Text = $"[center]{Loc2("INDIGOTAILOR-STITCH_UI_RESULT", effectText)}[/center]";
            _outcomePill.Visible = true;
            PositionPillAboveConfirm();

            // 5. 换装后的确认按钮点亮
            if (_confirmButton != null)
            {
                _confirmButton.Enable();
                _reskinner.UpdateState(_confirmButton, true, false);
            }
        }
    }

    /// <summary>
    /// 把结果胶囊固定在原生确认按钮（NConfirmButton）的正上方。
    ///
    /// 坐标：按钮全局坐标减去本覆盖层的全局坐标，换算成覆盖层本地坐标，避免父节点偏移。
    ///
    /// 边界处理：确认按钮本身贴在屏幕右下角，胶囊往往比按钮宽，
    /// 单纯居中会让胶囊右半截超出屏幕（实机表现：文字被右边缘切掉）。
    /// 所以算完之后要把 X 夹在 [8, 屏幕宽 - 胶囊宽 - 8] 区间内。
    /// </summary>
    private void PositionPillAboveConfirm()
    {
        if (_confirmButton == null) return;

        Vector2 btnLocal = _confirmButton.GlobalPosition - GlobalPosition;
        Vector2 btnSize = _confirmButton.Size;

        // 文本刚赋值时 Size 还停在上一次的尺寸，先按当前内容重算一次
        _outcomePill.ResetSize();

        // 面板刚变可见时 Size 可能还没算出来，用最小尺寸兜底
        Vector2 pillSize = _outcomePill.Size;
        if (pillSize.X <= 1f || pillSize.Y <= 1f)
        {
            pillSize = _outcomePill.GetCombinedMinimumSize();
        }

        const float VerticalGap = 16f;
        const float ScreenPadding = 8f;

        float x = btnLocal.X + btnSize.X * 0.5f - pillSize.X * 0.5f;

        // 夹在屏幕内。overlay 是 FullRect，所以 Size 就是可视区域尺寸。
        float screenW = Size.X;
        if (screenW > 1f && pillSize.X > 1f)
        {
            float maxX = screenW - pillSize.X - ScreenPadding;
            x = Mathf.Clamp(x, ScreenPadding, Mathf.Max(ScreenPadding, maxX));
        }

        _outcomePill.Position = new Vector2(x, btnLocal.Y - pillSize.Y - VerticalGap);
    }

    private sealed partial class SubCutReticleControl : Control
    {
        public Rect2 TargetGlobalRect { get; set; }
        private float _timer;
        private static readonly Color CyanColor = new("38EFDF");

        public SubCutReticleControl()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsPreset(LayoutPreset.FullRect);
        }

        public override void _Process(double delta)
        {
            if (!Visible) return;
            _timer += (float)delta * 4f;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (TargetGlobalRect.Size.X <= 1f) return;
            Vector2 localPos = TargetGlobalRect.Position - GlobalPosition;
            Rect2 rect = new(localPos, TargetGlobalRect.Size);

            float alpha = 0.65f + 0.35f * Mathf.Sin(_timer);
            Color color = new(CyanColor.R, CyanColor.G, CyanColor.B, alpha);

            DrawDashedRect(this, rect, color, 2f, 8f);

            // 4 角裁切标尺 L-bracket
            float cornerLen = Mathf.Min(rect.Size.X, rect.Size.Y) * 0.18f;
            DrawLine(rect.Position, rect.Position + new Vector2(cornerLen, 0), color, 3f);
            DrawLine(rect.Position, rect.Position + new Vector2(0, cornerLen), color, 3f);

            DrawLine(new Vector2(rect.End.X, rect.Position.Y), new Vector2(rect.End.X - cornerLen, rect.Position.Y), color, 3f);
            DrawLine(new Vector2(rect.End.X, rect.Position.Y), new Vector2(rect.End.X, rect.Position.Y + cornerLen), color, 3f);

            DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.Position.X + cornerLen, rect.End.Y), color, 3f);
            DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.Position.X, rect.End.Y - cornerLen), color, 3f);

            DrawLine(rect.End, rect.End - new Vector2(cornerLen, 0), color, 3f);
            DrawLine(rect.End, rect.End - new Vector2(0, cornerLen), color, 3f);
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

    public sealed class ConfirmButtonReskinner
    {
        private Texture2D? _normalTex;
        private Texture2D? _hoverTex;
        private Texture2D? _disabledTex;

        private NinePatchRect? _customSkinRect;
        private Label? _customLabel;
        private bool _isReskinned;

        private void InitTextures()
        {
            if (_normalTex == null && ResourceLoader.Exists("res://IndigoTailor/images/ui/native_confirm_btn_normal.png"))
                _normalTex = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/native_confirm_btn_normal.png");
            if (_hoverTex == null && ResourceLoader.Exists("res://IndigoTailor/images/ui/native_confirm_btn_hover.png"))
                _hoverTex = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/native_confirm_btn_hover.png");
            if (_disabledTex == null && ResourceLoader.Exists("res://IndigoTailor/images/ui/native_confirm_btn_disabled.png"))
                _disabledTex = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/native_confirm_btn_disabled.png");
        }

        public void Apply(NConfirmButton button)
        {
            // 不换皮了：直接用原版确认按钮（那个 ✓ 图标）。
            // 之前尝试用 NinePatchRect 套自定义皮肤，无论怎么调都会因为
            // 原生按钮是右下角右锚定 + 贴图比例不匹配而出现「太窄 / 被屏幕右边缘裁掉」。
            // 原版按钮本身尺寸和位置都是美术校准过的，直接用最稳。
            InitTextures();
            _isReskinned = false;
        }

        public void UpdateState(NConfirmButton button, bool isEnabled, bool isHovered)
        {
            if (!_isReskinned || _customSkinRect == null || _customLabel == null) return;
            if (!isEnabled)
            {
                _customSkinRect.Texture = _disabledTex ?? _normalTex;
                _customLabel.Modulate = new Color(1f, 1f, 1f, 0.45f);
            }
            else if (isHovered)
            {
                _customSkinRect.Texture = _hoverTex ?? _normalTex;
                _customLabel.Modulate = new Color(1f, 1f, 1f, 1.0f);
            }
            else
            {
                _customSkinRect.Texture = _normalTex;
                _customLabel.Modulate = new Color(0.9f, 0.95f, 1f, 0.9f);
            }

            // 三态贴图尺寸与金边厚度都不同，切图后必须跟着换九宫格边距，
            // 否则 normal 的边距会把 hover 的青色外发光、disabled 的透明留白一起拉伸。
            ApplyMarginsFor(_customSkinRect.Texture);
        }

        /// <summary>
        /// 按当前贴图重设九宫格边距。
        /// 边距是「贴图像素」且 NinePatchRect 的角按 1:1 屏幕像素绘制，
        /// 而本控件宽 267px、贴图宽 380px（disabled 只有 335px 有效内容），
        /// 所以实测值要按 267/贴图宽 缩放，才能和原版 Image 的等比缩小观感一致。
        /// 实测方法：取中间若干行，按列方差找「可拉伸的纯色板」左右边界；上下取金边行数。
        /// </summary>
        private void ApplyMarginsFor(Texture2D? tex)
        {
            if (_customSkinRect == null) return;

            // 皮肤现在严格铺满按钮本体（220x64 左右），不再按 267px 外扩区缩放，
            // 所以九宫格边距直接用贴图像素值，不再乘 267/380 的缩放比。
            if (tex == _disabledTex && _disabledTex != null)
            {
                // disabled 380x74，有效内容只有左上 335x52，其余透明；纯色板列 68..292，金边行 0..5 与 44..45。
                // 必须裁掉透明留白，否则右下角会采到全透明像素。
                _customSkinRect.RegionRect = new Rect2(0f, 0f, 335f, 52f);
                _customSkinRect.PatchMarginLeft = 68;
                _customSkinRect.PatchMarginRight = 42;
                _customSkinRect.PatchMarginTop = 6;
                _customSkinRect.PatchMarginBottom = 6;
            }
            else if (tex == _hoverTex && _hoverTex != null)
            {
                // hover 380x67，纯色板列 114..289，上下金边行 7..11 与 54..58。
                _customSkinRect.RegionRect = new Rect2(0f, 0f, 0f, 0f);
                _customSkinRect.PatchMarginLeft = 114;
                _customSkinRect.PatchMarginRight = 90;
                _customSkinRect.PatchMarginTop = 12;
                _customSkinRect.PatchMarginBottom = 8;
            }
            else
            {
                // normal 380x59，纯色板列 80..296，上下金边行 0..8 与 49..58。
                _customSkinRect.RegionRect = new Rect2(0f, 0f, 0f, 0f);
                _customSkinRect.PatchMarginLeft = 80;
                _customSkinRect.PatchMarginRight = 84;
                _customSkinRect.PatchMarginTop = 9;
                _customSkinRect.PatchMarginBottom = 10;
            }
        }

        public void Restore(NConfirmButton button)
        {
            if (!_isReskinned) return;
            var origImage = button.GetNodeOrNull<Control>("Image");
            if (origImage != null) origImage.Visible = true;
            var origOutline = button.GetNodeOrNull<Control>("Outline");
            if (origOutline != null) origOutline.Visible = true;

            // 还原原生尺寸与生长方向
            button.CustomMinimumSize = Vector2.Zero;
            button.GrowHorizontal = Control.GrowDirection.End;
            button.GrowVertical = Control.GrowDirection.End;

            if (_customSkinRect != null) _customSkinRect.Visible = false;
            if (_customLabel != null) _customLabel.Visible = false;
            _isReskinned = false;
        }
    }
}
