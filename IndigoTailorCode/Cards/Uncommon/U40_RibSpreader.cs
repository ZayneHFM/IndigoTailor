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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U40 - 扩创器 (Rib Spreader)
/// 对所有敌人施加 3(4) 层【脱线】与 2(3) 层虚弱，【幅边】手牌本回合费用 -1；获得 18(24) 点格挡，【幅边】手牌获得保留。【裁剪】。
/// </summary>
public sealed class RibSpreader() : IndigoTailorCard(
    baseCost: 4,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(18m, ValueProp.Move),
        new IntVar("Unravel", 3m),
        new IntVar("Weak", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_UNRAVEL", "INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (Owner?.Creature == null) return;
        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            decimal unravelVal = DynamicVars["Unravel"]?.BaseValue ?? 3m;
            decimal weakVal = DynamicVars["Weak"]?.BaseValue ?? 2m;
            foreach (var enemy in combatState.HittableEnemies)
            {
                await PowerCmd.Apply<UnravelPower>(choiceContext, enemy, unravelVal, Owner.Creature, this, false);
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weakVal, Owner.Creature, this, false);
            }
        }

        var hand = Owner.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count > 0)
        {
            var selvedges = new List<CardModel> { hand[0] };
            if (hand.Count > 1) selvedges.Add(hand[^1]);

            foreach (var card in selvedges)
            {
                card.EnergyCost.AddThisTurn(-1, true);
                card.AddKeyword(CardKeyword.Retain);
            }
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(6m);
        DynamicVars["Unravel"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
    }
}
