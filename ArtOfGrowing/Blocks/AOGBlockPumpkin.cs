using ArtOfGrowing.Items;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfGrowing.Blocks
{
    internal class AOGBlockPumpkin: Block
    {  
        public string Size => Variant["size"];   
        WorldInteraction[] interactions = null;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            interactions = ObjectCacheUtil.GetOrCreate(api, "pumpkinBlockInteractions", () =>
            {
                List<ItemStack> knifeStacklist = new List<ItemStack>();

                foreach (Item item in api.World.Items)
                {
                    if (item.Code == null) continue;

                    if (item.Tool == EnumTool.Knife || item is ItemCleaver)
                    {
                        knifeStacklist.Add(new ItemStack(item));
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofgrowing:blockhelp-pumpkin-harvest",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = knifeStacklist.ToArray()
                    }
                };
            });
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Empty) return base.OnBlockInteractStart(world, byPlayer, blockSel);
			if (slot.Itemstack.Collectible is ItemKnife || slot.Itemstack.Collectible is ItemCleaver)
			{
                return true;
			}
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null) return false;

            if (world.Rand.NextDouble() < 0.05)
            {
                world.PlaySoundAt(new AssetLocation("sounds/effect/squish1"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
            }

            if (world.Side == EnumAppSide.Client && world.Rand.NextDouble() < 0.25)
            {
                world.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(blockSel.HitPosition), new ItemStack(api.World.GetItem(new AssetLocation("vegetable-pumpkin"))), 0.25f, 1, 0.5f, byPlayer, new Vec3f(0, 1, 0));
            }

            return world.Side == EnumAppSide.Client || secondsUsed < 2.5f;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!slot.Empty)
            { 
			if (slot.Itemstack.Collectible is ItemKnife || slot.Itemstack.Collectible is ItemCleaver)
			{
                if (secondsUsed > 2.5f - 0.05f && world.Side == EnumAppSide.Server)
                {
                    string size2 = "wild";
                    float koef = 8;
                    if (Size != null) switch (Size)
                    {
                        case "wild":
                            koef = 2;
                            size2 = "small";
                            break;
                        case "small":
                            koef = 4;
                            size2 = "medium";
                            break;
                        case "medium":
                            koef = 6;
                            size2 = "decent";
                            break;
                        case "decent":
                            koef = 8;
                            size2 = "large";
                            break;
                        case "large":
                            koef = 12;
                            size2 = "hefty";
                            break;
                        case "hefty":
                            koef = 18;
                            size2 = "gigantic";
                            break;
                        case "gigantic":
                            koef = 24;
                            size2 = "gigantic";
                            break;
                    }
                    ItemStack seeds = new ItemStack(api.World.GetItem(new AssetLocation("seeds-pumpkin")),GameMath.RoundRandom(api.World.Rand, 1.2f));
                    ItemStack seeds2 = new ItemStack(api.World.GetItem(new AssetLocation("seeds-pumpkin")),GameMath.RoundRandom(api.World.Rand, 0.3f));
                    if (Size != null)
                    {
                        seeds = new ItemStack(api.World.GetItem(new AssetLocation("artofgrowing:seeds-" + Size + "-pumpkin")),GameMath.RoundRandom(api.World.Rand, 1.2f));
                        seeds2 = new ItemStack(api.World.GetItem(new AssetLocation("artofgrowing:seeds-" + size2 + "-pumpkin")), GameMath.RoundRandom(api.World.Rand, 0.3f));
                    }
				    api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("vegetable-pumpkin")),GameMath.RoundRandom(api.World.Rand, koef - 0.3f)), blockSel.Position.ToVec3d() +
						new Vec3d(0, 0.1, 0));  
                    api.World.SpawnItemEntity(seeds, blockSel.Position.ToVec3d() +
						new Vec3d(0, 0.1, 0));  
                    api.World.SpawnItemEntity(seeds2, blockSel.Position.ToVec3d() +
                        new Vec3d(0, 0.1, 0));
                    slot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, slot);
                    world.PlaySoundAt(new AssetLocation("sounds/effect/squish2"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                }
            } 
            }

            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "heldhelp-place",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
