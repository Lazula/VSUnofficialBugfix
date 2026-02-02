namespace UnofficialBugfix.FixLiquidCombineFreshness;

/*

NOTE:

TryPutLiquid is part of the ILiquidSink
interface, ergo we cannot patch it directly.
The only implementor of this interface is
BlockLiquidContainerBase. BlockLiquidContainerBase is the
only class that overrides these methods, but
all it does is call its base.

So, patching BlockLiquidContainerBase covers
all cases for the vanilla game.
*/

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixLiquidCombineFreshness
{
    /// BUG: Combining liquid stacks
    /// does not average the freshness, instead
    /// retaining the freshness of whatever was
    /// already present, whether more or less fresh,
    /// which is detrimental to typical play yet
    /// easily exploitable.
    /// FIX: Average the perish props, if any. <summary>



    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("TryPutLiquid")]
    [HarmonyPatch(new Type[] { typeof(ItemStack), typeof(ItemStack), typeof(float) })]
    public static bool FixTryPutLiquid1(BlockLiquidContainerBase __instance, ref int __result, ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
    {
        __result = CustomTryPutLiquid1(__instance, containerStack, liquidStack, desiredLitres);
        return false;
    }

    private static int CustomTryPutLiquid1(BlockLiquidContainerBase self, ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
    {
        Console.WriteLine("Entered BlockLiquidContainerBase.CustomTryPutLiquid1(ItemStack, ItemStack, float).");
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();
        if (liquidStack == null) return 0;

        var props = BlockLiquidContainerBase.GetContainableProps(liquidStack);
        if (props == null) return 0;

        float epsilon = 0.00001f;
        int desiredItems = (int)(props.ItemsPerLitre * desiredLitres + epsilon);
        int availItems = liquidStack.StackSize;

        ItemStack stack = self.GetContent(containerStack);
        ILiquidSink sink = containerStack.Collectible as ILiquidSink;

        if (stack == null)
        {
            if (!props.Containable) return 0;

            int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre + epsilon);

            ItemStack placedstack = liquidStack.Clone();
            placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
            self.SetContent(containerStack, placedstack);

            return Math.Min(desiredItems, placeableItems);
        }
        else
        {
            if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

            float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
            int placeableItems = (int)(maxItems - (float)stack.StackSize);

            int moved = GameMath.Min(availItems, placeableItems, desiredItems);

            // Average freshness before adding
            if (stack.Collectible.GetTransitionableProperties(api.World, stack, null) is TransitionableProperties[] tprops)
            {
                var perishProps = tprops.FirstOrDefault(p => p.Type == EnumTransitionType.Perish);
                if (perishProps != null)
                {
                    float our_freshness = stack.Collectible.UpdateAndGetTransitionState(api.World, new DummySlot(stack), EnumTransitionType.Perish).TransitionedHours;
                    float their_freshness = liquidStack.Collectible.UpdateAndGetTransitionState(api.World, new DummySlot(liquidStack), EnumTransitionType.Perish).TransitionedHours;
                    float avg_freshness = ((our_freshness * stack.StackSize) + (their_freshness * moved)) / (stack.StackSize + moved);

                    stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, avg_freshness);
                }
            }

            stack.StackSize += moved;
            return moved;
        }
    }

    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("TryPutLiquid")]
    [HarmonyPatch(new Type[] { typeof(BlockPos), typeof(ItemStack), typeof(float) })]
    public static bool FixTryPutLiquid2(BlockLiquidContainerBase __instance, ref int __result, BlockPos pos, ItemStack liquidStack, float desiredLitres)
    {
        __result = CustomTryPutLiquid2(__instance, pos, liquidStack, desiredLitres);
        return false;
    }

    private static int CustomTryPutLiquid2(BlockLiquidContainerBase self, BlockPos pos, ItemStack liquidStack, float desiredLitres)
    {
        Console.WriteLine("Entered BlockLiquidContainerBase.CustomTryPutLiquid2(BlockPos, ItemStack, float).");
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();

        if (liquidStack == null) return 0;

        WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidStack);

        float itemsPerLitre = props?.ItemsPerLitre ?? 1;
        int desiredItems = (int)(itemsPerLitre * desiredLitres);
        float availItems = liquidStack.StackSize;
        float maxItems = self.CapacityLitres * itemsPerLitre;

        ItemStack stack = self.GetContent(pos);
        if (stack == null)
        {
            if (props == null || !props.Containable) return 0;

            int placeableItems = (int)GameMath.Min(desiredItems, maxItems, availItems);
            int movedItems = Math.Min(desiredItems, placeableItems);

            ItemStack placedstack = liquidStack.Clone();
            placedstack.StackSize = movedItems;
            self.SetContent(pos, placedstack);

            return movedItems;
        }
        else
        {
            if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

            int placeableItems = (int)Math.Min(availItems, maxItems - (float)stack.StackSize);
            int movedItems = Math.Min(placeableItems, desiredItems);

            // Average freshness before adding
            if (stack.Collectible.GetTransitionableProperties(api.World, stack, null) is TransitionableProperties[] tprops)
            {
                var perishProps = tprops.FirstOrDefault(p => p.Type == EnumTransitionType.Perish);
                if (perishProps != null)
                {
                    float our_freshness = stack.Collectible.UpdateAndGetTransitionState(api.World, new DummySlot(stack), EnumTransitionType.Perish).TransitionedHours;
                    float their_freshness = liquidStack.Collectible.UpdateAndGetTransitionState(api.World, new DummySlot(liquidStack), EnumTransitionType.Perish).TransitionedHours;
                    float avg_freshness = ((our_freshness * stack.StackSize) + (their_freshness * movedItems)) / (stack.StackSize + movedItems);

                    stack.Collectible.SetTransitionState(stack, EnumTransitionType.Perish, avg_freshness);
                }
            }

            stack.StackSize += movedItems;
            api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(true);
            (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer).Inventory[self.GetContainerSlotId(pos)].MarkDirty();

            return movedItems;
        }
    }
}
