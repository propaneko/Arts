using ArtOfCooking.BlockEntities;
using ArtOfCooking.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Blocks
{
    internal class AOCBlockDoughKnead: Block
    {
        WorldInteraction[] interactionsAddWater;
        WorldInteraction[] interactionsAddEgg;
        WorldInteraction[] interactionsKneading;
        WorldInteraction[] interactionsRollind;

        ItemStack varietyStack;
        public string VariantV => Variant["variety"];
        
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            
            interactionsAddWater = ObjectCacheUtil.GetOrCreate(api, "AOCaddwaterInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj is BlockLiquidContainerBase blc && blc.IsTopOpened && blc.AllowHeldLiquidTransfer)
                    stacks.Add(new ItemStack(obj));
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-doughknead-addwater",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
            
            interactionsAddEgg = ObjectCacheUtil.GetOrCreate(api, "AOCaddeggInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj.FirstCodePart() == "egg"
                        || obj is BlockLiquidContainerBase blc && blc.IsTopOpened && blc.AllowHeldLiquidTransfer)
                    stacks.Add(new ItemStack(obj));
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-doughknead-addegg",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-doughknead-knead",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });
            
            interactionsKneading = ObjectCacheUtil.GetOrCreate(api, "AOCkneadInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-doughknead-knead",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });
            
            interactionsRollind = ObjectCacheUtil.GetOrCreate(api, "AOCrollingInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj.FirstCodePart() == "rollingpin")
                    stacks.Add(new ItemStack(obj));
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-doughknead-rolling",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }
        public void OnCreate (ItemSlot slot, int quantity)
        {
            varietyStack = slot.TakeOut(quantity);
            slot.MarkDirty();
        }        
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemSlot leftslot = byPlayer.Entity.LeftHandItemSlot;
            ItemStack handStack = handslot?.Itemstack;
            if (VariantV == null) return true;
            
            if (handStack != null && handStack?.Collectible is BlockLiquidContainerTopOpened blockLiqCont 
                && !blockLiqCont.IsEmpty(handStack)) 
            { 
                bool waterportion = blockLiqCont?.GetContent(handStack)?.Collectible?.FirstCodePart() == "waterportion";

                bool bloodportion = blockLiqCont?.GetContent(handStack)?.Collectible?.FirstCodePart() == "bloodportion";

                bool eggportion = blockLiqCont?.GetContent(handStack)?.Collectible?.FirstCodePart() == "eggportion" 
                    && blockLiqCont?.GetContent(handStack)?.Collectible?.Variant["type"] == "whole" 
                    || blockLiqCont?.GetContent(handStack)?.Collectible?.FirstCodePart() == "eggyolkfullportion";
                
                if (!waterportion && !eggportion && !bloodportion) return true;

                float liquidPortion = eggportion? 0.25f : 1;
                Block nextStage = null;

                switch (VariantV)
                {
                    case "flour":
                        if (waterportion) nextStage = world.GetBlock(CodeWithVariant("variety", "wetflour"));
                        if (bloodportion && Variant["type"] != "acorn") nextStage = world.GetBlock(CodeWithVariant("variety", "bloodflour"));
                        break;
                    case "wetflour":
                        if (eggportion) nextStage = world.GetBlock(CodeWithVariant("variety", "eggwetflour"));
                        break;
                    default:
                        return true;
                }

                if (nextStage == null) return true;
                
                blockLiqCont.TryTakeLiquid(handStack, liquidPortion);
                world.BlockAccessor.SetBlock(nextStage.Id, blockSel.Position);
                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/squish1"), byPlayer.Entity, null, true, 16, 0.5f);
                handslot.MarkDirty();
                return true;
            }

            if (handStack?.Collectible?.FirstCodePart() == "flour" && handStack?.Collectible?.Variant["type"] == "acorn")
            {     
                if (VariantV != "wetflour" || Variant["type"] == "acorn") return true;
                world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("artofcooking:doughknead-flour-acorn")).Id, blockSel.Position);
                handslot.MarkDirty();
                return true;
            }

            if (handStack?.Collectible?.FirstCodePart() == "egg" && VariantV == "wetflour")
            {
                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/block/woodcreak_4"), byPlayer.Entity, null, true, 16, 3f);
                return true;
            }

            if (VariantV == "wetflour" || VariantV == "bloodflour" || VariantV == "eggwetflour" 
                || VariantV == "pastry" && handStack?.Collectible?.FirstCodePart() == "rollingpin" 
                || VariantV == "unleavened" && handStack?.Collectible?.FirstCodePart() == "rollingpin") 
            {
                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/squeezehoneycomb"), byPlayer.Entity, null, true, 16, 0.5f);
                return true;
            }
            if (VariantV == "rot") 
            {
                ItemStack rotstack = new ItemStack(world.GetItem(new AssetLocation("rot")));
                if (!byPlayer.InventoryManager.TryGiveItemstack(rotstack, true))
                {
                    world.SpawnItemEntity(rotstack, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                }
                world.BlockAccessor.SetBlock(0, blockSel.Position);
            }
            else
            {
                if (varietyStack != null && !byPlayer.InventoryManager.TryGiveItemstack(varietyStack, true))
                {
                    world.SpawnItemEntity(varietyStack, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                }
                world.BlockAccessor.SetBlock(0, blockSel.Position); 
            }
            world.BlockAccessor.MarkBlockDirty(blockSel.Position);
            return true;
        }
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemSlot leftslot = byPlayer.Entity.LeftHandItemSlot;
            
            if (VariantV == null) return false;

            if (VariantV == "wetflour" && handslot.Empty && leftslot.Empty 
                || VariantV == "bloodflour" && handslot.Empty && leftslot.Empty 
                || VariantV == "eggwetflour" && handslot.Empty && leftslot.Empty 
                || handslot?.Itemstack?.Collectible?.FirstCodePart() == "egg"  && VariantV == "wetflour") 
            {
                if (byPlayer.Entity.World is IClientWorldAccessor)
                {
                    byPlayer.Entity.StartAnimation("squeezehoneycomb");
                }
                return secondsUsed < 2f;
            }


            if (VariantV == "pastry" && handslot?.Itemstack?.Collectible?.FirstCodePart() == "rollingpin" 
                || VariantV == "unleavened" && handslot?.Itemstack?.Collectible?.FirstCodePart() == "rollingpin") 
            {
                return secondsUsed < 2f;
            }

            byPlayer.Entity.StopAnimation("squeezehoneycomb");
            return false;
        }
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            byPlayer.Entity.StopAnimation("squeezehoneycomb");
            ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemSlot leftslot = byPlayer.Entity.LeftHandItemSlot;
            if (secondsUsed < 1.9f) return;

            if (VariantV == null) return;
            
            if (handslot?.Itemstack?.Collectible?.FirstCodePart() == "egg")
            {
                if (VariantV == "wetflour") 
                {
                    Block nextStage = world.GetBlock(CodeWithVariant("variety", "eggwetflour"));            
                    ItemStack eggshellStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggshell-chicken")),2);
                    world.BlockAccessor.SetBlock(nextStage.Id, blockSel.Position);
                    byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/squish1"), byPlayer.Entity, null, true, 16, 0.5f);
                    handslot.TakeOut(1);
                    handslot.MarkDirty();
                    if (eggshellStack != null && byPlayer?.InventoryManager.TryGiveItemstack(eggshellStack) == false)
                    {
                        world.SpawnItemEntity(eggshellStack, byPlayer.Entity.Pos.XYZ);
                    }
                }
            }

            if (VariantV == "wetflour" || VariantV == "bloodflour" || VariantV == "eggwetflour")
            {
                if (handslot.Empty && leftslot.Empty)
                {
                    int countDrop = 8;
                    if (Variant["type"] == "acorn") countDrop = 16;
                    ItemStack drop = new ItemStack(world.GetItem(new AssetLocation("artofcooking:doughpiece-unleavened-" + Variant["type"])),countDrop);
                    if (VariantV == "eggwetflour") drop = new ItemStack(world.GetItem(new AssetLocation("artofcooking:doughpiece-pastry-" + Variant["type"])),countDrop);
                    if (VariantV == "bloodflour") drop = new ItemStack(world.GetItem(new AssetLocation("artofcooking:doughpiece-blooddough-" + Variant["type"])),countDrop);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(drop, true))
                    {
                        world.SpawnItemEntity(drop, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                    }
                    world.BlockAccessor.SetBlock(0, blockSel.Position);
                }
            }

            if (VariantV == "pastry" || VariantV == "unleavened")
            {
                if (handslot.Itemstack.Collectible.FirstCodePart() == "rollingpin")
                {
                    ItemStack drop = new ItemStack(world.GetItem(new AssetLocation("artofcooking:lavash-" + Variant["type"] + "-raw")),1);
                    if (VariantV == "pastry") drop = new ItemStack(world.GetItem(new AssetLocation("artofcooking:flatbread-" + Variant["type"] + "-raw")),1);
                    if (!byPlayer.InventoryManager.TryGiveItemstack(drop, true))
                    {
                        world.SpawnItemEntity(drop, blockSel.Position.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                    }
                    world.BlockAccessor.SetBlock(0, blockSel.Position);
                    handslot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, handslot, 1);
                }
            }
            
            world.BlockAccessor.MarkBlockDirty(blockSel.Position);
        }
        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            byPlayer.Entity.StopAnimation("squeezehoneycomb");
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }     
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            switch (VariantV)
            {
                case "flour":
                    return interactionsAddWater.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                case "wetflour":
                    return interactionsAddEgg.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                case "eggwetflour":
                    return interactionsKneading.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                case "pastry":
                    return interactionsRollind.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                case "unleavened":
                    return interactionsRollind.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                default:
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }
            
        }   
    }
}
