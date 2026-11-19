using System;
using System.Collections.Generic;
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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R23 - 放血针 (Bloodletting Needle / Golden Master Pattern)
/// 获得 2(3) 点能量。将 2 张《伤口》置入手牌。消耗。
/// </summary>
public sealed class GoldenMasterPattern() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.Creature?.CombatState != null)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

            for (int i = 0; i < 2; i++)
            {
                CardModel wound = Owner.Creature.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);
                await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, Owner, CardPilePosition.Top);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
