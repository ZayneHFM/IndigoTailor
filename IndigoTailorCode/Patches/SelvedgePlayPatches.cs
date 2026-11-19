using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;

namespace IndigoTailor.Patches;

[HarmonyPatch]
public static class SelvedgePlayPatches
{
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
    [HarmonyPrefix]
    public static void RecordSelvedgeBeforeManualPlay(CardModel card)
    {
        if (card is IndigoTailorCard itc)
        {
            RecordSelvedgeState(itc);
        }
    }

    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.AutoPlay))]
    [HarmonyPrefix]
    public static void RecordSelvedgeBeforeAutoPlay(CardModel card)
    {
        if (card is IndigoTailorCard itc)
        {
            RecordSelvedgeState(itc);
        }
    }

    [HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
    [HarmonyPrefix]
    public static void RecordSelvedgeOnPlayWrapper(CardModel __instance)
    {
        if (__instance is IndigoTailorCard itc)
        {
            RecordSelvedgeState(itc);
        }
    }

    private static void RecordSelvedgeState(IndigoTailorCard card)
    {
        var hand = card.Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count > 0)
        {
            int idx = -1;
            for (int i = 0; i < hand.Count; i++)
            {
                if (ReferenceEquals(hand[i], card))
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0)
            {
                card.WasAtSelvedgeWhenPlayed = (idx == 0 || idx == hand.Count - 1);
                card.HandIndexWhenPlayed = idx;
                card.HandCountWhenPlayed = hand.Count;
            }
        }
    }
}
