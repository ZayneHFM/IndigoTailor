using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;
using IndigoTailor.Cards.Basic;
using IndigoTailor.Relics;

namespace IndigoTailor.Character;

public sealed class IndigoTailorCharacter : PlaceholderCharacterModel
{
    public static readonly Color ColorPrimary = new("3B4E6B");
    public static readonly Color ColorSeamCyan = new("38EFDF");
    public static readonly Color ColorBrassGold = new("E5B842");

    public override CharacterGender Gender => CharacterGender.Feminine;
    public override Color NameColor => ColorSeamCyan;
    public override int StartingHp => 72;
    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<IndigoTailorCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<IndigoTailorRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IndigoTailorPotionPool>();

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIndigo>(),
        ModelDb.Card<StrikeIndigo>(),
        ModelDb.Card<StrikeIndigo>(),
        ModelDb.Card<StrikeIndigo>(),
        ModelDb.Card<DefendIndigo>(),
        ModelDb.Card<DefendIndigo>(),
        ModelDb.Card<DefendIndigo>(),
        ModelDb.Card<DefendIndigo>(),
        ModelDb.Card<BiasCut>(),
        ModelDb.Card<BastingStitch>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<RustedBrassThimble>()
    ];

    public override float AttackAnimDelay => 0.16f;
    public override float CastAnimDelay => 0.22f;

    public override Color EnergyLabelOutlineColor => new("1B263B");
    public override Color MapDrawingColor => ColorSeamCyan;

    public override string CustomVisualPath => "res://IndigoTailor/scenes/scene_indigo_tailor.tscn";
    public override string CustomCharacterSelectBg => "res://IndigoTailor/scenes/tailor_select_bg.tscn";
    public override string CustomCharacterSelectIconPath => "res://IndigoTailor/images/charui/char_select_tailor.png";
    public override string CustomCharacterSelectLockedIconPath => "res://IndigoTailor/images/charui/char_select_tailor_locked.png";
    public override string CustomIconTexturePath => "res://IndigoTailor/images/ui/character_icon_indigo_tailor_head.png";
    public override string CustomIconOutlineTexturePath => "res://IndigoTailor/images/ui/character_icon_indigo_tailor_head_outline.png";
    private static Texture2D? _customIconTexture;

    /// <summary>
    /// 静态缓存头像贴图：custom icon getter 会被多次调用，原先每次都走 ResourceLoader 读盘。
    /// </summary>
    private static Texture2D CustomIconTexture =>
        _customIconTexture ??= ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/character_icon_indigo_tailor_head.png");

    /// <summary>
    /// 返回新的 TextureRect。注意不能缓存节点本身：
    /// 原版 CharacterModel.Icon 每次调用都 Instantiate 一个新 Control，调用方会把它 AddChild 到
    /// 战斗顶栏 / 统计界面等不同父节点；复用同一节点会把它从上一个父节点抢走，导致头像在别处消失。
    /// 所以只缓存贴图，节点仍每次新建。
    /// </summary>
    public override Control? CustomIcon => new TextureRect
    {
        Name = "TailorCustomIcon",
        Texture = CustomIconTexture,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(64f, 64f)
    };
    /// <summary>
    /// 战斗左下角能量球：纯 Godot 场景，由 BaseLib 的 NodeFactory&lt;NEnergyCounter&gt;
    /// 在运行时自动转换为可用的 NEnergyCounter（见 BaseLib.Abstracts.EnergyCounterPatch）。
    /// 场景结构对齐原版 ironclad_energy_counter.tscn：Layers / RotationLayers /
    /// EnergyVfxBack / EnergyVfxFront / Label 五个节点名是 NEnergyCounter._Ready() 按名索引的硬约定。
    /// </summary>
    public override string CustomEnergyCounterPath => "res://IndigoTailor/scenes/tailor_energy_counter.tscn";

    public override string CustomMapMarkerPath => "res://IndigoTailor/images/ui/map_marker_indigo_tailor.png";
    public override string CustomRestSiteAnimPath => "res://IndigoTailor/scenes/tailor_rest_site.tscn";
    public override string CustomMerchantAnimPath => "res://IndigoTailor/scenes/tailor_merchant.tscn";
    public override string CharacterSelectSfx => "res://IndigoTailor/audio/select_shears_snap.ogg";
    public override string CustomAttackSfx => "res://IndigoTailor/audio/cut_cloth.ogg";
    public override string CustomCastSfx => "res://IndigoTailor/audio/stitch_needle.ogg";
    public override string CustomDeathSfx => "res://IndigoTailor/audio/unpick_rip.ogg";

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite sprite)
    {
        return SetupAnimationState(
            sprite,
            "idle_loop",          // idle
            "die", false,         // dead
            "hurt", false,        // hit
            "attack", false,      // attack
            "cast", false,        // cast
            "idle_loop", true     // relaxed
        );
    }
}
