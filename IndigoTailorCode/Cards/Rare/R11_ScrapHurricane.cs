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
/// R11 - 子母剪 (Twin Fate Shears / Scrap Hurricane)
/// 移除目标全部格挡，施加 2(3) 层【脱线】；造成 10(13) 点伤害，目标每有 1 层【脱线】额外造成 3(4) 点伤害。【裁剪】。
/// </summary>
public sealed class ScrapHurricane() : IndigoTailorCard(
    baseCost: 3,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new IntVar("Unravel", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT", "INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        if (play.Target.Block > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, play.Target, play.Target.Block, Owner?.Creature);
        }

        await PowerCmd.Apply<UnravelPower>(choiceContext, play.Target, DynamicVars["Unravel"].BaseValue, Owner?.Creature, this, false);

        int unravelStacks = (int)(play.Target.GetPower<UnravelPower>()?.Amount ?? 0m);
        decimal bonusPerStack = IsUpgraded ? 4m : 3m;
        decimal totalDmg = DynamicVars.Damage.BaseValue + unravelStacks * bonusPerStack;

        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Unravel"].UpgradeValueBy(1m);
    }
}
