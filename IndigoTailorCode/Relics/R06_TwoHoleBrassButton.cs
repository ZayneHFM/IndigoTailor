using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Relics;

/// <summary>
/// R06 - 双孔铜扣 (TwoHoleBrassButton)
/// </summary>
public sealed class TwoHoleBrassButton : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "two_hole_brass_button";

    private int _scrapsConsumedThisTurn;
    private bool _energyGainedThisTurn;
    public override Task BeforeCombatStart()
    {
        _scrapsConsumedThisTurn = 0;
        _energyGainedThisTurn = false;
        return Task.CompletedTask;
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) { _scrapsConsumedThisTurn = 0; _energyGainedThisTurn = false; }
        return Task.CompletedTask;
    }
    public async Task OnScrapConsumedOrStitched()
    {
        if (_energyGainedThisTurn || Owner == null) return;
        _scrapsConsumedThisTurn++;
        if (_scrapsConsumedThisTurn >= 3)
        {
            _energyGainedThisTurn = true;
            Flash();
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }
}
