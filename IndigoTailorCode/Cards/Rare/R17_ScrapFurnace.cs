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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R17 - 煮骨釜 (Scrap Furnace / Bone Boiling Cauldron)
/// 失去 10 点生命值，为所有其他友军恢复 5(7) 点生命值与 1(2) 点能量。消耗。
/// </summary>
public sealed class ScrapFurnace() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AllAllies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Heal", 5m),
        new EnergyVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.Creature != null)
        {
            await CreatureCmd.Damage(choiceContext, Owner.Creature, 10m, ValueProp.Unpowered, Owner.Creature);

            // 单人模式下 Allies 只有自己，过滤掉自己后为空 —— 此时按「自我治疗 + 回能」结算，
            // 否则这张牌会变成纯自伤 10 点（历史 Bug）。
            var allies = Owner.Creature.CombatState?.Allies.Where(a => a != Owner.Creature).ToList();
            if (allies != null && allies.Count > 0)
            {
                foreach (var ally in allies)
                {
                    await CreatureCmd.Heal(ally, DynamicVars["Heal"].BaseValue, true);
                    if (ally.Player != null)
                    {
                        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, ally.Player);
                    }
                }
            }
            else
            {
                await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue, true);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(2m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
