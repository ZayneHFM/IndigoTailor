using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Common;

/// <summary>
/// C14 - 打版 (Chalk Pattern)
/// 获得 7(10) 点格挡。洗牌时，将此牌置于抽牌堆顶。
/// </summary>
public sealed class ChalkPattern() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    /// <summary>
    /// 洗牌时（开局初始洗牌与战斗中每次实际洗牌）把本实例移到抽牌堆顶。
    ///
    /// 索引 0 = 抽牌堆顶（先抽）：<c>CardPileCmd.Draw</c> 取
    /// <c>drawPile.Cards.FirstOrDefault()</c>；调试用 <c>DebugForcedTopCardOnNextShuffle</c>
    /// 与官方附魔 <c>PerfectFit.ModifyShuffleOrder</c> 都用 <c>Cards.Insert(0, ...)</c> 置顶。
    ///
    /// 仅当本实例确实在本次被打乱的列表里（引用比较）才移动，其余牌相对顺序不变；
    /// 无随机、无全局 Harmony、无 UI 改动。
    /// </summary>
    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        if (cards == null)
            return;

        int index = cards.FindIndex(c => ReferenceEquals(c, this));
        if (index <= 0)
            return;

        cards.RemoveAt(index);
        cards.Insert(0, this);
    }
}
