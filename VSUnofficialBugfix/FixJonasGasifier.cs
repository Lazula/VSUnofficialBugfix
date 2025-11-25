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

namespace UnofficialBugfix.FixJonasGasifier
{

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixJonasGasifier {
    /// TWEAK: Gasifier inventory is a regular slot, accepting
    /// up to 64 coal at once. This lasts for 21.6 days and the
    /// player should not be put at risk of such a huge amount
    /// of waste. 4 coal lasts for 1.3 days, same as a full coal
    /// pile with 16 lit in the boiler.
    /// 
    /// Included because it's part of my PR despite not
    /// objectively being a bug.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BEBehaviorJonasGasifier))]
    [HarmonyPatch("Interact")]
    public static bool ShrinkInventory(BEBehaviorJonasGasifier __instance, ref InventoryGeneric ___inventory, IPlayer byPlayer, BlockSelection blockSel) {
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (slot.Empty) return false;

        if (slot.Itemstack.Collectible.CombustibleProps != null && slot.Itemstack.Collectible.CombustibleProps.BurnTemperature >= 1100 && ___inventory[0].StackSize < 4)
        {
            int moved = slot.TryPutInto(__instance.Api.World, ___inventory[0]);
            if (moved > 0)
            {
                __instance.Api.World.PlaySoundAt(new AssetLocation("sounds/block/charcoal"), __instance.Pos, 0, byPlayer);
                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                slot.MarkDirty();
                __instance.Blockentity.MarkDirty(true);
            }
        }
        return false;
    }

    /// BUG: Once entered, the loop condition hoursPassed > 8
    /// is always true because it is never modified inside the
    /// loop, causing the gasifier to eat its entire inventory.
    /// FIX: Decrement hoursPassed in the loop.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BEBehaviorJonasGasifier))]
    [HarmonyPatch("onTick")]
    public static bool FixFuelConsumption(BEBehaviorJonasGasifier __instance, ref InventoryGeneric ___inventory, ref double ___burnStartTotalHours, ref bool ___lit, float dt) {
        if (___lit)
        {
            double hoursPassed = Math.Min(2400, __instance.Api.World.Calendar.TotalHours - ___burnStartTotalHours);
            while (hoursPassed > 8)
            {
                hoursPassed -= 8;
                ___burnStartTotalHours += 8;
                ___inventory[0].TakeOut(1);
                if (___inventory.Empty)
                {
                    ___lit = false;
                    __instance.UpdateState();
                    break;
                }
            }
        }
        return false;
    }
}
}
