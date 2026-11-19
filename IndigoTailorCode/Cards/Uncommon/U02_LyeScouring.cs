using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U02 - 盐水清创 (Lye Scouring)
/// 1费技能：消耗手牌中所有状态牌，每消耗 1 张便获得 6(8) 点格挡。消耗。（升级：保留）
/// </summary>
public sealed class LyeScouring() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        int statusCount = 0;
        if (hand != null)
        {
            var statuses = hand.Where(c => c.Type == CardType.Status && c != this).ToList();
            statusCount = statuses.Count;
            foreach (var s in statuses)
            {
                await CardCmd.Exhaust(choiceContext, s);
            }
        }

        if (statusCount > 0 && Owner?.Creature != null)
        {
            decimal totalBlock = statusCount * DynamicVars.Block.BaseValue;
            await CreatureCmd.GainBlock(Owner.Creature, totalBlock, ValueProp.Move, play, false);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        AddKeyword(CardKeyword.Retain);
    }
}
