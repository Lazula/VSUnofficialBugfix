namespace UnofficialBugfix.FixEarlyHealCancelDoubleUsage;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixEarlyHealCancelDoubleUsage
{
    /// BUG: Cancel has a weird ~0.3 second delay
    /// when sending the message to the server.
    /// Because of this, the purely time-based stepping
    /// will cause the server to return false.
    /// Cancel calls Stop on both the client *and* server,
    /// but the server also steps over the stop condition
    /// in the delay, making Stop succeed twice on the
    /// same tick.
    /// FIX: Step has to never reach a stop condition on
    /// the server.
    ///
    /// This DOES NOT FIX the early usage. There's no
    /// way for us to force a same-tick client-to-server
    /// sync when Cancel happens. We can do server-to-
    /// -client, but not the other way.
    [HarmonyPostfix()]
    [HarmonyPatch(typeof(CollectibleBehaviorHealingItem))]
    [HarmonyPatch("OnHeldInteractStep")]
    public static void FixOnHeldInteractStep(CollectibleBehaviorHealingItem __instance, ref bool __result, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        __result |= byEntity.World.Side.IsServer();
    }
}
