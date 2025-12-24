using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

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

namespace UnofficialBugfix.FixBEClayOven
{

    [HarmonyPatchCategory("unofficialbugfix")]
    internal static class FixBEClayOven
    {
        /// BUG: The clay oven will show the ignite world interaction
        /// regardless of what's in the oven because the "fuel slot"
        /// is also the first cooking slot.
        /// FIX: Use the property that is only true when the fuel
        /// slot actually contains fuel.
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockEntityOven))]
        [HarmonyPatch("CanIgnite")]
        public static bool FixCanIgnite(BlockEntityOven __instance, ref bool __result)
        {
            __result = __instance.HasFuel && !__instance.IsBurning;
            return false;
        }


        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TryPut")]
        private static extern bool TryPut(BlockEntityOven instance, ItemSlot slot);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TryTake")]
        private static extern bool TryTake(BlockEntityOven instance, IPlayer byPlayer);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "TryAddFuel")]
        private static extern bool TryAddFuel(BlockEntityOven instance, ItemSlot slot);

        /// BUG: The GetBool("bakeable", true) check is
        /// supposed to default to true if the item isn't
        /// fuel and the "bakeable" property isn't defined,
        /// but since there's no check to make sure that
        /// the item has any bakingProperties in the first
        /// place, this makes the oven always say that the oven
        /// is full because every item is considered bakeable.
        /// FIX: Make sure the item has baking properties
        /// before checking for bakeable.
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockEntityOven))]
        [HarmonyPatch("OnInteract")]
        public static bool FixNonBakeableErrorText(BlockEntityOven __instance, ref bool __result, ref ItemStack ___lastRemoved, ref InventoryOven ___ovenInv, IPlayer byPlayer, BlockSelection bs)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (slot.Empty)
            {
                if (TryTake(__instance, byPlayer))
                {
                    byPlayer.InventoryManager.BroadcastHotbarSlot();
                    __result = true;
                    return false;
                }
                __result = false;
                return false;
            }
            else
            {
                CollectibleObject colObj = slot.Itemstack.Collectible;
                if (colObj.Attributes?.IsTrue("isClayOvenFuel") == true)
                {
                    if (TryAddFuel(__instance, slot))
                    {
                        AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
                        __instance.Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                        byPlayer.InventoryManager.BroadcastHotbarSlot();
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        __result = true;
                        return false;
                    }

                    __result = false;
                    return false;
                }
                else if (colObj.Attributes?["bakingProperties"] != null || colObj.CombustibleProps?.SmeltingType == EnumSmeltType.Bake && colObj.CombustibleProps.MeltingPoint < BlockEntityOven.maxBakingTemperatureAccepted)  //Can't meaningfully bake anything requiring heat over 260 in the basic clay oven
                {
                    if (slot.Itemstack.Equals(__instance.Api.World, ___lastRemoved, GlobalConstants.IgnoredStackAttributes) && !___ovenInv[0].Empty)
                    {
                        if (TryTake(__instance, byPlayer))
                        {
                            byPlayer.InventoryManager.BroadcastHotbarSlot();
                            __result = true;
                            return false;
                        }
                    }
                    else
                    {
                        var stackName = slot.Itemstack?.Collectible.Code;
                        if (TryPut(__instance, slot))
                        {
                            AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
                            __instance.Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/buildhigh"), byPlayer.Entity, byPlayer, true, 16);
                            byPlayer.InventoryManager.BroadcastHotbarSlot();
                            __instance.Api.World.Logger.Audit("{0} Put 1x{1} into Clay oven at {2}.",
                                byPlayer.PlayerName,
                                stackName,
                                __instance.Pos
                            );
                            __result = true;
                            return false;
                        }
                        else
                        {
                            if (slot.Itemstack.Block?.GetBehavior<BlockBehaviorCanIgnite>() == null)
                            {
                                ICoreClientAPI capi = __instance.Api as ICoreClientAPI;
                                bool hasBakingProps = BakingProperties.ReadFrom(slot.Itemstack) != null;

                                if (capi != null && (slot.Empty || (hasBakingProps && slot.Itemstack.Attributes.GetBool("bakeable", true)) == false)) capi.TriggerIngameError(__instance, "notbakeable", Lang.Get("This item is not bakeable."));
                                else if (capi != null && !slot.Empty) capi.TriggerIngameError(__instance, "notbakeable", __instance.IsBurning ? Lang.Get("Wait until the fire is out") : Lang.Get("Oven is full"));

                                __result = true;
                                return false;
                            }
                        }
                    }

                    __result = false;
                    return false;
                }
                else if (TryTake(__instance, byPlayer))
                //TryTake with non-empty hotbar slot, filling available empty slots in player inventory
                {
                    byPlayer.InventoryManager.BroadcastHotbarSlot();
                    __result = true;
                    return false;
                }
            }

            __result = false;
            return false;
        }
    }
}
