namespace UnofficialBugfix.FixMealNonPerishTransition;

[HarmonyPatchCategory("unofficialbugfix")]
public static class FixMealNonPerishTransition
{
    /// Backport of 1.22 bugfix that prevents
    /// non-perish transitions such as drying
    /// from occuring for itemstacks in meals. <summary>

    #region GetContentInDummySlot
    public static ItemSlot BlockContainerGetContentInDummySlot(BlockContainer self, ItemSlot inslot, ItemStack itemstack)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        DummyInventory dummyInv = new DummyInventory(api);
        ItemSlot dummySlot = new DummySlot(itemstack, dummyInv);
        dummySlot.MarkedDirty += () => { inslot.Inventory?.DidModifyItemSlot(inslot); return true; };

        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mulByConfig) =>
        {
            float mul = inslot.Inventory?.InvokeTransitionSpeedDelegates(transType, stack, mulByConfig) ?? 1;
            if (transType != EnumTransitionType.Perish) mul = 0;
            return mul * self.GetContainingTransitionModifierContained(api.World, inslot, transType);
        };

        return dummySlot;
    }

    private static ItemSlot BlockCookedContainerBaseGetContentInDummySlot(BlockCookedContainerBase self, ItemSlot inslot, ItemStack itemstack)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        DummyInventory dummyInv = new DummyInventory(api);
        ItemSlot dummySlot = new DummySlot(itemstack, dummyInv);
        dummySlot.MarkedDirty += () => { inslot.Inventory?.DidModifyItemSlot(inslot); return true; };

        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mulByConfig) =>
        {
            float mul = inslot.Inventory?.InvokeTransitionSpeedDelegates(transType, stack, mulByConfig) ?? 1;
            if (transType != EnumTransitionType.Perish) mul = 0;
            return mul * self.GetContainingTransitionModifierContained(api.World, inslot, transType);
        };

        return dummySlot;
    }

    #endregion


    #region UpdateAndGetTransitionStates


    public static TransitionState[] CustomCollectibleObjectUpdateAndGetTransitionStates(CollectibleObject self, IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot is ItemSlotCreative) return null;

        ItemStack itemstack = inslot.Itemstack;

        TransitionableProperties[] propsm = self.GetTransitionableProperties(world, inslot.Itemstack, null);

        if (itemstack == null || propsm == null || propsm.Length == 0)
        {
            return null;
        }

        if (itemstack.Attributes == null)
        {
            itemstack.Attributes = new TreeAttribute();
        }

        if (itemstack.Attributes.GetBool("timeFrozen")) return null;


        if (!(itemstack.Attributes["transitionstate"] is ITreeAttribute))
        {
            itemstack.Attributes["transitionstate"] = new TreeAttribute();
        }

        ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["transitionstate"];


        float[] transitionedHours;
        float[] freshHours;
        float[] transitionHours;
        TransitionState[] states = new TransitionState[propsm.Length];

        if (!attr.HasAttribute("createdTotalHours"))
        {
            attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
            attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);

            freshHours = new float[propsm.Length];
            transitionHours = new float[propsm.Length];
            transitionedHours = new float[propsm.Length];

            for (int i = 0; i < propsm.Length; i++)
            {
                transitionedHours[i] = 0;
                freshHours[i] = propsm[i].FreshHours.nextFloat(1, world.Rand);
                transitionHours[i] = propsm[i].TransitionHours.nextFloat(1, world.Rand);
            }

            attr["freshHours"] = new FloatArrayAttribute(freshHours);
            attr["transitionHours"] = new FloatArrayAttribute(transitionHours);
            attr["transitionedHours"] = new FloatArrayAttribute(transitionedHours);
        }
        else
        {
            freshHours = (attr["freshHours"] as FloatArrayAttribute).value;
            transitionHours = (attr["transitionHours"] as FloatArrayAttribute).value;
            transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute).value;

            // A modder/dev might have added a new transition property since last time
            int gw = propsm.Length - freshHours.Length;
            if (gw > 0)
            {
                int i = freshHours.Length;
                while (i < propsm.Length)
                {
                    freshHours = freshHours.Append(propsm[i].FreshHours.nextFloat(1, world.Rand));
                    transitionHours = transitionHours.Append(propsm[i].TransitionHours.nextFloat(1, world.Rand));
                    transitionedHours = transitionedHours.Append(0);
                    i++;
                }
                (attr["freshHours"] as FloatArrayAttribute).value = freshHours;
                (attr["transitionHours"] as FloatArrayAttribute).value = transitionHours;
                (attr["transitionedHours"] as FloatArrayAttribute).value = transitionedHours;
            }
        }

        double lastUpdatedTotalHours = attr.GetDouble("lastUpdatedTotalHours");
        double nowTotalHours = world.Calendar.TotalHours;

        bool nowSpoiling = false;

        float hoursPassed = (float)(nowTotalHours - lastUpdatedTotalHours);

        for (int i = 0; i < propsm.Length; i++)
        {
            TransitionableProperties prop = propsm[i];
            if (prop == null) continue;

            float transitionRateMul = self.GetTransitionRateMul(world, inslot, prop.Type);

            if (hoursPassed > 0.05f) // Maybe prevents us from running into accumulating rounding errors?
            {
                float hoursPassedAdjusted = hoursPassed * transitionRateMul;
                transitionedHours[i] += hoursPassedAdjusted;

                /*if (api.World.Side == EnumAppSide.Server && inslot.Inventory.ClassName == "chest")
                {
                    Console.WriteLine(hoursPassed + " hours passed. " + inslot.Itemstack.Collectible.Code + " spoil by " + transitionRateMul + "x. Is inside " + inslot.Inventory.ClassName + " {0}/{1}", transitionedHours[i], freshHours[i]);
                }*/
            }

            // Don't advance non-perish transitions
            if (prop.Type != EnumTransitionType.Perish)
            {
                freshHours[i] = propsm[i].FreshHours.nextFloat(1, world.Rand);
                transitionedHours[i] = 0;
            }

            float freshHoursLeft = Math.Max(0, freshHours[i] - transitionedHours[i]);
            float transitionLevel = Math.Max(0, transitionedHours[i] - freshHours[i]) / transitionHours[i];

            // Don't continue transitioning spoiled foods
            if (transitionLevel > 0)
            {
                if (prop.Type == EnumTransitionType.Perish)
                {
                    nowSpoiling = true;
                }
                else
                {
                    if (nowSpoiling) continue;
                }
            }

            if (transitionLevel >= 1 && world.Side == EnumAppSide.Server)
            {
                ItemStack newstack = self.OnTransitionNow(inslot, propsm[i]);

                if (newstack.StackSize <= 0)
                {
                    inslot.Itemstack = null;
                }
                else
                {
                    itemstack.SetFrom(newstack);
                }

                inslot.MarkDirty();

                // Only do one transformation, then do the next one next update
                // This does fully not respect time-fast-forward, so that should be fixed some day
                break;
            }

            states[i] = new TransitionState()
            {
                FreshHoursLeft = freshHoursLeft,
                TransitionLevel = Math.Min(1, transitionLevel),
                TransitionedHours = transitionedHours[i],
                TransitionHours = transitionHours[i],
                FreshHours = freshHours[i],
                Props = prop
            };

            //if (transitionRateMul > 0) break; // Only do one transformation at the time (i.e. food can not cure and perish at the same time) - Tyron 9/oct 2020, but why not at the same time? We need it for cheese ripening
        }

        if (hoursPassed > 0.05f)
        {
            attr.SetDouble("lastUpdatedTotalHours", nowTotalHours);
        }

        return states.Where(s => s != null).OrderBy(s => (int)s.Props.Type).ToArray();
    }

    public static TransitionState[] CustomBlockContainerUpdateAndGetTransitionStates(BlockContainer self, IWorldAccessor world, ItemSlot inslot)
    {
        if (inslot is ItemSlotCreative) return CustomCollectibleObjectUpdateAndGetTransitionStates(self, world, inslot);

        ItemStack[] stacks = self.GetContents(world, inslot.Itemstack);

        if (inslot.Itemstack.Attributes.GetBool("timeFrozen"))
        {
            foreach (var stack in stacks) stack?.Attributes.SetBool("timeFrozen", true);
            return null;
        }

        if (stacks != null)
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var stack = stacks[i];
                if (stack == null) continue;

                ItemSlot dummySlot = BlockContainerGetContentInDummySlot(self, inslot, stack);
                CustomCollectibleObjectUpdateAndGetTransitionStates(stack.Collectible, world, dummySlot);
                if (dummySlot.Itemstack == null)
                {
                    stacks[i] = null;
                }
            }
        }

        self.SetContents(inslot.Itemstack, stacks);

        return CustomCollectibleObjectUpdateAndGetTransitionStates(self, world, inslot);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AppendPerishableInfoText")]
    private static extern float CollectibleObjectAppendSinglePerishableInfoText(CollectibleObject self, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, TransitionState state, bool nowSpoiling);

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

        TransitionState[]? states = CustomBlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, mealStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(BlockContainerGetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(mealStack, stacks);

            mealStack.Attributes.RemoveAttribute("recipeCode");
            mealStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, mealStack.Attributes.TryGetFloat("quantityServings") ?? 1);
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

        TransitionState[]? states = CustomBlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, cookedContStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(BlockCookedContainerBaseGetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(cookedContStack, stacks);

            cookedContStack.Attributes.RemoveAttribute("recipeCode");
            cookedContStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, cookedContStack.Attributes.TryGetFloat("quantityServings") ?? 1);
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

        TransitionState[]? states = CustomBlockContainerUpdateAndGetTransitionStates(self, world, inslot);

        stacks = self.GetNonEmptyContents(world, crockStack);
        if (stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
        {
            for (int i = 0; i < stacks.Length; i++)
            {
                var transProps = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
                var spoilProps = transProps?.FirstOrDefault(props => props.Type == EnumTransitionType.Perish);

                if (spoilProps == null) continue;

                stacks[i] = stacks[i].Collectible.OnTransitionNow(BlockCookedContainerBaseGetContentInDummySlot(self, inslot, stacks[i]), spoilProps);
            }
            self.SetContents(crockStack, stacks);

            crockStack.Attributes.RemoveAttribute("recipeCode");
            crockStack.Attributes.RemoveAttribute("quantityServings");
        }

        foreach (var stack in stacks)
        {
            stack.StackSize /= (int)Math.Max(1, crockStack.Attributes.TryGetFloat("quantityServings") ?? 1);
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
           CustomCollectibleObjectUpdateAndGetTransitionStates(stack.Collectible, api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            CollectibleObjectAppendSinglePerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
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

        DummyInventory dummyInv = new DummyInventory(api);

        ItemSlot dummySlot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);
        dummyInv.OnAcquireTransitionSpeed += (transType, stack, mul) =>
        {
            float invMul = inSlot.Inventory?.GetTransitionSpeedMul(transType, crockStack) ?? 1;
            if (transType != EnumTransitionType.Perish) mul = 0;
            return invMul * self.GetContainingTransitionModifierContained(world, inSlot, transType);
        };

        if (dummySlot.Itemstack is ItemStack stack &&
            CustomCollectibleObjectUpdateAndGetTransitionStates(stack.Collectible, api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            CollectibleObjectAppendSinglePerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
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
            if (transType != EnumTransitionType.Perish) mul = 0;
            return invMul * self.GetContainingTransitionModifierContained(world, inSlot, transType);
        };

        if (dummySlot.Itemstack is ItemStack stack &&
            CustomCollectibleObjectUpdateAndGetTransitionStates(stack.Collectible, api.World, dummySlot)?.FirstOrDefault(state => state.Props.Type is EnumTransitionType.Perish) is TransitionState perishState)
        {
            CollectibleObjectAppendSinglePerishableInfoText(stack.Collectible, dummySlot, dsc, world, perishState, false);
        }
    }
    #endregion
}
