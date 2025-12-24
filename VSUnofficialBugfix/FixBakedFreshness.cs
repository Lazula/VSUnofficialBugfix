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

namespace UnofficialBugfix.FixBakedFreshness
{

    [HarmonyPatchCategory("unofficialbugfix")]
    internal static class FixBakedFreshness
    {
        // Separated from other clay oven fixes because
        // we have to reach into pie too, and I would
        // rather keep the oven-only stuff to its own
        // file.



        /// Pie already has code to handle this, but it doesn't
        /// actually work... We need to remove it because OnBaked
        /// is called after we set freshness in the oven. This
        /// is so mods can do what they want with the freshness.
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockPie))]
        [HarmonyPatch("OnBaked")]
        public static bool FixPieOnBaked(BlockPie __instance, ref ICoreAPI ___api, ItemStack oldStack, ItemStack newStack)
        {
            // Copy over properties and bake the contents
            newStack.Attributes["contents"] = oldStack.Attributes["contents"];
            newStack.Attributes.SetInt("pieSize", oldStack.Attributes.GetAsInt("pieSize"));
            newStack.Attributes.SetString("topCrustType", BlockPie.GetTopCrustType(oldStack));
            newStack.Attributes.SetInt("bakeLevel", oldStack.Attributes.GetAsInt("bakeLevel", 0) + 1);

            ItemStack[] stacks = __instance.GetContents(___api.World, newStack);
            __instance.SetContents(newStack, stacks);
            return false;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "updateMesh")]
        private static extern void updateMesh(BlockEntityDisplay instance, int index);

        /// BUG: Clay oven creates entirely new items
        /// when cooking, causing them to be completely
        /// fresh.
        /// FIX: Use CarryOverFreshness to create the
        /// same behavior as other food processes.
        [HarmonyPrefix()]
        [HarmonyPatch(typeof(BlockEntityOven))]
        [HarmonyPatch("IncrementallyBake")]
        public static bool CarryOverFreshnessOnBake(BlockEntityOven __instance, ref OvenItemData[] ___bakingData, ref InventoryOven ___ovenInv, float dt, int slotIndex)
        {
            ItemSlot slot = __instance.Inventory[slotIndex];
            OvenItemData bakeData = ___bakingData[slotIndex];

            float targetTemp = bakeData.BrowningPoint;
            if (targetTemp == 0) targetTemp = 160f;  //prevents any possible divide by zero
            float diff = bakeData.temp / targetTemp;
            float timeFactor = bakeData.TimeToBake;
            if (timeFactor == 0) timeFactor = 1;  //prevents any possible divide by zero
            float delta = GameMath.Clamp((int)diff, 1, 30) * dt / timeFactor;

            float currentLevel = bakeData.BakedLevel;
            if (bakeData.temp > targetTemp)
            {
                currentLevel = bakeData.BakedLevel + delta;
                bakeData.BakedLevel = currentLevel;
            }

            var bakeProps = BakingProperties.ReadFrom(slot.Itemstack);
            float levelFrom = bakeProps?.LevelFrom ?? 0f;
            float levelTo = bakeProps?.LevelTo ?? 1f;
            float startHeightMul = bakeProps?.StartScaleY ?? 1f;
            float endHeightMul = bakeProps?.EndScaleY ?? 1f;

            float progress = GameMath.Clamp((currentLevel - levelFrom) / (levelTo - levelFrom), 0, 1);
            float heightMul = GameMath.Mix(startHeightMul, endHeightMul, progress);
            float nowHeightMulStaged = (int)(heightMul * BlockEntityOven.BakingStageThreshold) / (float)BlockEntityOven.BakingStageThreshold;

            bool reDraw = nowHeightMulStaged != bakeData.CurHeightMul;

            bakeData.CurHeightMul = nowHeightMulStaged;

            // see if increasing the partBaked by delta, has moved this stack up to the next "bakedStage", i.e. a different item

            if (currentLevel > levelTo)
            {
                float nowTemp = bakeData.temp;
                string resultCode = bakeProps?.ResultCode;

                if (resultCode != null)
                {
                    ItemStack resultStack = null;
                    if (slot.Itemstack.Class == EnumItemClass.Block)
                    {
                        Block block = __instance.Api.World.GetBlock(new AssetLocation(resultCode));
                        if (block != null)
                        {
                            resultStack = new ItemStack(block);
                        }
                    }
                    else
                    {
                        Item item = __instance.Api.World.GetItem(new AssetLocation(resultCode));
                        if (item != null) resultStack = new ItemStack(item);
                    }

                    if (resultStack != null)
                    {
                        TransitionableProperties[] tprops = resultStack.Collectible.GetTransitionableProperties(__instance.Api.World, slot.Itemstack, null);
                        TransitionableProperties perishProps = tprops?.FirstOrDefault(p => p.Type == EnumTransitionType.Perish);

                        // Carry over freshness
                        if (perishProps != null)
                        {
                            CollectibleObject.CarryOverFreshness(__instance.Api, slot, resultStack, perishProps);
                        }

                        ___ovenInv[slotIndex].Itemstack.Collectible.GetCollectibleInterface<IBakeableCallback>()?.OnBaked(___ovenInv[slotIndex].Itemstack, resultStack);

                        ___ovenInv[slotIndex].Itemstack = resultStack;
                        ___bakingData[slotIndex] = new OvenItemData(resultStack);
                        ___bakingData[slotIndex].temp = nowTemp;

                        reDraw = true;
                    }
                }
                else
                {
                    // Allow the oven also to 'smelt' low-temperature bakeable items which do not have specific baking properties

                    ItemSlot result = new DummySlot(null);
                    if (slot.Itemstack.Collectible.CanSmelt(__instance.Api.World, ___ovenInv, slot.Itemstack, null))
                    {
                        slot.Itemstack.Collectible.DoSmelt(__instance.Api.World, ___ovenInv, ___ovenInv[slotIndex], result);
                        if (!result.Empty)
                        {
                            ___ovenInv[slotIndex].Itemstack = result.Itemstack;
                            ___bakingData[slotIndex] = new OvenItemData(result.Itemstack);
                            ___bakingData[slotIndex].temp = nowTemp;
                            reDraw = true;
                        }
                    }
                }
            }

            if (reDraw)
            {
                updateMesh(__instance, slotIndex);
                __instance.MarkDirty(true);
            }

            return false;
        }
    }
}
