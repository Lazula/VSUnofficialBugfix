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

namespace UnofficialBugfix.FixBETroughBlockInfo
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixBETroughBlockInfo {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "contentCode")]
    private static extern ref string GetContentCode(BlockEntityTrough trough);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ResolveWildcardContent")]
    private static extern ItemStack ResolveWildcardContent(BlockEntityTrough trough, ContentConfig config, IWorldAccessor worldAccessor);

    // BUGFIX: Trough does not take trough suitability into account, only creature diet
    // Clear the previous blockinfo and write a new one
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(BlockEntityTrough))]
    [HarmonyPatch("GetBlockInfo")]
    public static void OverrideBlockEntityTroughGetBlockInfo(BlockEntityTrough __instance, IPlayer forPlayer, StringBuilder dsc) {
        dsc.Clear();

        ItemStack firstStack = __instance.Inventory[0].Itemstack;

        if (__instance.contentConfigs == null)
        {
            return;
        }

        ContentConfig config = __instance.contentConfigs.FirstOrDefault(c => c.Code == GetContentCode(__instance));

        if (config == null && firstStack != null)
        {
            dsc.AppendLine(firstStack.StackSize + "x " + firstStack.GetName());
        }

        if (config == null || firstStack == null) return;

        int fillLevel = firstStack.StackSize / config.QuantityPerFillLevel;

        dsc.AppendLine(Lang.Get("Portions: {0}", fillLevel));

        ItemStack contentsStack = config.Content.ResolvedItemstack ?? ResolveWildcardContent(__instance, config, forPlayer.Entity.World);

        if (contentsStack == null) return;

        dsc.AppendLine(Lang.Get(contentsStack.GetName()));

        HashSet<string> creatureNames = new HashSet<string>();
        foreach (var entityType in __instance.Api.World.EntityTypes)
        {
            var attr = entityType.Attributes;
            if (attr == null || attr["creatureDiet"].Exists == false) continue;

            var diet = attr["creatureDiet"].AsObject<CreatureDiet>();
            if (diet.Matches(contentsStack) && __instance.Block is BlockTroughBase trough)
            {
                if(!trough.UnsuitableForEntity(entityType.Code.Path)) {
                    string code = attr?["handbook"]["groupcode"].AsString() ?? "item-creature-" + entityType.Code;
                    creatureNames.Add(Lang.Get(code));
                }
            }
        }

        if (creatureNames.Count > 0)
        {
            dsc.AppendLine(Lang.Get("trough-suitable", string.Join(", ", creatureNames)));
        }
        else
        {
            dsc.AppendLine(Lang.Get("trough-unsuitable"));
        }
    }
}
}
