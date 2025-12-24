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

namespace UnofficialBugfix.FixPieNoCrustAttr
{

    [HarmonyPatchCategory("unofficialbugfix")]
    internal static class FixPieNoCrustAttr
    {
        /// BUG: Adding a top crust doesnt set the topCrustType
        /// attribute, which is used to rotate the type. This
        /// causes just-made full crusts to not stack with rotated
        /// full crusts because only the latter has the attribute.
        /// FIX: Manually add the attribute if we just added top crust.
#nullable enable
        [HarmonyPostfix()]
        [HarmonyPatch(typeof(BlockEntityPie))]
        [HarmonyPatch("TryAddIngredientFrom")]
        public static void FixMissingCrustAttr(BlockEntityPie __instance, ref ICoreAPI ___Api, ItemSlot slot, IPlayer? byPlayer = null)
        {
            if (__instance.Inventory[0].Itemstack?.Block is not BlockPie pieBlock) return;

            ItemStack?[] cStacks = pieBlock.GetContents(___Api.World, __instance.Inventory[0].Itemstack);

            if (cStacks[5] != null && __instance.Inventory[0].Itemstack.Attributes.GetString("topCrustType") == null)
            {
                __instance.Inventory[0].Itemstack.Attributes.SetString("topCrustType", "full");
            }
        }
    }
}
