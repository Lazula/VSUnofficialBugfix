namespace UnofficialBugfix;

public class BlockBehaviorBOCCollect : BlockBehaviorRightClickPickup
{
    public BlockBehaviorBOCCollect(Block block) : base(block)
    {
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        if (byPlayer?.Entity is not EntityPlayer player
            || player.Controls.ShiftKey) { return false; }

        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            return false;
        }

        if (block.Sounds?.Place is AssetLocation placeSound)
        {
            world.PlaySoundAt(placeSound, blockSel.Position, -0.4, byPlayer);
        }
        (world as IClientWorldAccessor)?.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

        // Get the appropriate drops for a BOC with one candle
        // should give this general mod support if there are
        // other BOCs
        Block oneCandleBlock = world.GetBlock(block.CodeWithPart("1", 1));
        ItemStack[] givenCandles = oneCandleBlock.GetDrops(world, blockSel.Position, byPlayer);
        foreach (ItemStack candleStack in givenCandles)
        {
            byPlayer?.InventoryManager.TryGiveItemstack(candleStack);
            if (candleStack.StackSize > 0)
            {
                world.SpawnItemEntity(candleStack, blockSel.Position);
            }
        }

        int.TryParse(block.LastCodePart(), out int stage);
        Block nextblock = world.GetBlock(block.CodeWithPart("" + (stage - 1), 1));
        world.BlockAccessor.SetBlock(stage > 1 ? nextblock.BlockId : 0, blockSel.Position);

        handling = EnumHandling.PreventDefault;
        return true;
    }
}
