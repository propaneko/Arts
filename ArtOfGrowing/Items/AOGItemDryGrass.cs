using ArtOfGrowing.BlockEntites;
using ArtOfGrowing.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfGrowing.Items
{
    public class AOGItemDryGrass : ItemDryGrass
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.Controls.ShiftKey)
            {
                if (byEntity.Controls.CtrlKey)
                {
                    Interact(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                    return;
                }

                OnHeldInteractStartThatch(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public static void Interact(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            IWorldAccessor world = byEntity?.World;

            if (blockSel == null || world == null || !byEntity.Controls.ShiftKey) return;


            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            if (byPlayer == null) return;

            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                itemslot.MarkDirty();
                world.BlockAccessor.MarkBlockDirty(blockSel.Position.UpCopy());
                return;
            }

            AOGBlockGroundStorage blockgs = world.GetBlock(new AssetLocation("haystorage")) as AOGBlockGroundStorage;
            if (blockgs == null) return;

            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            BlockEntity beAbove = world.BlockAccessor.GetBlockEntity(blockSel.Position.UpCopy());
            if (be is AOGBlockEntityGroundStorage || beAbove is AOGBlockEntityGroundStorage)
            {
                if (((be as AOGBlockEntityGroundStorage) ?? (beAbove as AOGBlockEntityGroundStorage)).OnPlayerInteractStart(byPlayer, blockSel))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                }
                return;
            }

            // Must be aiming at the up face
            if (blockSel.Face != BlockFacing.UP) return;
            Block onBlock = world.BlockAccessor.GetBlock(blockSel.Position);

            // Must have a support below
            if (!onBlock.CanAttachBlockAt(world.BlockAccessor, blockgs, blockSel.Position, BlockFacing.UP))
            {
                return;
            }

            // Must have empty space above
            BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);
            if (world.BlockAccessor.GetBlock(pos).Replaceable < 6000) return;


            if (blockgs.CreateStorage(byEntity.World, blockSel, byPlayer))
            {
                handHandling = EnumHandHandling.PreventDefault;
            }
        }

        public virtual void OnHeldInteractStartThatch(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            IWorldAccessor world = byEntity.World;
            Block firepitBlock = world.GetBlock(new AssetLocation("artofgrowing:firepit-construct1"));
            if (firepitBlock == null)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }


            BlockPos onPos = blockSel.DidOffset ? blockSel.Position : blockSel.Position.AddCopy(blockSel.Face);

            IPlayer byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
            if (!byEntity.World.Claims.TryAccess(byPlayer, onPos, EnumBlockAccessFlags.BuildOrBreak))
            {
                return;
            }

            Block block = world.BlockAccessor.GetBlock(onPos.DownCopy());
            Block aimedBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            if (aimedBlock is BlockGroundStorage)
            {
                var bec = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(blockSel.Position);
                if (bec.Inventory[3].Empty && bec.Inventory[2].Empty && bec.Inventory[1].Empty && bec.Inventory[0].Itemstack.Collectible is ItemFirewood)
                {
                    if (bec.Inventory[0].StackSize == bec.Capacity)
                    {
                        string useless = "";
                        if (!firepitBlock.CanPlaceBlock(world, byPlayer, new BlockSelection() { Position = onPos, Face = BlockFacing.UP }, ref useless)) return;
                        world.BlockAccessor.SetBlock(firepitBlock.BlockId, onPos);
                        if (firepitBlock.Sounds != null) world.PlaySoundAt(firepitBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer);
                        itemslot.Itemstack.StackSize--;

                    }
                    handHandling = EnumHandHandling.PreventDefault;
                    return;
                }

                if (!(aimedBlock is BlockPitkiln))
                {
                    BlockPitkiln blockpk = world.GetBlock(new AssetLocation("pitkiln")) as BlockPitkiln;
                    if (blockpk.TryCreateKiln(world, byPlayer, blockSel.Position))
                    {
                        handHandling = EnumHandHandling.PreventDefault;
                    }
                }
            }
            else
            {

                string useless = "";

                if (!block.CanAttachBlockAt(byEntity.World.BlockAccessor, firepitBlock, onPos.DownCopy(), BlockFacing.UP)) return;
                if (!firepitBlock.CanPlaceBlock(world, byPlayer, new BlockSelection() { Position = onPos, Face = BlockFacing.UP }, ref useless)) return;

                world.BlockAccessor.SetBlock(firepitBlock.BlockId, onPos);

                if (firepitBlock.Sounds != null) world.PlaySoundAt(firepitBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer);

                itemslot.Itemstack.StackSize--;
                handHandling = EnumHandHandling.PreventDefaultAction;
            }
        }
    }
}
