using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Common;

/// <summary>
/// C12 - 飞针雨 (Needle Rain / Indigo Needle Rain)
/// 对所有敌人造成 6(8) 点伤害。本回合每打出过 1 张【胚件】，伤害 +2(3)。
/// </summary>
public sealed class IndigoNeedleRain() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        int scrapCount = 0;
        if (CombatManager.Instance?.History?.CardPlaysStarted != null)
        {
            foreach (var e in CombatManager.Instance.History.CardPlaysStarted)
            {
                if (e.HappenedThisTurn(combatState) && e.CardPlay?.Card is IndigoTailorCard tc && tc.IsScrapToken)
                {
                    scrapCount++;
                }
            }
        }
        decimal bonusPerScrap = IsUpgraded ? 3m : 2m;
        decimal totalDmg = DynamicVars.Damage.BaseValue + scrapCount * bonusPerScrap;

        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
