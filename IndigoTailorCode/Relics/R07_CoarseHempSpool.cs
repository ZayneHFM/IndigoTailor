using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
/// R07 - 粗麻线轴 (CoarseHempSpool)
/// 拾取时，选择牌库中 1 张牌赋予【固有】。
/// 该牌同时记为绑定牌，战斗中会被移到手牌最左端（幅边位）。
/// </summary>
public sealed class CoarseHempSpool : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "coarse_hemp_spool";

    private bool _firstTurnDone;

    [SavedProperty]
    public string BoundSelvedgeCardId { get; set; } = string.Empty;

    /// <summary>
    /// 拾取时让玩家从牌库中选 1 张牌，并对其调用 AddKeyword(CardKeyword.Innate)。
    /// 选中的牌同时记入 BoundSelvedgeCardId，用于战斗开始时移到手牌左端。
    /// 若玩家未选择（或牌库为空），则不改动任何卡牌，避免软锁。
    /// </summary>
    public override async Task AfterObtained()
    {
        if (Owner is not { } owner || owner.Deck == null) return;

        // 提示语优先用模组文案，缺键时回落到原版附魔提示，避免空提示。
        var prompt = RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_INNATE", CardSelectorPrefs.EnchantSelectionPrompt);
        var prefs = new CardSelectorPrefs(prompt, 1);
        var chosen = (await CardSelectCmd.FromDeckGeneric(owner, prefs)).FirstOrDefault();
        if (chosen == null) return;

        chosen.AddKeyword(CardKeyword.Innate);
        BoundSelvedgeCardId = chosen.Id.Entry;
        Flash();
    }

    public override Task BeforeCombatStart()
    {
        _firstTurnDone = false;
        if (!string.IsNullOrEmpty(BoundSelvedgeCardId) && Owner != null)
        {
            Flash();
            LoomSeamManager.MoveBoundCardToLeftSelvedgeOnCombatStart(Owner, BoundSelvedgeCardId);
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (_firstTurnDone || Owner?.Creature is not { } creature || !participants.Contains(creature) || string.IsNullOrEmpty(BoundSelvedgeCardId))
            return Task.CompletedTask;

        _firstTurnDone = true;
        var hand = Owner.PlayerCombatState?.Hand;
        if (hand != null && hand.Cards.Count > 1)
        {
            var match = hand.Cards.FirstOrDefault(c => c.Id.Entry == BoundSelvedgeCardId || c.Id.ToString() == BoundSelvedgeCardId);
            if (match != null && !ReferenceEquals(hand.Cards[0], match))
            {
                hand.RemoveInternal(match, true);
                hand.AddInternal(match, 0, true);
                hand.InvokeContentsChanged();
            }
        }
        return Task.CompletedTask;
    }
}
