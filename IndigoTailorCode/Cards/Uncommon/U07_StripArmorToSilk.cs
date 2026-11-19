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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U07 - 抽丝剥甲 (Strip Armor to Silk)
/// 1费技能：给予 2(3) 层【脱线】，将 1(2) 张【布胚】置入手牌。
/// </summary>
public sealed class StripArmorToSilk() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Unravel", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null && play.Target.IsAlive && Owner?.Creature != null)
        {
            await PowerCmd.Apply<UnravelPower>(choiceContext, play.Target, DynamicVars["Unravel"].BaseValue, Owner.Creature, this, false);
        }

        int count = IsUpgraded ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_CRINOLINE", false);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Unravel"].UpgradeValueBy(1m);
    }
}
