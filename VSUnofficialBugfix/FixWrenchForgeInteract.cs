namespace UnofficialBugfix.FixEarlyHealCancelDoubleUsage;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixWrenchForgeInteract
{
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(ItemWrench))]
    [HarmonyPatch("OnHeldInteractStart")]
    public static void FixOnHeldInteractStart(ItemWrench __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (blockSel != null && byEntity.World.BlockAccessor.GetBlockEntity<BlockEntityForge>(blockSel.Position) != null)
        {
            handling = EnumHandHandling.NotHandled;
        }
    }
}
