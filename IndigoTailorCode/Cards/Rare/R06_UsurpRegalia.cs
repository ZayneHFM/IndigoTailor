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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R06 - 借袍 (Usurp Regalia)
/// (保留。)移除目标全部格挡，你获得等量的格挡。消耗。
/// </summary>
public sealed class UsurpRegalia() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null && play.Target.Block > 0)
        {
            decimal targetBlock = play.Target.Block;
            await CreatureCmd.LoseBlock(choiceContext, play.Target, targetBlock, Owner.Creature);
            if (Owner?.Creature != null)
            {
                await CreatureCmd.GainBlock(Owner.Creature, targetBlock, ValueProp.Unpowered, null);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
