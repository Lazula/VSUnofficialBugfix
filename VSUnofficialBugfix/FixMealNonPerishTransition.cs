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
}
