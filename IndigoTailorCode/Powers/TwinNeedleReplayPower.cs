using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;

namespace IndigoTailor.Powers;

/// <summary>
/// 双针 (Twin Needle Replay - R15):
/// 你的下一张具有【布贴】的牌会额外打出一次。
/// </summary>
public sealed class TwinNeedleReplayPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (Amount > 0 && card is IndigoTailorCard tc && tc.AttachedPatches.Count > 0)
        {
            Flash();
            return playCount + 1;
        }
        return playCount;
    }

    /// <summary>
    /// 只在「本牌确实吃到了额外打出」时才消耗一层。
    ///
    /// 原实现只判断了 `AttachedPatches.Count > 0`，没有判断 Amount，
    /// 若 Power 已经降到 0 层但对象还在场上，任何带补丁的牌打出都会再 Decrement，
    /// 层数变成负数并在下次打出时产生异常表现。
    /// 与 <see cref="ModifyCardPlayCount"/> 的判断条件保持一致。
    /// </summary>
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount > 0 && cardPlay.Card is IndigoTailorCard tc && tc.AttachedPatches.Count > 0)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
