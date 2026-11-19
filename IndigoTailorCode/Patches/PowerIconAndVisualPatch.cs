using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Character;

namespace IndigoTailor.Patches;

[HarmonyPatch]
public static class PowerIconAndVisualPatch
{
    private static string ResolvePowerIcon(string name, bool isBig)
    {
        string snake = MegaCrit.Sts2.Core.Helpers.StringHelper.SnakeCase(name);
        string noSuffix = snake.EndsWith("_power") && snake.Length > 6 ? snake[..^6] : snake;
        string subFolder = isBig ? "big/" : "";

        string[] candidates =
        [
            $"res://IndigoTailor/images/powers/{subFolder}{snake}.png",
            $"res://IndigoTailor/images/powers/{subFolder}{noSuffix}.png",
            $"res://IndigoTailor/images/powers/{subFolder}{noSuffix}_power.png",
            $"res://IndigoTailor/images/powers/{snake}.png",
            $"res://IndigoTailor/images/powers/{noSuffix}.png"
        ];

        foreach (var p in candidates)
        {
            if (ResourceLoader.Exists(p)) return p;
        }
        return isBig
            ? "res://IndigoTailor/images/powers/big/unravel.png"
            : "res://IndigoTailor/images/powers/unravel.png";
    }

    [HarmonyPatch(typeof(PowerModel), "get_IconPath")]
    [HarmonyPostfix]
    public static void FixCustomPowerIconPath(PowerModel __instance, ref string __result)
    {
        if (__instance.GetType().Namespace?.StartsWith("IndigoTailor") == true)
        {
            __result = ResolvePowerIcon(__instance.GetType().Name, false);
        }
    }

    [HarmonyPatch(typeof(PowerModel), "get_PackedIconPath")]
    [HarmonyPostfix]
    public static void FixCustomPowerPackedIconPath(PowerModel __instance, ref string __result)
    {
        if (__instance.GetType().Namespace?.StartsWith("IndigoTailor") == true)
        {
            __result = ResolvePowerIcon(__instance.GetType().Name, false);
        }
    }

    [HarmonyPatch(typeof(PowerModel), "get_BigIconPath")]
    [HarmonyPostfix]
    public static void FixCustomPowerBigIconPath(PowerModel __instance, ref string __result)
    {
        if (__instance.GetType().Namespace?.StartsWith("IndigoTailor") == true)
        {
            __result = ResolvePowerIcon(__instance.GetType().Name, true);
        }
    }

    [HarmonyPatch(typeof(CharacterModel), "get_CharacterSelectBg")]
    [HarmonyPostfix]
    public static void FixCustomCharacterSelectBg(CharacterModel __instance, ref string __result)
    {
        if (__instance == null) return;
        string entry = __instance.Id.Entry ?? string.Empty;
        if (entry.Contains("INDIGOTAILOR", System.StringComparison.OrdinalIgnoreCase) ||
            __instance.GetType().Name.Contains("IndigoTailor", System.StringComparison.OrdinalIgnoreCase))
        {
            __result = "res://IndigoTailor/scenes/tailor_select_bg.tscn";
        }
    }
}
