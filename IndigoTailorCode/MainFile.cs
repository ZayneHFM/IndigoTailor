using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using IndigoTailor.Mechanics;
using IndigoTailor.UI;

namespace IndigoTailor;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "IndigoTailor";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 让 Godot 检索本程序集的脚本，否则带脚本的场景会找不到程序集
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        LoomSeamManager.InitializeHooks();
        PatchworkStitchingOverlay.RegisterOverlay();

        Logger.Info("The Indigo Tailor (v2.1 Playable) initialized successfully.");
    }
}
