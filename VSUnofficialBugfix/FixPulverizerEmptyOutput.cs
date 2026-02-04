namespace UnofficialBugfix.FixPulverizerEmptyOutput;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixPulverizerEmptyOutput
{
    //[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ReduceSaturation")]
    //private static extern bool ReduceSaturation(EntityBehaviorHunger instance, float satLossMultiplier);

    /// BUG: If the crushed output stack is empty, the pulverizer
    /// will return early and never spawn an item, even though
    /// caps too weak to crush the input should return it.
    /// FIX: Don't return early and check for spawning conditions
    /// in one place instead.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BEPulverizer))]
    [HarmonyPatch("Crush")]
    [HarmonyPatch(new Type[] { typeof(int), typeof(int), typeof(int) })]
    public static bool FixPulverizerCrush(BEPulverizer __instance, int slot, int capTier, double xOffset)
    {
        CustomPulverizerCrush(__instance, slot, capTier, xOffset);
        return false;
    }

    public static void CustomPulverizerCrush(BEPulverizer self, int slot, int capTier, double xOffset)
    {
        ICoreServerAPI Api = Traverse.Create(self).Field("Api").GetValue<ICoreServerAPI>();
        InventoryPulverizer inv = Traverse.Create(self).Field("inv").GetValue<InventoryPulverizer>();
        Matrixf mat = Traverse.Create(self).Field("mat").GetValue<Matrixf>();

        ItemStack inputStack = inv[slot].TakeOut(1);
        ItemStack outputStack = null;

        if (inputStack.Collectible.CrushingProps is CrushingProperties props)
        {
            outputStack = props?.CrushedStack?.ResolvedItemstack.Clone();
            if (outputStack != null)
            {
                outputStack.StackSize = GameMath.RoundRandom(Api.World.Rand, props.Quantity.nextFloat(outputStack.StackSize, Api.World.Rand));
            }
        }

        bool canCrush = inputStack.Collectible.CrushingProps.HardnessTier <= capTier;
        // Make sure to always return the input if crushing isn't possible
        bool hasOutput = !canCrush || (canCrush && outputStack?.StackSize > 0);
        if (hasOutput)
        {
            Vec3d position = mat.TransformVector(new Vec4d(xOffset * 0.999, 0.1, 0.8, 0)).XYZ.Add(self.Pos).Add(0.5, 0, 0.5);
            double lengthways = Api.World.Rand.NextDouble() * 0.07 - 0.035;
            double sideways = Api.World.Rand.NextDouble() * 0.03 - 0.005;
            Vec3d velocity = new Vec3d(self.Facing.Axis == EnumAxis.Z ? sideways : lengthways, Api.World.Rand.NextDouble() * 0.02 - 0.01, self.Facing.Axis == EnumAxis.Z ? lengthways : sideways);

            Api.World.SpawnItemEntity(canCrush ? outputStack : inputStack, position, velocity);
        }


        self.MarkDirty(true);
    }
}
