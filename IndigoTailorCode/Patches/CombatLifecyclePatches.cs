using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Patches;

/// <summary>
/// 战斗生命周期重置补丁。
///
/// 背景（本次排查发现的 P1 级 bug）：
/// <see cref="LoomSeamManager"/> 里有三个 static 字段跨战斗/跨回合泄漏——
/// <c>StitchesThisCombat</c>、<c>KeyholeEnergyUsedThisTurn</c>、<c>ScrapsSavedByHurricaneThisTurn</c>。
/// 原先只在 <c>MainFile.Initialize()</c> 里调过一次 <c>InitializeHooks()</c>，
/// 意味着：
/// 1. 《百衲魔像》的「本场战斗每缝合过 1 次 +4」会从第 2 场战斗起持续叠加历史值；
/// 2. 任何依赖「每回合限一次」的机制会变成「整局游戏限一次」。
///
/// 修法：在战斗开始与玩家回合开始时分别重置。
/// </summary>
[HarmonyPatch]
public static class CombatLifecyclePatches
{
    [HarmonyPatch(typeof(CombatManager), "StartCombatInternal")]
    [HarmonyPostfix]
    public static void StartCombatInternalPostfix()
    {
        LoomSeamManager.ResetCombatState();
    }

    [HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
    [HarmonyPostfix]
    public static void SetupPlayerTurnPostfix()
    {
        LoomSeamManager.ResetTurnState();
    }
}
