using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;
using IndigoTailor.Mechanics;

namespace IndigoTailor.Patches;

/// <summary>
/// Ensures permanent 【布贴】 attachments created by R10 Kintsugi Permanence (金缮定型)
/// and R12 Treadle Refurbish Table (旧衣翻新台) serialize and deserialize cleanly across
/// save/load, rest site upgrades, and merchant card duplication.
/// </summary>
[HarmonyPatch]
public static class PermanentPatchworkSavePatch
{
    public static void SyncPermanentPatchesToString(IndigoTailorCard card)
    {
        var perm = card.AttachedPatches.FindAll(p => p.IsPermanent);
        if (perm.Count == 0)
        {
            card.PermanentPatchSerialized = string.Empty;
            return;
        }
        card.PermanentPatchSerialized = string.Join(";", perm.ConvertAll(p => $"{(int)p.Kind}:{p.Magnitude}:{(p.IsUpgraded ? 1 : 0)}:{p.SourceCardId}"));
    }

    /// <summary>Restores the saved permanent patches, preserving temporary patches and duplicates.</summary>
    public static void RestorePermanentPatchesFromString(IndigoTailorCard card, bool applyInCombat = false)
    {
        if (string.IsNullOrEmpty(card.PermanentPatchSerialized)) return;

        // The card list and enchantment list can mirror the same patch. Match
        // each list separately so one existing patch cannot satisfy two records.
        var existing = card.AttachedPatches.FindAll(p => p.IsPermanent);
        foreach (var patch in ParsePermanentTokens(card.PermanentPatchSerialized))
        {
            int match = existing.FindIndex(p => SameSignature(p, patch));
            if (match >= 0)
                existing.RemoveAt(match);
            else
                card.AttachedPatches.Add(patch);
        }

        if (!applyInCombat || !CanHookEnchantment(card)) return;

        var active = card.Enchantment is TailorPatchEnchantment enchantment
            ? enchantment.Patches.FindAll(p => p.IsPermanent)
            : new List<PatchworkSlotData>();
        foreach (var patch in card.AttachedPatches.FindAll(p => p.IsPermanent))
        {
            int match = active.FindIndex(p => SameSignature(p, patch));
            if (match >= 0)
                active.RemoveAt(match);
            else
                LoomSeamManager.AddPatchDirectly(card, patch);
        }
    }


    private static List<PatchworkSlotData> ParsePermanentTokens(string serialized)
    {
        var result = new List<PatchworkSlotData>();
        foreach (string token in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = token.Split(new[] { ':' }, 4);
            if (parts.Length < 3) continue;
            if (!Enum.TryParse(parts[0], out PatchworkKind kind)) continue;
            if (!int.TryParse(parts[1], out int magnitude)) continue;
            bool upgraded = parts[2] == "1" || (bool.TryParse(parts[2], out bool parsedUp) && parsedUp);
            result.Add(new PatchworkSlotData
            {
                Kind = kind,
                Magnitude = magnitude,
                IsUpgraded = upgraded,
                IsPermanent = true,
                SourceCardId = parts.Length >= 4 ? parts[3] : string.Empty
            });
        }
        return result;
    }

    private static bool SameSignature(PatchworkSlotData a, PatchworkSlotData b)
    {
        return a.Kind == b.Kind
            && a.Magnitude == b.Magnitude
            && a.IsUpgraded == b.IsUpgraded
            && string.Equals(a.SourceCardId ?? string.Empty, b.SourceCardId ?? string.Empty, StringComparison.Ordinal);
    }

    private static bool CanHookEnchantment(IndigoTailorCard card)
    {
        // 已有异种附魔 → 不能覆盖，保留原附魔。
        if (card.Enchantment != null && card.Enchantment is not TailorPatchEnchantment)
            return false;
        // 原版 EnchantmentModel.CanEnchant 硬约束：Status / Curse / Quest 不能附魔。
        return card.Type is not (CardType.Status or CardType.Curse or CardType.Quest);
    }
}
