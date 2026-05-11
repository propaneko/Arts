using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfGrowing.Items
{
    public class AOGItemInteract : Item
    {
        WorldInteraction[] interactions;
        public string Name => Code.FirstCodePart();

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            if (Code.FirstCodePart() == "flaxbundle") interactions = ObjectCacheUtil.GetOrCreate(api, "flaxClearInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (Item items in api.World.Items)
                {
                    if (items.Code == null) continue;

                    if (items.Code.FirstCodePart() == "creaser")
                    {
                        stacks.Add(new ItemStack(items));
                    }
                }
                
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofgrowing:heldhelp-interact",
                        MouseButton = EnumMouseButton.Right
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofgrowing:heldhelp-interact",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
            else interactions = ObjectCacheUtil.GetOrCreate(api, "grainHeadsInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofgrowing:heldhelp-interact",
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {     
            if (!byEntity.Controls.ShiftKey)
            {
                handling = EnumHandHandling.PreventDefault;
                if (api.World.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), byEntity, null, true, 16, 0.5f);
                }
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent,ref handling);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.Controls.ShiftKey) return false;
            if (byEntity.World is IClientWorldAccessor)
            {
                byEntity.StartAnimation("squeezehoneycomb");
            }
            return secondsUsed < 2f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation("squeezehoneycomb");

                if (secondsUsed < 1.9f) return;

                    IWorldAccessor world = byEntity.World;

                    IPlayer byPlayer = null;
                    if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                    if (Name == "flaxbundle") 
                    {
                        int quantity = 1;
                        int tquantity = 1;
                        if (byEntity.Controls.FloorSitting) tquantity = tquantity * 2;    
                        if (!byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.FirstCodePart() == "creaser") tquantity = Math.Min(tquantity * 4, byEntity.LeftHandItemSlot.Itemstack.Collectible.Durability);        
                        quantity = Math.Min(tquantity, slot.StackSize);
                        slot.TakeOut(quantity);
                        slot.MarkDirty();
                        ItemStack stack = new ItemStack(world.GetItem(new AssetLocation("artofgrowing:flaxbundle-soft")),quantity); 
                        if (byPlayer?.InventoryManager.TryGiveItemstack(stack) == false)
                        {
                            byEntity.World.SpawnItemEntity(stack, byEntity.Pos.XYZ);
                        } 
                        if (!byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.FirstCodePart() == "creaser") 
                        {
                            byEntity.LeftHandItemSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byEntity.LeftHandItemSlot, quantity);
                        }
                    }   
                    if (Name == "grainbundle") 
                    {
                        string size = Variant["size"];
                        string type = Variant["type"];

                        int tquantity = 1;
                        if (byEntity.Controls.FloorSitting) tquantity *= 2;
                        if (byEntity.LeftHandItemSlot.Empty) tquantity *= 2;
                        
                        int quantity = Math.Min(tquantity, slot.StackSize);
                        slot.TakeOut(quantity);
                        slot.MarkDirty();

                        var asset = world.GetItem(new AssetLocation("seeds-" + type));
                        if (type == "soybean" || type == "peanut") asset = world.GetItem(new AssetLocation("legume-" + type));
                        if (size != null) asset = world.GetItem(new AssetLocation("artofgrowing:seeds-" + size + "-" + type));            
                        if (asset == null) return;

                        ItemStack stack = new ItemStack(asset,GameMath.RoundRandom(api.World.Rand, 3.5f) * quantity);
                        if (byPlayer?.InventoryManager.TryGiveItemstack(stack) == false)
                        {
                            byEntity.World.SpawnItemEntity(stack, byEntity.Pos.XYZ);
                        } 
                    } 
                    return;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.StopAnimation("squeezehoneycomb");
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
