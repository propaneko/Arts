using ArtOfGrowing.BlockEntites;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfGrowing.Blocks
{
    internal class AOGBlockCrop: BlockCrop
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {   
            if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Scythe || byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife)
            {
                base.OnBlockBroken(world, pos, byPlayer, 0);
                
                if (Variant["stage"] != null && Variant["stage"] == CropProps.GrowthStages.ToString())
                {
                    ItemStack[] array = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
                    List<ItemStack> list = new List<ItemStack>();
                    ItemStack[] drops = array;
                    ItemStack hayStack = drops[0].Clone();
                    drops[0].StackSize = 0;
                    AOGBlockGroundStorage blockgs = world.GetBlock(new AssetLocation("artofgrowing:haystorage")) as AOGBlockGroundStorage;
                    blockgs.CreateStorageFromMowing(world, pos, hayStack);
                    
                    if (drops != null)
                    {
                        for (int j = 0; j < drops.Length; j++)
                        {
                            if (SplitDropStacks)
                            {
                                for (int k = 0; k < drops[j].StackSize; k++)
                                {
                                    ItemStack itemStack = drops[j].Clone();
                                    itemStack.StackSize = 1;
                                    world.SpawnItemEntity(itemStack, pos);
                                }
                            }
                            else
                            {
                                world.SpawnItemEntity(drops[j].Clone(), pos);
                            }
                        }
                    }
                }
                else
                    world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("artofgrowing:haylayer-free-veryshort-straw-free")).Id, pos);                               
            }
            else 
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }   
    
    internal class AOGBlockTallGrass : BlockPlant
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var grass = "grass";
            switch (FirstCodePart()) 
            { 
            case "talldrygrass":
                grass = "drygrass";
                break;
            }

            if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife && Variant["tallgrass"] != null && Variant["tallgrass"] != "eaten")
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                var blockCode = "artofgrowing:haylayer-eaten-veryshort-" + grass + "-free";
                var blockPos = pos.Copy();
                world.RegisterCallback((dt) => {
                    var haylayerBlock = world.GetBlock(new AssetLocation(blockCode));
                    if (haylayerBlock != null && haylayerBlock.Id != 0)
                    world.BlockAccessor.SetBlock(haylayerBlock.Id, blockPos);
                }, 50);
                return;
            }

            if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Scythe && Variant["tallgrass"] != null && Variant["tallgrass"] != "eaten")
            {
                bool trimMode = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetInt("toolMode", 0) == 0;
                var blockCode = trimMode 
                    ? "artofgrowing:haylayer-eaten-" + Variant["tallgrass"] + "-" + grass + "-free"
                    : "artofgrowing:haylayer-free-" + Variant["tallgrass"] + "-" + grass + "-free";
                var blockPos = pos.Copy();
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                world.RegisterCallback((dt) => {
                    var haylayerBlock = world.GetBlock(new AssetLocation(blockCode));
                    if (haylayerBlock != null && haylayerBlock.Id != 0)
                    world.BlockAccessor.SetBlock(haylayerBlock.Id, blockPos);
                }, 50);
                return;
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }

    internal class AOGBlockHayLayer: Block, IDrawYAdjustable
    {
        public static float WildCropDropMul = 0.25f;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (FirstCodePart() != "strawlayer") return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            BlockEntityFarmland befarmland = world.BlockAccessor.GetBlockEntity(pos.DownCopy()) as BlockEntityFarmland;
            if (befarmland == null)
            {
                dropQuantityMultiplier *= byPlayer?.Entity.Stats.GetBlended("wildCropDropRate")?? 1;
            }

            SplitDropStacks = false;

            ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            if (befarmland == null)
            {
                List<ItemStack> moddrops = new List<ItemStack>();
                foreach (var drop in drops)
                {
                    if (!(drop.Item is ItemPlantableSeed))
                    {
                        drop.StackSize = GameMath.RoundRandom(world.Rand, WildCropDropMul * drop.StackSize);
                    }

                    if (drop.StackSize > 0) moddrops.Add(drop);
                }

                drops = moddrops.ToArray();
            }


            if (befarmland != null)
            {
                drops = befarmland.GetDrops(drops);
            }

            return drops;
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            if (Variant["overlay"] == "eaten") world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("game:tallgrass-eaten-free")).Id, pos);
            world.BlockAccessor.MarkBlockDirty(pos);
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var alldrops = GetDrops(world, blockSel.Position, byPlayer);
            foreach (var drop in alldrops)
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(drop, true))
                {
                    world.SpawnItemEntity(drop, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                }
            }
            if (Variant["overlay"] == "eaten") world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("game:tallgrass-eaten-free")).Id, blockSel.Position);
            else world.BlockAccessor.SetBlock(0, blockSel.Position);
            world.BlockAccessor.MarkBlockDirty(blockSel.Position);
            return true;
        }
        public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
            return nblock is BlockFarmland ? -0.0625f : 0f;
        }
    }     
    internal class AOGItemScythe: ItemScythe
    {      
        protected override void breakMultiBlock(BlockPos pos, IPlayer plr)
        {
            api.World.BlockAccessor.BreakBlock(pos, plr);
            api.World.BlockAccessor.MarkBlockDirty(pos);
        }
    }   
}
