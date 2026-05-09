using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Xml;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Items
{
    public class AOCItemEgg : Item
    {
        public float ContainedEggLitres = 0.2f;

        public bool CanCrackInto(Block block, BlockSelection blockSel)
        {
            var pos = blockSel?.Position;

            if (block is ILiquidSink blcto)
            {
                return pos == null || !blcto.IsFull(pos);
            }

            if (pos != null)
            {
                var beg = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGroundStorage;
                if (beg != null)
                {
                    ItemSlot crackIntoSlot = beg.GetSlotAt(blockSel);

                    if (crackIntoSlot?.Itemstack?.Block is ILiquidSink bowl)
                    {
                        return !bowl.IsFull(crackIntoSlot.Itemstack);
                    }
                }
            }

            return false;
        }

        WorldInteraction[] interactions;
        WorldInteraction[] interactionsyolk;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server)
            {
                JsonObject jsonLitres = Attributes?["containedEggLitres"];
                if (jsonLitres?.Exists == true)
                {
                    ContainedEggLitres = jsonLitres.AsFloat();
                }
                return;
            }
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "eggCrackInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();

                foreach (Block block in api.World.Blocks)
                {
                    if (block.Code == null) continue;

                    if (CanCrackInto(block, null))
                    {
                        stacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-egginteract",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-eggwhiteinteract",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = new string[] {"ctrl", "shift" },
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
            
            interactionsyolk = ObjectCacheUtil.GetOrCreate(api, "eggyolkInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (Block block in api.World.Blocks)
                {
                    if (block.Code == null) continue;

                    if (CanCrackInto(block, null))
                    {
                        stacks.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:heldhelp-eggyolkinteract",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }



        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block != null && CanCrackInto(block, blockSel) && byEntity.Controls.ShiftKey)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "eggyolk") 
                {
                    IWorldAccessor world = byEntity.World;                    

                    if (!CanCrackInto(block, blockSel)) return;
                    
                    string source = Variant["source"];
                    ItemStack eggStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggportion-raw-yolk")), 99999);
                    ItemStack eggshellStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggshell-" + source)),1);
                    float portion = ContainedEggLitres / 4;

                    ILiquidSink blockCnt = block as ILiquidSink;
                    if (blockCnt != null)
                    {
                        if (blockCnt.TryPutLiquid(blockSel.Position, eggStack, portion) == 0) return;
                    }
                    else
                    {
                        var beg = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
                        if (beg != null)
                        {
                            ItemSlot crackIntoSlot = beg.GetSlotAt(blockSel);

                            if (crackIntoSlot != null && crackIntoSlot?.Itemstack?.Block != null && CanCrackInto(crackIntoSlot.Itemstack.Block, null))
                            {
                                blockCnt = crackIntoSlot.Itemstack.Block as ILiquidSink;
                                blockCnt.TryPutLiquid(crackIntoSlot.Itemstack, eggStack, portion);
                                beg.MarkDirty(true);
                            }
                        }
                    }

                    slot.TakeOut(1);
                    slot.MarkDirty();

                    IPlayer byPlayer = null;
                    if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                    if (byPlayer?.InventoryManager.TryGiveItemstack(eggshellStack) == false)
                    {
                        byEntity.World.SpawnItemEntity(eggshellStack, byEntity.Pos.XYZ);
                    }

                    if (byEntity.World.Side == EnumAppSide.Client)
                    {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/squish2"), byEntity, null, true, 16, 0.5f);
                    }

                }
                else if (byEntity.World.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/woodcreak_4"), byEntity, null, true, 16, 3f);
                }   
                handling = EnumHandHandling.PreventDefault;
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent,ref handling);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel?.Block != null && CanCrackInto(blockSel.Block, blockSel))
            {
                if (!byEntity.Controls.ShiftKey || slot.Itemstack.Collectible.FirstCodePart() == "eggyolk") 
                    return false;
                if (byEntity.World is IClientWorldAccessor)
                {
                    byEntity.StartAnimation("squeezehoneycomb");
                }

                return secondsUsed < 1f;
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
            BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation("squeezehoneycomb");

            if (blockSel != null)
            {
                Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                if (CanCrackInto(block, blockSel))
                {
                    if (secondsUsed < 0.9f) return;

                    IWorldAccessor world = byEntity.World;

                    if (!CanCrackInto(block, blockSel)) return;
                    
                    string source = Variant["source"];
                    ItemStack eggStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggportion-raw-whole")), 99999);            
                    ItemStack eggshellStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggshell-" + source)),2);
                    ItemStack yolkStack = null;
                    float portion = ContainedEggLitres;
                    if (byEntity.Controls.CtrlKey && slot.Itemstack.Collectible.FirstCodePart() == "egg")
                    {
                        eggStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggportion-raw-white")), 99999);
                        eggshellStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggshell-" + source)),1);
                        yolkStack = new ItemStack(world.GetItem(new AssetLocation("artofcooking:eggyolk-" + source)),1);
                        portion = ContainedEggLitres / 4 * 3;
                    }

                    ILiquidSink blockCnt = block as ILiquidSink;
                    if (blockCnt != null)
                    {
                        if (blockCnt.TryPutLiquid(blockSel.Position, eggStack, portion) == 0) return;
                    }
                    else
                    {
                        var beg = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
                        if (beg != null)
                        {
                            ItemSlot crackIntoSlot = beg.GetSlotAt(blockSel);

                            if (crackIntoSlot != null && crackIntoSlot?.Itemstack?.Block != null && CanCrackInto(crackIntoSlot.Itemstack.Block, null))
                            {
                                blockCnt = crackIntoSlot.Itemstack.Block as ILiquidSink;
                                blockCnt.TryPutLiquid(crackIntoSlot.Itemstack, eggStack, portion);
                                beg.MarkDirty(true);
                            }
                        }
                    }

                    slot.TakeOut(1);
                    slot.MarkDirty();

                    IPlayer byPlayer = null;
                    if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                    if (byPlayer?.InventoryManager.TryGiveItemstack(eggshellStack) == false)
                    {
                        byEntity.World.SpawnItemEntity(eggshellStack, byEntity.Pos.XYZ);
                    }
                    if (yolkStack != null && byPlayer?.InventoryManager.TryGiveItemstack(yolkStack) == false)
                    {
                        byEntity.World.SpawnItemEntity(yolkStack, byEntity.Pos.XYZ);
                    }
                    
                    if (world.Side == EnumAppSide.Client)
                    {
                        world.PlaySoundAt(new AssetLocation("sounds/effect/squish2"), byEntity, null, true, 16, 0.5f);
                    }

                    return;
                }
            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.StopAnimation("squeezehoneycomb");
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (inSlot.Itemstack.Collectible.FirstCodePart() == "eggyolk")
                return interactionsyolk.Append(base.GetHeldInteractionHelp(inSlot));
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
