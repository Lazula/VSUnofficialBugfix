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

namespace UnofficialBugfix.FixPartialMealNutritionInfo
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixPartialMealNutritionInfo {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "displayContentsInfo")]
    private static extern bool displayContentsInfo(BlockMeal instance);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetHeldItemInfo")]
    private static extern void GetHeldItemInfo(BlockContainer instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo);

    /// BUG: Meal held item info doesn't scale
    /// the nutrition amounts by amount left.
    /// FIX: Scale with GetNutritionHealthMul
    #nullable enable
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(BlockMeal))]
    [HarmonyPatch("GetHeldItemInfo")]
    public static void FixPartialMealNutritionFacts(BlockMeal __instance, ref ICoreAPI ___api, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
        if (inSlot.Itemstack is not ItemStack mealStack) return;
        ItemStack[] stacks = __instance.GetNonEmptyContents(world, mealStack);
        CookingRecipe? recipe = __instance.GetCookingRecipe(world, mealStack);

        // Get the wrong nutrition facts so we know how far back to erase
        string old_facts = __instance.GetContentNutritionFacts(world, inSlot, null, recipe == null);
        dsc.Remove(dsc.Length - old_facts.Length, old_facts.Length);

        if (!MealMeshCache.ContentsRotten(stacks))
        {
            float servingsLeft = mealStack.Attributes.GetFloat("quantityServings", 1);
            float[] nmul = __instance.GetNutritionHealthMul(null, inSlot, null);
            string facts = __instance.GetContentNutritionFacts(world, inSlot, stacks, null, true, servingsLeft * nmul[0], servingsLeft * nmul[1]);

            if (facts != null)
            {
                dsc.Append(facts);
            }
        }
    }
}
}
