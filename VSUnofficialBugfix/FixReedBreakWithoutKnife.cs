using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using VSSurvivalMod;
using HarmonyLib;
using UnofficialBugfix;

namespace UnofficialBugfix.FixReedBreakWithoutKnife
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixReedBreakWithoutKnife {
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SpawnBlockBrokenParticles")]
    private static extern void SpawnBlockBrokenParticles(Block instance, BlockPos pos, IPlayer plr = null);

    /// BUG: Harvested reeds drop their root when broken by hand.
    /// FIX: Check for the player holding a knife or the reed being
    /// grown before dropping the item. The latter check is to allow
    /// the reed item to drop when breaking the reed without a knife.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockReeds))]
    [HarmonyPatch("OnBlockBroken")]
    public static bool PreventReedRootDropWithoutKnife(
        BlockReeds __instance,
        BlockDropItemStack[] ___Drops,
        BlockSounds ___Sounds,
        RelaxedReadOnlyDictionary<string, string> ___Variant,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        float dropQuantityMultiplier = 1f
    ) {
        if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
        {
            foreach (var bdrop in ___Drops)
            {
                ItemStack drop = bdrop.GetNextItemStack();
                if (drop != null && (byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife || ___Variant["state"] == "normal"))
                {
                    UnofficialBugfixModSystem.Logger.Notification("[PreventReedRootDropWithoutKnife] Reed broken with knife or while normal. Allowing drop.");
                    world.SpawnItemEntity(drop, pos, null);
                } else {
                    UnofficialBugfixModSystem.Logger.Notification("[PreventReedRootDropWithoutKnife] Reed broken without hand while harvested. Preventing drop.");
                }
            }

            world.PlaySoundAt(___Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
        }

        if (byPlayer != null && ___Variant["state"] == "normal" && (byPlayer.InventoryManager.ActiveTool == EnumTool.Knife || byPlayer.InventoryManager.ActiveTool == EnumTool.Sickle || byPlayer.InventoryManager.ActiveTool == EnumTool.Scythe))
        {
            world.BlockAccessor.SetBlock(world.GetBlock(__instance.CodeWithVariants(new string[] { "habitat", "state" }, new string[] { "land", "harvested" })).BlockId, pos);
            return false;
        }

        SpawnBlockBrokenParticles(__instance, pos);
        world.BlockAccessor.SetBlock(0, pos);

        return false;
    }
}
}
