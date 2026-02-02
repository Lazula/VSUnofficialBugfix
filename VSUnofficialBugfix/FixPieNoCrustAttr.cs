namespace UnofficialBugfix.FixPieNoCrustAttr;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixPieNoCrustAttr
{
    /// BUG: Adding a top crust doesnt set the topCrustType
    /// attribute, which is used to rotate the type. This
    /// causes just-made full crusts to not stack with rotated
    /// full crusts because only the latter has the attribute.
    /// FIX: Manually add the attribute if we just added top crust.
#nullable enable
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(BlockEntityPie))]
    [HarmonyPatch("TryAddIngredientFrom")]
    public static void FixMissingCrustAttr(BlockEntityPie __instance, ref ICoreAPI ___Api, ItemSlot slot, IPlayer? byPlayer = null)
    {
        if (__instance.Inventory[0].Itemstack?.Block is not BlockPie pieBlock) return;

        ItemStack?[] cStacks = pieBlock.GetContents(___Api.World, __instance.Inventory[0].Itemstack);

        if (cStacks[5] != null && __instance.Inventory[0].Itemstack.Attributes.GetString("topCrustType") == null)
        {
            __instance.Inventory[0].Itemstack.Attributes.SetString("topCrustType", "full");
        }
    }
}
