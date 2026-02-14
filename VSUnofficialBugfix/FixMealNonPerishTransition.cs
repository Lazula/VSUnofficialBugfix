namespace UnofficialBugfix.FixMealNonPerishTransition;

[HarmonyPatchCategory("unofficialbugfix")]
public static class FixMealNonPerishTransition
{

    /// Backport of 1.22 bugfix that prevents
    /// non-perish transitions such as drying
    /// from occuring for itemstacks in meals.

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(BlockContainer))]
    [HarmonyPatch("UpdateAndGetTransitionStates")]
    public static TransitionState[] BlockContainerUpdateAndGetTransitionStates(BlockContainer __instance, IWorldAccessor world, ItemSlot inslot) => null;

    /// Backport of a new method on BlockContainer in 1.22
    /// 
    /// This is where we overwrite the non-perish multipliers.
    private static ItemSlot GetContentInDummySlot(BlockContainer self, ItemSlot inslot, ItemStack itemstack)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();

        ItemSlot dummySlot;
        DummyInventory dummyInv = new DummyInventory(api);
        dummySlot = new DummySlot(itemstack, dummyInv);
        dummySlot.MarkedDirty += () => { inslot.Inventory?.DidModifyItemSlot(inslot); return true; };

        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mulByConfig) =>
        {
            float mul = inslot.Inventory?.InvokeTransitionSpeedDelegates(transType, stack, mulByConfig) ?? 1;
            return mul * self.GetContainingTransitionModifierContained(api.World, inslot, transType);
        };

        return dummySlot;
    }

#nullable enable
    #region UpdateAndGetTransitionStates

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockMeal))]
    [HarmonyPatch("UpdateAndGetTransitionStates")]
    public static bool FixBlockMealUpdateAndGetTransitionStates(BlockMeal __instance, ref TransitionState[]? __result, IWorldAccessor world, ItemSlot inslot)
    {
        __result = CustomBlockMealUpdateAndGetTransitionStates(__instance, world, inslot);
        return false;
    }

    public static TransitionState[]? CustomBlockMealUpdateAndGetTransitionStates(BlockMeal self, IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot.Itemstack is not ItemStack mealStack) return null;

        ItemStack[] stacks = self.GetNonEmptyContents(world, mealStack);
        foreach (var stack in stacks)
        {
            stack.StackSize *= (int)Math.Max(1, mealStack.Attributes.TryGetFloat("quantityServings") ?? 1);
        }

        self.SetContents(mealStack, stacks);

        TransitionState[]? states = BlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, mealStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(GetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(mealStack, stacks);

            mealStack.Attributes.RemoveAttribute("recipeCode");
            mealStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, mealStack.Attributes.TryGetFloat("quantityServings") ?? 1);

            if (stack.Collectible?.GetTransitionableProperties(world, stack, null) is TransitionableProperties[] allProps)
            {
                foreach (TransitionableProperties tprops in allProps)
                {
                    if (tprops.Type != EnumTransitionType.Perish)
                    {
                        stack.Collectible.SetTransitionState(stack, tprops.Type, 0);
                    }
                }
            }
        }

        self.SetContents(mealStack, stacks);

        if (stacks.Length == 0 &&
            AssetLocation.CreateOrNull(self.Attributes?["eatenBlock"]?.AsString()) is AssetLocation loc &&
            world.GetBlock(loc) is Block block)
        {
            inslot.Itemstack = new ItemStack(block);
            inslot.MarkDirty();
        }

        return states;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockCookedContainer))]
    [HarmonyPatch("UpdateAndGetTransitionStates")]
    public static bool FixBlockCookedContainerUpdateAndGetTransitionStates(BlockCookedContainer __instance, ref TransitionState[]? __result, IWorldAccessor world, ItemSlot inslot)
    {
        __result = CustomBlockCookedContainerUpdateAndGetTransitionStates(__instance, world, inslot);
        return false;
    }

    public static TransitionState[]? CustomBlockCookedContainerUpdateAndGetTransitionStates(BlockCookedContainer self, IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot.Itemstack is not ItemStack cookedContStack) return null;

        ItemStack[] stacks = self.GetNonEmptyContents(world, cookedContStack);
        foreach (var stack in stacks)
        {
            stack.StackSize *= (int)Math.Max(1, cookedContStack.Attributes.TryGetFloat("quantityServings") ?? 1);
        }

        self.SetContents(cookedContStack, stacks);

        TransitionState[]? states = BlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, cookedContStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(GetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(cookedContStack, stacks);

            cookedContStack.Attributes.RemoveAttribute("recipeCode");
            cookedContStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, cookedContStack.Attributes.TryGetFloat("quantityServings") ?? 1);

            if (stack.Collectible?.GetTransitionableProperties(world, stack, null) is TransitionableProperties[] allProps)
            {
                foreach (TransitionableProperties tprops in allProps)
                {
                    if (tprops.Type != EnumTransitionType.Perish)
                    {
                        stack.Collectible.SetTransitionState(stack, tprops.Type, 0);
                    }
                }
            }
        }

        self.SetContents(cookedContStack, stacks);

        if (stacks.Length == 0 && self.Attributes?["emptiedBlockCode"]?.AsString() is string emptiedBlockCode && world.GetBlock(new AssetLocation(emptiedBlockCode)) is Block block)
        {
            inslot.Itemstack = new ItemStack(block);
            inslot.MarkDirty();
        }

        return states;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockCrock))]
    [HarmonyPatch("UpdateAndGetTransitionStates")]
    public static bool FixBlockCrockUpdateAndGetTransitionStates(BlockCrock __instance, ref TransitionState[]? __result, IWorldAccessor world, ItemSlot inslot)
    {
        __result = CustomBlockCrockUpdateAndGetTransitionStates(__instance, world, inslot);
        return false;
    }

    public static TransitionState[]? CustomBlockCrockUpdateAndGetTransitionStates(BlockCrock self, IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot.Itemstack is not ItemStack crockStack) return null;

        ItemStack[] stacks = self.GetNonEmptyContents(world, crockStack);
        foreach (var stack in stacks)
        {
            stack.StackSize *= (int)Math.Max(1, crockStack.Attributes.TryGetFloat("quantityServings") ?? 1);
        }

        self.SetContents(crockStack, stacks);

        TransitionState[]? states = BlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, crockStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(GetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(crockStack, stacks);

            crockStack.Attributes.RemoveAttribute("recipeCode");
            crockStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, crockStack.Attributes.TryGetFloat("quantityServings") ?? 1);

            if (stack.Collectible?.GetTransitionableProperties(world, stack, null) is TransitionableProperties[] allProps)
            {
                foreach (TransitionableProperties tprops in allProps)
                {
                    if (tprops.Type != EnumTransitionType.Perish)
                    {
                        stack.Collectible.SetTransitionState(stack, tprops.Type, 0);
                    }
                }
            }
        }

        self.SetContents(crockStack, stacks);

        return states;
    }

    #endregion
    #region GetHeldItemInfo

    /// Fix the display issue

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(BlockContainer))]
    [HarmonyPatch("GetHeldItemInfo")]
    public static void BlockContainerGetHeldItemInfo(BlockContainer __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { }

    // Single-state text is protected
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AppendPerishableInfoText")]
    private static extern float ProtectedAppendPerishableInfoText(CollectibleObject self, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, TransitionState state, bool nowSpoiling);


    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockMeal))]
    [HarmonyPatch("GetHeldItemInfo")]
    public static bool FixBlockMealGetHeldItemInfo(BlockMeal __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        CustomBlockMealGetHeldItemInfo(__instance, inSlot, dsc, world, withDebugInfo);
        return false;
    }

    public static void CustomBlockMealGetHeldItemInfo(BlockMeal self, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        bool displayContentsInfo = Traverse.Create(self).Field("displayContentsInfo").Field("api").GetValue<bool>();

        if (inSlot.Itemstack is not ItemStack mealStack) return;
        BlockContainerGetHeldItemInfo(self, inSlot, dsc, world, withDebugInfo);
        float temp = self.GetTemperature(world, mealStack);
        if (temp > 20)
        {
            dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
        }

        CookingRecipe? recipe = self.GetCookingRecipe(world, mealStack);

        ItemStack[] stacks = self.GetNonEmptyContents(world, mealStack);
        DummyInventory dummyInv = new DummyInventory(api);
        ItemSlot dummySlot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);

        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) =>
        {
            float invMul = inSlot.Inventory?.GetTransitionSpeedMul(transType, mealStack) ?? 1;
            if (transType != EnumTransitionType.Perish) mul = 0;
            return invMul * self.GetContainingTransitionModifierContained(world, inSlot, transType);
        };

        if (dummySlot.Itemstack is ItemStack stack &&
            stack.Collectible.UpdateAndGetTransitionStates(api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            ProtectedAppendPerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
        }

        float servings = self.GetQuantityServings(world, mealStack);

        if (recipe != null)
        {
            if (Math.Round(servings, 1) < 0.05)
            {
                dsc.AppendLine(Lang.Get("{1}% serving of {0}", recipe.GetOutputName(world, stacks).UcFirst(), Math.Round(servings * 100, 0)));
            }
            else
            {
                dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks).UcFirst()));
            }

        }
        else if (mealStack.Attributes.HasAttribute("quantityServings"))
        {
            if (Math.Round(servings, 1) < 0.05)
            {
                dsc.AppendLine(Lang.Get("meal-servingsleft-percent", Math.Round(servings * 100, 0)));
            }
            else dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(servings, 1)));
        }
        else if (displayContentsInfo && !MealMeshCache.ContentsRotten(stacks))
        {
            dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("meal-ingredientlist-" + stacks.Length, stacks.Select(stack => Lang.Get("{0}x {1}", stack.StackSize, stack.GetName())))));
        }

        if (!MealMeshCache.ContentsRotten(stacks))
        {
            // We'll calculate for the serving size if it's less than 1.
            servings = Math.Min(1, servings);
            float[] nmul = self.GetNutritionHealthMul(null, inSlot, null);
            string facts = self.GetContentNutritionFacts(world, inSlot, stacks, null, recipe == null, servings * nmul[0], servings * nmul[1]);

            if (facts != null)
            {
                dsc.Append(facts);
            }
        }
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockCrock))]
    [HarmonyPatch("GetHeldItemInfo")]
    public static bool FixBlockCrockGetHeldItemInfo(BlockCrock __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        CustomBlockCrockGetHeldItemInfo(__instance, inSlot, dsc, world, withDebugInfo);
        return false;
    }

    public static void CustomBlockCrockGetHeldItemInfo(BlockCrock self, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();

        BlockContainerGetHeldItemInfo(self, inSlot, dsc, world, withDebugInfo);

        if (inSlot.Itemstack is not ItemStack crockStack) return;

        CookingRecipe? recipe = self.GetCookingRecipe(world, crockStack);
        ItemStack[]? stacks = self.GetNonEmptyContents(world, crockStack);

        if (stacks == null || stacks.Length == 0)
        {
            dsc.AppendLine(Lang.Get("Empty"));

            if (crockStack.Attributes.GetBool("sealed") == true)
            {
                dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
            }

            return;
        }

        DummyInventory dummyInv = new DummyInventory(api);
        ItemSlot dummySlot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);

        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) =>
        {
            float invMul = inSlot.Inventory?.GetTransitionSpeedMul(transType, crockStack) ?? 1;
            if (transType != EnumTransitionType.Perish) invMul = 0;
            return invMul * self.GetContainingTransitionModifierContained(world, inSlot, transType);
        };


        if (recipe != null)
        {
            double servings = crockStack.Attributes.GetDecimal("quantityServings");

            if (recipe != null)
            {
                if (servings == 1)
                {
                    dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks)));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("{0} servings of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks)));
                }
            }

            string? facts = BlockMeal.AllMealBowls(api)?[0]?.GetContentNutritionFacts(world, inSlot, null);
            if (facts != null)
            {
                dsc.Append(facts);
            }



        }
        else if (crockStack.Attributes.HasAttribute("quantityServings"))
        {
            double servings = crockStack.Attributes.GetDecimal("quantityServings");

            if (Math.Round(servings, 1) < 0.05)
            {
                dsc.AppendLine(Lang.Get("meal-servingsleft-percent", Math.Round(servings * 100, 0)));
            }
            else dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(servings, 1)));
        }
        else if (!MealMeshCache.ContentsRotten(stacks))
        {
            dsc.AppendLine(Lang.Get("Contents: {0}", Lang.Get("meal-ingredientlist-" + stacks.Length, stacks.Select(stack => Lang.Get("{0}x {1}", stack.StackSize, stack.GetName())))));
        }

        if (dummySlot.Itemstack is ItemStack stack &&
            stack.Collectible.UpdateAndGetTransitionStates(api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            ProtectedAppendPerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
        }

        if (crockStack.Attributes.GetBool("sealed"))
        {
            dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
        }
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockCookedContainer))]
    [HarmonyPatch("GetHeldItemInfo")]
    public static bool FixBlockCookedContainerGetHeldItemInfo(BlockCookedContainer __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        CustomBlockCookedContainerGetHeldItemInfo(__instance, inSlot, dsc, world, withDebugInfo);
        return false;
    }

    public static void CustomBlockCookedContainerGetHeldItemInfo(BlockCookedContainer self, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        if (inSlot.Itemstack is not ItemStack cookedContStack) return;
        BlockContainerGetHeldItemInfo(self, inSlot, dsc, world, withDebugInfo);
        float temp = self.GetTemperature(world, cookedContStack);
        if (temp > 20)
        {
            dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
        }

        CookingRecipe? recipe = self.GetMealRecipe(world, cookedContStack);
        float servings = cookedContStack.Attributes.GetFloat("quantityServings");

        ItemStack[] stacks = self.GetNonEmptyContents(world, cookedContStack);


        if (recipe != null)
        {
            string message;
            string outputName = recipe.GetOutputName(world, stacks);
            if (recipe.CooksInto != null)
            {
                message = "nonfood-portions";
            }
            else
            {
                message = "{0} servings of {1}";
            }
            dsc.AppendLine(Lang.Get(message, Math.Round(servings, 1), outputName));
        }

        string? nutriFacts = BlockMeal.AllMealBowls(api)?[0]?.GetContentNutritionFacts(api.World, inSlot, stacks, null);

        if (nutriFacts != null && recipe?.CooksInto == null) dsc.AppendLine(nutriFacts);

        if (cookedContStack.Attributes.GetBool("timeFrozen")) return;

        DummyInventory dummyInv = new DummyInventory(api);

        ItemSlot dummySlot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);
        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) =>
        {
            float invMul = inSlot.Inventory?.GetTransitionSpeedMul(transType, cookedContStack) ?? 1;
            if (transType != EnumTransitionType.Perish) invMul = 0;
            return invMul * self.GetContainingTransitionModifierContained(world, inSlot, transType);
        };

        if (dummySlot.Itemstack is ItemStack stack &&
            stack.Collectible.UpdateAndGetTransitionStates(api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            ProtectedAppendPerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
        }
    }
    #endregion
}
