using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Powers;

/// <summary>
/// 连针 (Running Stitch - U17):
/// 每回合前 2(3) 次触发【缝合】时，抽 1 张牌。
/// </summary>
public sealed class RunningStitchPower : IndigoTailorPower
{
    public int TriggerCountThisTurn { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        TriggerCountThisTurn = 0;
        return Task.CompletedTask;
    }

    public async Task OnSeamOrStitch(PlayerChoiceContext choiceContext)
    {
        if (TriggerCountThisTurn < Amount && Owner?.Player != null)
        {
            TriggerCountThisTurn++;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, Owner.Player, false);
        }
    }
}
