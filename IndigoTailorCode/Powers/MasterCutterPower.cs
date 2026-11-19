using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Powers;

/// <summary>
/// 快剪宗匠 (Master Cutter - U05):
/// 每回合你第 1 次【裁剪】或【拆线】时，获得 1 点能量并抽 1 张牌。
/// </summary>
public sealed class MasterCutterPower : IndigoTailorPower
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

    public async Task OnCutOrUnpick(PlayerChoiceContext choiceContext)
    {
        if (TriggerCountThisTurn == 0 && Owner?.Player != null)
        {
            TriggerCountThisTurn++;
            Flash();
            await PlayerCmd.GainEnergy(Amount, Owner.Player);
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player, false);
        }
    }
}
