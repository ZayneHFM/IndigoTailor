using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndigoTailor.Powers;

/// <summary>
/// 狂梭乱织 (Runaway Bobbin - R20):
/// 每当你打出 1 张【碎布】或触发【缝合】时，对随机敌人施加【脱线 1】并造成 3(5) 点伤害（每回合最多触发 6 次）。
/// </summary>
public sealed class RunawayBobbinPower : IndigoTailorPower
{
    public const int MaxTriggersPerTurn = 6;
    public int TriggersThisTurn { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        TriggersThisTurn = 0;
        return Task.CompletedTask;
    }

    public async Task OnScrapOrStitched(PlayerChoiceContext choiceContext)
    {
        if (TriggersThisTurn >= MaxTriggersPerTurn || Owner?.CombatState == null) return;
        TriggersThisTurn++;
        Flash();

        var enemies = Owner.CombatState.HittableEnemies.ToList();
        if (enemies.Count > 0)
        {
            var target = enemies[Random.Shared.Next(enemies.Count)];
            await PowerCmd.Apply<UnravelPower>(choiceContext, target, 1m, Owner, null, false);
            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
        }
    }
}
