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

// This patch technically works but seems to have something missing to make it
// do what it's supposed to.

namespace UnofficialBugfix.FixEntityBehaviorMultiplyEatAnyway
{
[HarmonyPatchCategory("unofficialbugfix")]
public static class FixEntityBehaviorMultiplyEatAnyway {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "typeAttributes")]
    private static extern ref JsonObject GetTypeAttributes(EntityBehaviorMultiply bem);

    // BUGFIX: The eatAnyway attribute isn't loaded properly.
    // EntityBehaviorMultiplyBase has its own eatAnyway field (and it's correct),
    // but it's private and EntityBehaviorMultiply has its own private field that
    // can't be loaded from json, making it always false.
    //
    // We just use the json object manually here because that's still loaded.
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(EntityBehaviorMultiply))]
    [HarmonyPatch("ShouldEat", MethodType.Getter)]
    public static void OverrideEntityBehaviorMultiplyEatAnyway(EntityBehaviorMultiply __instance, bool __result) {
        // No recheck needed for anything else.
        bool eatAnyway = GetTypeAttributes(__instance)["eatAnyway"].AsBool();
        if (eatAnyway && !__result)
        {
            // This method could be one line but logging is conditional.
            //UnofficialBugfix.UnofficialBugfixModSystem.Logger.Notification("Overrode eatAnyway with type attribute");
            __result = true;
        }
    }
}
}
