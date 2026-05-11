using ArtOfCooking.BlockEntities;
using ArtOfCooking.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Blocks
{
    public class BlockDoughForm : Block
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "doughformBlockInteractions", () =>
            {
                List<ItemStack> doughStackList = new List<ItemStack>();

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj is AOCItemDough)
                    {
                        doughStackList.Add(new ItemStack(obj));
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-doughform-adddough",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = doughStackList.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-doughform-removedough",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Left,
                        Itemstacks = doughStackList.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-selecttoolmode",
                        HotKeyCode = "toolmodeselect",
                        MouseButton = EnumMouseButton.None,
                        Itemstacks = doughStackList.ToArray(),
                        GetMatchingStacks = getMatchingStacks
                    }
                };
            });
        }

        private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection bs, EntitySelection es)
        {
            BlockEntityDoughForm bec = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityDoughForm;
            List<ItemStack> stacks = new List<ItemStack>();

            foreach (var val in wi.Itemstacks)
            {
                if (bec?.BaseMaterial != null && bec.BaseMaterial.Collectible.LastCodePart() == val.Collectible.LastCodePart())
                {
                    stacks.Add(val);
                }
            }
            return stacks.ToArray();
        }

        Cuboidf box = new Cuboidf(0, 0, 0, 1, 1 / 16f, 1);

        public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
        {
            return box;
        }


        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntityDoughForm bea = blockAccessor.GetBlockEntity(pos) as BlockEntityDoughForm;
            if (bea != null)
            {
                Cuboidf[] selectionBoxes = bea.GetSelectionBoxes(blockAccessor, pos);

                return selectionBoxes;
            }

            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return GetSelectionBoxes(blockAccessor, pos);
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[0];
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[0];
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            pos.Y--;
            if (!world.BlockAccessor.GetMostSolidBlock(pos).CanAttachBlockAt(world.BlockAccessor, this, pos, BlockFacing.UP))
            {
                pos.Y++;
                world.BlockAccessor.BreakBlock(pos, null);
            }
            else pos.Y++;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }


}
