namespace UnofficialBugfix.FixLiquidDrinkOverridesTake;

[HarmonyPatchCategory("unofficialbugfix")]
internal static class FixLiquidDrinkOverridesTake
{
    /// BUG: Trying to take one portion of a drinkable
    /// liquid is overwritten with drinking because
    /// Collectible.OnHeldInteractStart branches off
    /// to tryEatBegin.
    /// FIX: Avoid calling up to base until confirming
    /// that we have no liquid movement action to take.
    [HarmonyPrefix()]
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("OnHeldInteractStart")]
    public static bool FixLiquidContainerOnHeldInteractStart(BlockLiquidContainerBase __instance, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    {
        CustomLiquidContainerOnHeldInteractStart(__instance, itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        return false;
    }


    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CollectibleObject))]
    [HarmonyPatch("OnHeldInteractStart")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectibleObjectOnHeldInteractStart(CollectibleObject instance, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling) { }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "tryEatBegin")]
    private static extern void tryEatBegin(BlockLiquidContainerBase instance, ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SpillContents")]
    private static extern bool SpillContents(BlockLiquidContainerBase instance, ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel);

    public static void CustomLiquidContainerOnHeldInteractStart(BlockLiquidContainerBase self, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    {
        ICoreAPI api = Traverse.Create(self).Field("api").GetValue<ICoreAPI>();

        if (blockSel != null && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage begs)
        {
            ItemSlot gslot = begs.GetSlotAt(blockSel);
            if (gslot == null || !gslot.Empty && gslot.Itemstack.Collectible is ILiquidInterface)
            {
                return;
            }
        }

        if (blockSel == null || byEntity.Controls.ShiftKey)
        {
            // We need to make sure we aren't doing anything
            // with liquid handling before calling up to
            // the base method because it will call out to
            // tryEatBegin and hijack our control flow.
            bool lookingAtLiquidContainer = blockSel != null && api.World.BlockAccessor.GetBlock(blockSel.Position) is BlockLiquidContainerBase;
            bool shouldDrink = !lookingAtLiquidContainer && self.CanDrinkFrom && self.GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null;

            if (handHandling != EnumHandHandling.PreventDefaultAction && shouldDrink)
            {
                tryEatBegin(self, itemslot, byEntity, ref handHandling, "drink", 4);
                return;
            }

            if (!byEntity.Controls.ShiftKey || (byEntity.Controls.ShiftKey && !lookingAtLiquidContainer))
            {
                CollectibleObjectOnHeldInteractStart(self, itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            }

            return;
        }

        if (self.AllowHeldLiquidTransfer)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            ItemStack contentStack = self.GetContent(itemslot.Itemstack);
            WaterTightContainableProps props = contentStack == null ? null : self.GetContentProps(contentStack);

            Block targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                return;
            }

            if (!self.TryFillFromBlock(itemslot, byEntity, blockSel.Position))
            {
                BlockLiquidContainerTopOpened targetCntBlock = targetedBlock as BlockLiquidContainerTopOpened;
                if (targetCntBlock != null)
                {
                    if (targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
                    {
                        self.TryTakeContent(itemslot.Itemstack, 1);
                        byEntity.World.PlaySoundAt(props?.FillSpillSound ?? "sounds/block/water", blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                    }

                }
                else
                {
                    if (byEntity.Controls.CtrlKey)
                    {
                        SpillContents(self, itemslot, byEntity, blockSel);
                    }
                }
            }
        }

        if (self.CanDrinkFrom && self.GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
        {
            tryEatBegin(self, itemslot, byEntity, ref handHandling, "drink", 4);
            return;
        }

        if (self.AllowHeldLiquidTransfer || self.CanDrinkFrom)
        {
            // Prevent placing on normal use
            handHandling = EnumHandHandling.PreventDefaultAction;
        }
    }
}
