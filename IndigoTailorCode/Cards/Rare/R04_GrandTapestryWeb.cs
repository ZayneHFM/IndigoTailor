using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R04 - 天罗网 (Grand Tapestry Web)
/// 对所有敌人施加 4(6) 层【脱线】与 2(3) 层易伤。消耗。
/// </summary>
public sealed class GrandTapestryWeb() : IndigoTailorCard(
    baseCost: 2,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Unravel", 4m),
        new IntVar("Vulnerable", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.Creature == null) return;
        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            decimal unravelVal = DynamicVars["Unravel"]?.BaseValue ?? 4m;
            decimal vulnVal = DynamicVars["Vulnerable"]?.BaseValue ?? 2m;
            foreach (var enemy in combatState.HittableEnemies)
            {
                await PowerCmd.Apply<UnravelPower>(choiceContext, enemy, unravelVal, Owner.Creature, this, false);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vulnVal, Owner.Creature, this, false);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Unravel"].UpgradeValueBy(2m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}
