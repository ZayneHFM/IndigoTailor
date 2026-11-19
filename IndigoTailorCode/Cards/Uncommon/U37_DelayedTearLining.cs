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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U37 - 分诊 (Delayed Tear Lining / Triage)
/// 抽 3(4) 张牌。每抽到 1 张状态牌，获得 5(7) 点格挡。
/// </summary>
public sealed class DelayedTearLining() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Cards", 3m),
        new BlockVar(5m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner == null) return;

        decimal drawCount = IsUpgraded ? 4m : 3m;
        var drawnCards = await CardPileCmd.Draw(choiceContext, drawCount, Owner, false);

        int statusCount = drawnCards?.Count(c => c.Type == CardType.Status || c.Type == CardType.Curse) ?? 0;
        if (statusCount > 0 && Owner.Creature != null)
        {
            decimal blockGain = statusCount * DynamicVars.Block.BaseValue;
            await CreatureCmd.GainBlock(Owner.Creature, blockGain, ValueProp.Unpowered, null);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
