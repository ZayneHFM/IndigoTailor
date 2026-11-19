using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Character;

namespace IndigoTailor.Patches;

/// <summary>
/// 修复战斗界面左上角与统计界面角色头像回退为铁甲战士的 Harmony 补丁。
/// 拦截 CharacterModel 的 Icon 与 IconTexture getter，精准分发裁缝专属头像。
/// </summary>
[HarmonyPatch(typeof(CharacterModel))]
public static class CharacterIconPatches
{
    private static Texture2D? _tailorIconTexture;
    public static Texture2D TailorIconTexture =>
        _tailorIconTexture ??= ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/character_icon_indigo_tailor_head.png");

    private static Texture2D? _tailorIconOutlineTexture;
    public static Texture2D TailorIconOutlineTexture =>
        _tailorIconOutlineTexture ??= ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/ui/character_icon_indigo_tailor_head_outline.png");

    [HarmonyPatch("Icon", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetIconPrefix(CharacterModel __instance, ref Control __result)
    {
        if (__instance is IndigoTailorCharacter)
        {
            var tr = new TextureRect
            {
                Name = "TailorCombatIcon",
                Texture = TailorIconTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(64f, 64f)
            };
            tr.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            __result = tr;
            return false;
        }
        return true;
    }

    [HarmonyPatch("IconTexture", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetIconTexturePrefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is IndigoTailorCharacter)
        {
            __result = TailorIconTexture;
            return false;
        }
        return true;
    }

    [HarmonyPatch("IconOutlineTexture", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool GetIconOutlineTexturePrefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (__instance is IndigoTailorCharacter)
        {
            __result = TailorIconOutlineTexture;
            return false;
        }
        return true;
    }
}
