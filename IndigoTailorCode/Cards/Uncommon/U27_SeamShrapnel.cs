using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
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
/// U27 - 炸线 (Seam Shrapnel)
/// 对所有敌人造成 7(9) 点伤害。【拆线】1 张手牌，每剥离 1 枚【布贴】便额外造成 6(8) 点伤害。
/// </summary>
public sealed class SeamShrapnel() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH", "INDIGOTAILOR_UNPICK");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null)
        {
            var candidates = hand.Where(c => LoomSeamManager.GetPatchCount(c) > 0).ToList();
            if (candidates.Count > 0)
            {
                var chosen = await CommonActions.SelectSingleCard(
                    this,
                    RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_UNPICK", CardSelectorPrefs.DiscardSelectionPrompt),
                    choiceContext,
                    PileType.Hand,
                    c => LoomSeamManager.GetPatchCount(c) > 0);

                if (chosen != null && LoomSeamManager.GetPatchCount(chosen) > 0)
                {
                    int stripped = LoomSeamManager.RemoveAllPatches(chosen);
                    await LoomSeamManager.OnUnpicked(choiceContext, Owner, chosen, stripped);

                    decimal extraDmg = stripped * (IsUpgraded ? 8m : 6m);
                    if (extraDmg > 0)
                    {
                        await DamageCmd.Attack(extraDmg)
                            .FromCard(this, play)
                            .TargetingAllOpponents(combatState)
                            .WithHitFx("vfx/vfx_attack_slash")
                            .Execute(choiceContext);
                    }
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
