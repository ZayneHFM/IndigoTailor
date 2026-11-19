using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Cards;

namespace IndigoTailor.Powers;

/// <summary>
/// 余料衬裙 (Crinoline Offcuts - U04):
/// 每当你消耗或【缝合】1 张【碎布】时，获得 3(4) 点格挡。
/// </summary>
public sealed class CrinolineOffcutsPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Owner == null) return;
        if (card is IndigoTailorCard tc && tc.IsScrapToken)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }

    public async Task OnScrapStitched(PlayerChoiceContext choiceContext)
    {
        if (Owner == null) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
