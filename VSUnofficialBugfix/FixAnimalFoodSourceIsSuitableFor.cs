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

namespace UnofficialBugfix.FixAnimalFoodSourceIsSuitableFor
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixAnimalFoodSourceIsSuitableFor
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "FoodTags")]
    private static extern ref string[] FoodTags(CreatureDiet diet);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "WeightedFoodTags")]
    private static extern ref WeightedFoodTag[] WeightedFoodTags(CreatureDiet diet);

    // Copypasta but with attrs given since that's what BETrough.GetBlockInfo uses
    internal static void FixCreatureDietForTroughBlockInfo(EntityProperties props, CreatureDiet diet) {
        AssetLocation code = props.Code;
        JsonObject attrs = props.Attributes;

        // Set the food tags.
        ref string[] foodTags = ref FoodTags(diet);
        foodTags = attrs["creatureDiet"]["foodTags"].AsObject<string[]>() ?? [];

        // Set the weighted tags, filling in unweighted foods with 1.
        List<WeightedFoodTag> wFoodTags = new(attrs["creatureDiet"]["weightedFoodTags"].AsObject<WeightedFoodTag[]>() ?? []);
        foreach (var tag in foodTags) wFoodTags.Add(new WeightedFoodTag() { Code = tag, Weight = 1 });            
        ref WeightedFoodTag[] wft = ref WeightedFoodTags(diet);
        wft = wFoodTags.ToArray();

        UnofficialBugfixModSystem.Logger.Notification("Added food tags to {0}: {1}", code.ToShortString(), foodTags);
    }

    // BUGFIX: The new weighted creature diet can't be
    // loaded from json due to the base tags being
    // protected. We just load it manually.
    //
    // Unfortunately, IsSuitableFor is a method on IAnimalFoodSource,
    // and we can't patch interfaces, so we hook into every vanilla
    // implementor instead.
    internal static bool FixCreatureDiet(Entity entity, CreatureDiet diet) {
        AssetLocation code = entity.Properties.Code;
        JsonObject attrs = entity.Properties.Attributes;

        // Set the food tags.
        ref string[] foodTags = ref FoodTags(diet);
        foodTags = attrs["creatureDiet"]["foodTags"].AsObject<string[]>();

        // Set the weighted tags, filling in unweighted foods with 1.
        List<WeightedFoodTag> wFoodTags = new(attrs["creatureDiet"]["weightedFoodTags"].AsObject<WeightedFoodTag[]>() ?? []);
        foreach (var tag in foodTags) wFoodTags.Add(new WeightedFoodTag() { Code = tag, Weight = 1 });            
        ref WeightedFoodTag[] wft = ref WeightedFoodTags(diet);
        wft = wFoodTags.ToArray();

        //UnofficialBugfixModSystem.Logger.Notification("Added food tags to {0}: {1}", code.ToShortString(), foodTags);

        return true;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityAnimalTrap))]
    [HarmonyPatch("IsSuitableFor")]
    public static bool PatchBlockEntityAnimalTrapIsSuitableFor(Entity entity, CreatureDiet diet) {
        return FixCreatureDiet(entity, diet);
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityBeehive))]
    [HarmonyPatch("IsSuitableFor")]
    public static bool PatchBlockEntityBeehiveIsSuitableFor(Entity entity, CreatureDiet diet) {
        return FixCreatureDiet(entity, diet);
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityBerryBush))]
    [HarmonyPatch("IsSuitableFor")]
    public static bool PatchBlockEntityBerryBushIsSuitableFor(Entity entity, CreatureDiet diet) {
        return FixCreatureDiet(entity, diet);
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityFarmland))]
    [HarmonyPatch("IsSuitableFor")]
    public static bool PatchBlockEntityFarmlandIsSuitableFor(Entity entity, CreatureDiet diet) {
        return FixCreatureDiet(entity, diet);
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityTrough))]
    [HarmonyPatch("IsSuitableFor")]
    public static bool PatchBlockEntityTroughIsSuitableFor(Entity entity, CreatureDiet diet) {
        return FixCreatureDiet(entity, diet);
    }
}
}
