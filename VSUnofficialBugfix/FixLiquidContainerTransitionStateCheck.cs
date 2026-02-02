namespace UnofficialBugfix.FixLiquidContainerTransitionStateCheck;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixLiquidContainerTransitionStateCheck
{
    /// BUG: Liquid containers check their own transition
    /// state instead of that of the contents.
    /// FIX: Use GetContent to see the actual spoilage. Move
    /// the check into GetNutritionProperties so that spoilage
    /// is reflected in the item tooltip in addition to
    /// actual consumption.
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("GetNutritionProperties")]
    [HarmonyPatch(new Type[] { typeof(IWorldAccessor), typeof(ItemStack), typeof(Entity) })]
    public static void FixLiquidContainerGetNutritionProperties(BlockLiquidContainerBase __instance, ref FoodNutritionProperties __result, IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        CustomLiquidContainerGetNutritionProperties(__instance, ref __result, world, itemstack, forEntity);
    }

    public static void CustomLiquidContainerGetNutritionProperties(BlockLiquidContainerBase self, ref FoodNutritionProperties __result, IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        ItemStack contentStack = self.GetContent(itemstack);

        if (__result == null || contentStack == null || BlockLiquidContainerBase.GetContainableProps(contentStack) == null)
        {
            return;
        }

        float satLossMul = 1;
        float healthLossMul = 1;

        if (forEntity is EntityPlayer player)
        {
            ItemSlot dummySlot = new DummySlot(contentStack);
            TransitionState state = contentStack.Collectible.UpdateAndGetTransitionState(api.World, dummySlot, EnumTransitionType.Perish);
            float spoilState = state != null ? state.TransitionLevel : 0;

            satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, contentStack, player);
            healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, contentStack, player);
        }

        __result.Satiety *= satLossMul;
        __result.Health *= healthLossMul;
    }

    // We have to overwrite the old version of tryEatStop
    // because fixing this bug involved moving the check
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("tryEatStop")]
    [HarmonyPatch(new Type[] { typeof(float), typeof(ItemSlot), typeof(EntityAgent) })]
    public static bool FixTryEatStop(BlockLiquidContainerBase __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        CustomLiquidContainerTryEatStop(__instance, secondsUsed, slot, byEntity);
        return false;
    }

    public static void CustomLiquidContainerTryEatStop(BlockLiquidContainerBase self, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        FoodNutritionProperties nutriProps = self.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);

        if (byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
        {
            float drinkCapLitres = 1f;

            float litresEach = self.GetCurrentLitres(slot.Itemstack);
            float litresTotal = litresEach * slot.StackSize;

            if (litresEach > drinkCapLitres)
            {
                nutriProps.Satiety /= litresEach;
                nutriProps.Health /= litresEach;
            }

            byEntity.ReceiveSaturation(nutriProps.Satiety, nutriProps.FoodCategory);

            IPlayer player = null;
            if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            float litresToDrink = Math.Min(drinkCapLitres, litresTotal);
            self.TryTakeLiquid(slot.Itemstack, litresToDrink / slot.Itemstack.StackSize);

            float healthChange = nutriProps.Health;

            float intox = byEntity.WatchedAttributes.GetFloat("intoxication");
            byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intox + nutriProps.Intoxication));

            if (healthChange != 0)
            {
                byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
            }

            slot.MarkDirty();
            player.InventoryManager.BroadcastHotbarSlot();
        }
    }
}
