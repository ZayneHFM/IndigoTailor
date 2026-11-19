using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Powers;

/// <summary>
/// 量体裁衣 (Bespoke Tailoring - R18):
/// 回合开始时，将 1 张【针胚(+)】与 1 张【布胚(+)】置入手牌。
/// </summary>
public sealed class BespokeTailoringPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public bool SpawnsUpgraded { get; set; }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        Flash();
        int count = (int)Amount;
        for (int i = 0; i < count; i++)
        {
            await RawScrapHelper.AddScrapToHandSafe(player, "DYED_SCRAP_NEEDLE", SpawnsUpgraded);
            await RawScrapHelper.AddScrapToHandSafe(player, "DYED_SCRAP_CRINOLINE", SpawnsUpgraded);
        }
    }
}
