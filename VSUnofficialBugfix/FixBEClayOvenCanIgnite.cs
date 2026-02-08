namespace UnofficialBugfix.FixBEClayOvenCanIgnite;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class BEClayOvenCanIgnite
{
    /// BUG: The clay oven will show the ignite world interaction
    /// regardless of what's in the oven because the "fuel slot"
    /// is also the first cooking slot.
    /// FIX: Use the property that is only true when the fuel
    /// slot actually contains fuel.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockEntityOven))]
    [HarmonyPatch("CanIgnite")]
    public static bool FixCanIgnite(BlockEntityOven __instance, ref bool __result)
    {
        __result = __instance.HasFuel && !__instance.IsBurning;
        return false;
    }
}
