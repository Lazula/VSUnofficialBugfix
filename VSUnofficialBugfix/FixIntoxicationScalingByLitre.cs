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

namespace UnofficialBugfix.FixIntoxicationScalingByLitre
{

    [HarmonyPatchCategory("unofficialbugfix-tox")]
    internal static class FixIntoxicationScalingByLitre
    {
        /// BUG: FoodNutritionProperties.GetNutritionProperties() does not scale
        /// WaterTightContainableProps.Intoxication by its litre value.
        /// FIX: Add the missing litre multiplier.
        [HarmonyPostfix()]
        [HarmonyPatch(typeof(BlockLiquidContainerBase))]
        [HarmonyPatch("GetNutritionProperties")]
        public static void AddIntoxScalingPerLitre(ref FoodNutritionProperties __result, BlockLiquidContainerBase __instance, IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            ItemStack contentStack = __instance.GetContent(itemstack);

            if (contentStack == null)
            {
                return;
            }

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(contentStack);

            if (props?.NutritionPropsPerLitre != null)
            {
                float litre = contentStack.StackSize / props.ItemsPerLitre;
                float oldTox = __result.Intoxication;
                __result.Intoxication *= litre;
                //UnofficialBugfixModSystem.Logger.Notification("[FixIntoxicationScalingByLitre] fixed tox intake from {0} to {1}", oldTox, __result.Intoxication);
            }
        }
    }
}
