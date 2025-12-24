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

namespace UnofficialBugfix.FixSliceCheeseFreshness
{

    [HarmonyPatchCategory("unofficialbugfix")]
    internal static class FixSliceCheeseFreshness
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "displayContentsInfo")]
        private static extern bool displayContentsInfo(BlockMeal instance);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetHeldItemInfo")]
        private static extern void GetHeldItemInfo(BlockContainer instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo);

        /// BUG: Slicing cheese creates new wheel
        /// and slice items, resetting freshness
        /// FIX: Carry over transition state
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BECheese))]
        [HarmonyPatch("TakeSlice")]
        public static bool FixCheeseFreshnessResetOnSlice(BECheese __instance, ref ItemStack __result)
        {
            if (__instance.Inventory[0].Empty)
            {
                __result = null;
                return false;
            }

            float freshness = __instance.Inventory[0].Itemstack.Collectible.UpdateAndGetTransitionState(__instance.Api.World, __instance.Inventory[0], EnumTransitionType.Perish).TransitionedHours;
            ItemCheese cheese = __instance.Inventory[0].Itemstack.Collectible as ItemCheese;
            __instance.MarkDirty(true);

            switch (cheese.Part)
            {
                case "1slice":
                    {
                        ItemStack stack = __instance.Inventory[0].Itemstack.Clone();
                        stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, freshness);
                        __instance.Inventory[0].Itemstack = null;
                        __instance.Api.World.BlockAccessor.SetBlock(0, __instance.Pos);
                        __result = stack;
                        return false;
                    }
                case "2slice":
                    {
                        ItemStack stack = new ItemStack(__instance.Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
                        stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, freshness);
                        __instance.Inventory[0].Itemstack = stack;
                        __result = stack.Clone();
                        return false;
                    }
                case "3slice":
                    {
                        ItemStack stack = new ItemStack(__instance.Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
                        stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, freshness);
                        __instance.Inventory[0].Itemstack = new ItemStack(__instance.Api.World.GetItem(cheese.CodeWithVariant("part", "2slice")));
                        __instance.Inventory[0].Itemstack.Collectible.SetTransitionState(__instance.Inventory[0].Itemstack, EnumTransitionType.Perish, freshness);
                        __result = stack.Clone();
                        return false;
                    }
                case "4slice":
                    {
                        ItemStack stack = new ItemStack(__instance.Api.World.GetItem(cheese.CodeWithVariant("part", "1slice")));
                        stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, freshness);
                        __instance.Inventory[0].Itemstack = new ItemStack(__instance.Api.World.GetItem(cheese.CodeWithVariant("part", "3slice"))); ;
                        __instance.Inventory[0].Itemstack.Collectible.SetTransitionState(__instance.Inventory[0].Itemstack, EnumTransitionType.Perish, freshness);
                        __result = stack.Clone();
                        return false;
                    }
            }

            __result = null;
            return false;
        }
    }
}
