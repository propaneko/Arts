using ArtOfCooking.BlockEntities;
using ArtOfCooking.Systems;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Blocks
{
    public class AOCBlockSpoon : BlockMeal
    {
        AOCMeshCache meshCache;
        protected override bool PlacedBlockEating => false;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            meshCache = api.ModLoader.GetModSystem<AOCMeshCache>();
        }
        public override float[] GetNutritionHealthMul(BlockPos pos, ItemSlot slot, EntityAgent forEntity)
        {
            return new float[] { 1.2f, 1.2f };
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!PlacedBlockEating) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            ItemStack stack = OnPickBlock(world, blockSel.Position);

            if (!byPlayer.Entity.Controls.ShiftKey)
            {
                if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                {
                    world.BlockAccessor.SetBlock(0, blockSel.Position);
                    world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
                    return true;
                }
                return false;
            }

            AOCBlockEntitySpoon bemeal = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AOCBlockEntitySpoon;
            DummySlot dummySlot = new DummySlot(stack, bemeal.inventory);
            dummySlot.MarkedDirty += () => true;

            return tryPlacedBeginEatMeal(dummySlot, byPlayer);
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            MultiTextureMeshRef meshref = meshCache.GetOrCreateMealInContainerMeshRef(this, GetCookingRecipe(capi.World, itemstack), GetNonEmptyContents(capi.World, itemstack));
            if (meshref != null) renderinfo.ModelRef = meshref;
        }
        public override MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
        {
            var capi = api as ICoreClientAPI;
            ItemStack itemstack = slot.Itemstack;
            return meshCache.GenMealInContainerMesh(this, GetCookingRecipe(capi.World, itemstack), GetNonEmptyContents(capi.World, itemstack));
        }
       public override string GetMeshCacheKey(ItemSlot slot)
        {
        return "" + meshCache.GetMealHashCode(slot.Itemstack);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            float temp = GetTemperature(world, inSlot.Itemstack);
            if (temp > 20)
            {
                dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
            }

            CookingRecipe recipe = GetCookingRecipe(world, inSlot.Itemstack);

            ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
            ItemSlot slot = BlockCrock.GetDummySlotForFirstPerishableStack(world, stacks, null, inSlot.Inventory);

            slot.Itemstack?.Collectible.AppendPerishableInfoText(slot, dsc, world);

            float servings = GetQuantityServings(world, inSlot.Itemstack);

            if (recipe != null)
            {
                if (Math.Round(servings, 1) < 0.05)
                {
                    dsc.AppendLine(Lang.Get("{1}% serving of {0}", recipe.GetOutputName(world, stacks).UcFirst(), Math.Round(servings * 100, 0)));
                } else
                {
                    dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks).UcFirst()));
                }
                
            } else
            {
                if (inSlot.Itemstack.Attributes.HasAttribute("quantityServings"))
                {
                    dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(servings, 1)));
                }
                else if (displayContentsInfo)
                {
                    dsc.AppendLine(Lang.Get("Contents:"));
                    if (stacks != null && stacks.Length > 0)
                    {
                        dsc.AppendLine(stacks[0].StackSize + "x " + stacks[0].GetName());
                    }
                }

            }

            if (!AOCMeshCache.ContentsRotten(stacks))
            {
                string facts = GetContentNutritionFacts(world, inSlot, null, recipe == null);

                if (facts != null)
                {
                    dsc.Append(facts);
                }
            }
        }
        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);

            IWorldAccessor world = entityItem.World;
            if (world.Side != EnumAppSide.Server) return;

            if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
            {
                ItemStack[] stacks = GetContents(world, entityItem.Itemstack);

                if (AOCMeshCache.ContentsRotten(stacks))
                {
                    for (int i = 0; i < stacks.Length; i++)
                    {
                        if (stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
                        {
                            world.SpawnItemEntity(stacks[i], entityItem.Pos.XYZ);
                        }
                    }
                } else
                {
                    ItemStack rndStack = stacks[world.Rand.Next(stacks.Length)];
                    world.SpawnCubeParticles(entityItem.Pos.XYZ, rndStack, 0.3f, 25, 1, null);
                }

                var eatenBlock = Attributes["eatenBlock"].AsString();
                if (eatenBlock == null) return;
                Block block = world.GetBlock(new AssetLocation(eatenBlock));

                entityItem.Itemstack = new ItemStack(block);
                entityItem.WatchedAttributes.MarkPathDirty("itemstack");
            }
        }
        
        public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {
            TransitionState[] states = base.UpdateAndGetTransitionStates(world, inslot);

            ItemStack[] stacks = GetNonEmptyContents(world, inslot.Itemstack);
            if (AOCMeshCache.ContentsRotten(stacks))
            {
                inslot.Itemstack.Attributes?.RemoveAttribute("recipeCode");
                inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
            }
            if (stacks == null || stacks.Length == 0)
            {
                inslot.Itemstack.Attributes?.RemoveAttribute("recipeCode");
                inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
            }

            string eaten = Attributes["eatenBlock"].AsString();
            if ((stacks == null || stacks.Length == 0) && eaten != null)
            {
                Block block = world.GetBlock(new AssetLocation(eaten));

                if (block != null)
                {
                    inslot.Itemstack = new ItemStack(block);
                    inslot.MarkDirty();
                }
            }

            return states;
        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            ItemStack[] contentStacks = GetContents(api.World, itemStack);
            if (AOCMeshCache.ContentsRotten(contentStacks))
            {
                return Lang.Get("Spoon of rotten food");
            }

            return base.GetHeldItemName(itemStack);
        }
    }
}
