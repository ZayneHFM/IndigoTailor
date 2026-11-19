using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Relics;

/// <summary>
/// R11 - 断刃巨剪 (BrokenGiantShears)
/// 在你的回合开始时获得 1 点能量。
/// 每场战斗开始时，向抽牌堆洗入 2 张《乱麻》。
/// </summary>
public sealed class BrokenGiantShears : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "broken_giant_shears";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    /// <summary>
    /// 每场战斗开始时，向抽牌堆洗入 2 张《乱麻》。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner == null) return;
        Flash();
        await RawScrapHelper.ShuffleTangledThreadsIntoDrawPile(Owner, 2);
    }

    /// <summary>
    /// 回合开始时获得 1 点能量（临时能量，不是能量上限）。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        Flash();
        await PlayerCmd.GainEnergy(1m, player);
    }
}
