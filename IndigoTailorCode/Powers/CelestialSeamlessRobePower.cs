using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Powers;

/// <summary>
/// 天衣无缝 (Celestial Seamless Robe - R03):
/// 回合结束时，手牌中每有 1 枚【布贴】，获得 3(4) 点格挡。
/// </summary>
public sealed class CelestialSeamlessRobePower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Player == null) return;

        int patches = LoomSeamManager.CountAllHandPatches(Owner.Player);
        if (patches > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, patches * Amount, ValueProp.Unpowered, null);
        }
    }
}
