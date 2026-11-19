using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndigoTailor.Powers;

/// <summary>
/// 碎锦风暴 (Scrap Hurricane - R11):
/// 每回合你打出的前 3(4) 张【碎布】洗入抽牌堆而非【消耗】；
/// 每当你打出 2 张【碎布】时，抽 1 张牌（升级版再得 2 点格挡）。
/// </summary>
public sealed class ScrapHurricanePower : IndigoTailorPower
{
    public int ScrapsRecycledThisTurn { get; set; }
    public int ScrapsPlayedThisCombat { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ScrapsRecycledThisTurn = 0;
        return Task.CompletedTask;
    }

    public async Task OnScrapPlayed(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Player == null) return;
        ScrapsPlayedThisCombat++;

        if (ScrapsPlayedThisCombat % 2 == 0)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, 1m, Owner.Player, false);
            if (Amount >= 4)
            {
                await CreatureCmd.GainBlock(Owner, 2m, ValueProp.Unpowered, null);
            }
        }
    }
}
