namespace UnofficialBugfix.FixFirepitScrollCrash;

public class FixFirepitScrollCrash
{
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(ItemSlotInput))]
    [HarmonyPatch("CanBeStackedWithOutputSlotItem")]
    public static bool FixCanBeStackedWithOutputSlotItem(ItemSlotInput __instance, ref bool __result, ItemSlot sourceSlot, bool notifySlot = true)
    {
        __result = CustomCanBeStackedWithOutputSlotItem(__instance, sourceSlot, notifySlot);
        return false;
    }

    public static bool CustomCanBeStackedWithOutputSlotItem(ItemSlotInput self, ItemSlot sourceSlot, bool notifySlot = true)
    {
        InventoryBase inventory = Traverse.Create(self).Field("inventory").GetValue<InventoryBase>();
        ItemSlot outslot = inventory[self.outputSlotId];
        if (outslot.Empty) return true;

        CombustibleProperties combustibleProps = sourceSlot.Itemstack?.Collectible.CombustibleProps?.Clone();
        ItemStack compareStack = combustibleProps?.SmeltedStack?.ResolvedItemstack;
        if (compareStack == null) compareStack = sourceSlot.Itemstack;

        if (!outslot.Itemstack.Equals(inventory.Api.World, compareStack, GlobalConstants.IgnoredStackAttributes))
        {
            outslot.Inventory.PerformNotifySlot(self.outputSlotId);
            return false;
        }

        return true;
    }
}
