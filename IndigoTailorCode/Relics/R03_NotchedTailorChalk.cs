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
/// R03 - 缺口划粉 (NotchedTailorChalk)
/// </summary>
public sealed class NotchedTailorChalk : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "notched_tailor_chalk";

    private bool _triggeredThisTurn;
    public override Task BeforeCombatStart() { _triggeredThisTurn = false; return Task.CompletedTask; }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _triggeredThisTurn = false;
        return Task.CompletedTask;
    }
    public async Task OnSeamOrStitchTriggered()
    {
        if (_triggeredThisTurn || Owner?.Creature == null) return;
        _triggeredThisTurn = true;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 4m, ValueProp.Unpowered, null);
    }
}
