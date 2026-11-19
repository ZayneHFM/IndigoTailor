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
/// R05 - 止血死结 (Silent Dart / Hemostatic Knot)
/// 获得 12(16) 点格挡。目标每有 1 层【脱线】，使其本回合失去 1(2) 点力量。消耗。
/// </summary>
public sealed class SilentDart() : IndigoTailorCard(
    baseCost: 2,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        if (play.Target != null && play.Target.IsAlive)
        {
            var unravelPower = play.Target.GetPower<UnravelPower>();
            int unravel = (int)(unravelPower?.Amount ?? 0m);
            if (unravel > 0)
            {
                decimal strLoss = unravel * (IsUpgraded ? 2m : 1m);
                await PowerCmd.Apply<DarkShacklesPower>(choiceContext, play.Target, strLoss, Owner.Creature, this, false);
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
