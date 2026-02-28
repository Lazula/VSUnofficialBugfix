namespace UnofficialBugfix.FixMilkingLagDesync;

[HarmonyPatchCategory("unofficialbugfix")]
public static class FixMilkingLagDesync
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "lastMilkedTotalHours")]
    private static extern ref double LastMilkedTotalHours(EntityBehaviorMilkable self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "lactatingDaysAfterBirth")]
    private static extern ref double LactatingDaysAfterBirth(EntityBehaviorMilkable self);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bhmul")]
    private static extern ref EntityBehaviorMultiply BHM(EntityBehaviorMilkable self);

    /// BUG: Pausing or experiencing server lag causes
    /// the client to erroneously enter
    /// BehaviorMilkingContainer.Stop. This calls
    /// behaviorMilkable.MilkingComplete which will
    /// set the last milking time. However, because the
    /// server correctly knows that this event shouldn't
    /// happen, it will not set the milking time and will
    /// not give the player the milk. The client will
    /// still think that the time should have been updated,
    /// creating a desync where the client must rejoin the
    /// world to receive correct information from the server.
    ///
    /// FIX: Use WatchedAttributes to force syncing from
    /// the server. The vanilla patch uses a property to
    /// make this easier, but we don't have that option here.

    private static void WriteMilkTime(EntityBehaviorMilkable self, double lastMilkedTotalHours)
    {
        self.entity.WatchedAttributes.SetFloat("lastMilkedTotalHours", (float)lastMilkedTotalHours);
        LastMilkedTotalHours(self) = lastMilkedTotalHours;
    }

    private static void ReadMilkTime(EntityBehaviorMilkable self)
    {
        LastMilkedTotalHours(self) = self.entity.WatchedAttributes.GetFloat("lastMilkedTotalHours", 0);
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityBehaviorMilkable))]
    [HarmonyPatch("CanMilk")]
    public static bool FixCanMilk(EntityBehaviorMilkable __instance)
    {
        ReadMilkTime(__instance);
        return true;
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityBehaviorMilkable))]
    [HarmonyPatch("MilkingComplete")]
    public static bool FixMilkingComplete(EntityBehaviorMilkable __instance, ItemSlot slot, EntityAgent byEntity)
    {
        CustomMilkingComplete(__instance, slot, byEntity);
        return false;
    }

    public static void CustomMilkingComplete(EntityBehaviorMilkable self, ItemSlot slot, EntityAgent byEntity)
    {
        ItemStack liquidStack = Traverse.Create(self).Field("liquidStack").GetValue<ItemStack>();
        float yieldLitres = Traverse.Create(self).Field("yieldLitres").GetValue<float>();
        ILoadedSound milkSound = Traverse.Create(self).Field("milkSound").GetValue<ILoadedSound>();

        if (slot.Itemstack.Collectible is not BlockLiquidContainerBase lcblock)
        {
            return;
        }

        if (self.entity.World.Side == EnumAppSide.Server)
        {
            WriteMilkTime(self, self.entity.World.Calendar.TotalHours);

            ItemStack contentStack = liquidStack.Clone();
            contentStack.StackSize = 999999;

            if (slot.Itemstack.StackSize == 1)
            {
                lcblock.TryPutLiquid(slot.Itemstack, contentStack, yieldLitres);
            }
            else
            {
                ItemStack containerStack = slot.TakeOut(1);
                lcblock.TryPutLiquid(containerStack, contentStack, yieldLitres);

                if (!byEntity.TryGiveItemStack(containerStack))
                {
                    byEntity.World.SpawnItemEntity(containerStack, byEntity.Pos.XYZ.Add(0, 0.5, 0));
                }
            }

            slot.MarkDirty();
        }

        milkSound?.Stop();
        milkSound?.Dispose();
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(EntityBehaviorMilkable))]
    [HarmonyPatch("GetInfoText")]
    public static bool FixGetInfoText(EntityBehaviorMilkable __instance, StringBuilder infotext)
    {
        ReadMilkTime(__instance);
        return true;
    }
}
