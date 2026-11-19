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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U20 - 清创剪 (Fingertip Needles / Debridement Shears)
/// 造成 9(12) 点伤害。将 1 张《伤口》放入抽牌堆。幅边：改为置入手牌。
/// </summary>
public sealed class FingertipNeedles() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override bool HasSelvedgeEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        bool atSelvedge = LoomSeamManager.IsCardAtSelvedge(this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (Owner?.Creature?.CombatState != null)
        {
            CardModel wound = Owner.Creature.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);
            if (atSelvedge)
            {
                await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, Owner, CardPilePosition.Top);
            }
            else
            {
                await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Draw, Owner, CardPilePosition.Random);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
