using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Cards;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Relics;

/// <summary>
/// R09 - 立裁人台 (DressmakerMannequin)
/// 回合结束时，手牌中每有 1 张未打出的【胚件】，获得 3 点格挡。
/// </summary>
public sealed class DressmakerMannequin : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "dressmaker_mannequin";

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<Creature> creatures)
    {
        if (side != CombatSide.Player || Owner?.Creature == null) return;

        // 只数手牌中的【胚件】（IsScrapToken），不计入补丁数。
        int count = Owner.PlayerCombatState?.Hand?.Cards
            .OfType<IndigoTailorCard>()
            .Count(c => c.IsScrapToken) ?? 0;

        if (count > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, count * 3m, ValueProp.Unpowered, null);
        }
    }
}
