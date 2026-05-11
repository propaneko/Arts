using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ArtOfGrowing.Items
{
    public class AOGItemFlaxSoft : Item
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "flaxSoftInteractions", () =>
            {
                List<ItemStack> stacks = new List<ItemStack>();
                
                foreach (Item items in api.World.Items)
                {
                    if (items.Code == null) continue;

                    if (items.Code.FirstCodePart() == "ridge")
                    {
                        stacks.Add(new ItemStack(items));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofgrowing:heldhelp-flaxsoft",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacks.ToArray()
                    }
                };
            });
        }



        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {    
            if (!byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.FirstCodePart() == "ridge" && !byEntity.Controls.ShiftKey)
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
            if (byEntity.LeftHandItemSlot.Empty || byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Code.FirstCodePart() != "ridge" || byEntity.Controls.ShiftKey) return false;
            if (byEntity.World is IClientWorldAccessor)
            {
                byEntity.StartAnimation("squeezehoneycomb");
            }
            return secondsUsed < 2f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation("squeezehoneycomb");
            if (byEntity.LeftHandItemSlot == null || byEntity.LeftHandItemSlot.Empty) return;
            if (secondsUsed < 1.9f) return;
            IWorldAccessor world = byEntity.World;
            int quantity = 1;
            int tquantity = 1;
            if (byEntity.Controls.FloorSitting) tquantity = tquantity * 2;    
            if (!byEntity.LeftHandItemSlot.Empty && byEntity.LeftHandItemSlot?.Itemstack?.Collectible.Variant["material"] == "wooden") tquantity = Math.Min(tquantity * 4, byEntity.LeftHandItemSlot.Itemstack.Collectible.Durability);         
            quantity = Math.Min(tquantity, slot.StackSize);
            slot.TakeOut(quantity);
            slot.MarkDirty();

            IPlayer byPlayer = null;
            
            if (byEntity is EntityPlayer) byPlayer = world.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            ItemStack stack = new ItemStack(world.GetItem(new AssetLocation("flaxfibers")),quantity);
            if (byPlayer?.InventoryManager.TryGiveItemstack(stack) == false)
            {
                byEntity.World.SpawnItemEntity(stack, byEntity.Pos.XYZ);
            }
            if (!byEntity.LeftHandItemSlot.Empty) 
            {
                byEntity.LeftHandItemSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byEntity.LeftHandItemSlot, quantity);
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
