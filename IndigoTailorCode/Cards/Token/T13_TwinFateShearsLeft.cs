using System;
using System.Collections.Generic;
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

namespace IndigoTailor.Cards.Token;

/// <summary>
/// T13 - 子母剪·左 (Twin Fate Shears: Left)
/// 移除目标全部格挡，施加 2(3) 层【脱线】。消耗。
/// 缝合：攻击前移除目标全部格挡。
/// </summary>
public sealed class TwinFateShearsLeft() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Token,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Unravel", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL", "INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        if (play.Target.Block > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, play.Target, play.Target.Block, Owner?.Creature);
        }

        await PowerCmd.Apply<UnravelPower>(choiceContext, play.Target, DynamicVars["Unravel"].BaseValue, Owner?.Creature, this, false);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Unravel"].UpgradeValueBy(1m);
    }
}
