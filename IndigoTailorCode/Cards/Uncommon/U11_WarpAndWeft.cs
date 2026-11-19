using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
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
/// U11 - 穿骨引线 (Warp and Weft)
/// 造成 5(7) 点伤害。将 1 张手牌置于右端，每跨越 1 张手牌便额外造成 3(4) 点伤害。
/// </summary>
public sealed class WarpAndWeft() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        decimal bonusDmg = 0m;
        if (Owner?.PlayerCombatState != null)
        {
            CardPile hand = Owner.PlayerCombatState.Hand;
            if (hand.Cards.Count > 1)
            {
                var prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_CARD_TO_MOVE_RIGHT", CardSelectorPrefs.DiscardSelectionPrompt);
                var chosen = await CommonActions.SelectSingleCard(this, prompt, choiceContext, PileType.Hand, c => c != this);
                if (chosen != null)
                {
                    int idx = LoomSeamManager.FindHandCardIndex(hand.Cards, chosen);
                    if (idx >= 0)
                    {
                        int crossed = Math.Max(0, hand.Cards.Count - 1 - idx);
                        bonusDmg = crossed * (IsUpgraded ? 4m : 3m);

                        hand.RemoveInternal(chosen, true);
                        hand.AddInternal(chosen, hand.Cards.Count, true);
                        hand.InvokeContentsChanged();
                    }
                }
            }
        }

        decimal totalDmg = DynamicVars.Damage.BaseValue + bonusDmg;
        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
