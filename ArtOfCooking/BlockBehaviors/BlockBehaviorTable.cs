using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ArtOfCooking.BlockBehaviors
{
    internal class AOCBlockBehaviorTable: BlockBehavior
    {
        public AOCBlockBehaviorTable(Block block) : base(block)
        {
        }      
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (slot.Empty) return false;
            
            handling = EnumHandling.PreventDefault;
            
            BlockLiquidContainerTopOpened container = slot.Itemstack.Collectible as BlockLiquidContainerTopOpened;
            if (container != null && container.IsEmpty(slot.Itemstack) && blockSel.Face == BlockFacing.UP)
            {
                if (container.Code.FirstCodePart() != "metalbowl" && container.Code.FirstCodePart() != "bowl") return false;

                BlockPos placePos = blockSel.Position.AddCopy(blockSel.Face);
                
                if (!byPlayer.Entity.World.Claims.TryAccess(byPlayer, placePos, EnumBlockAccessFlags.BuildOrBreak))
                {
                    slot.MarkDirty();
                    return false;
                }

                BlockPos belowPos = blockSel.Position.AddCopy(blockSel.Face).Down();
                Block belowBlock = world.BlockAccessor.GetBlock(belowPos);

                if (!belowBlock.CanAttachBlockAt(byPlayer.Entity.World.BlockAccessor, container, belowPos, BlockFacing.UP)) return false;


                if (!world.BlockAccessor.GetBlock(placePos).IsReplacableBy(container)) return false;

                world.BlockAccessor.SetBlock(container.BlockId, placePos);

               if (container.Sounds != null && container.Sounds.Place.Location != null) world.PlaySoundAt(container.Sounds.Place.Location, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                
                
                BlockLiquidContainerTopOpened placeContainer = world.BlockAccessor.GetBlock(placePos) as BlockLiquidContainerTopOpened;
                
                slot.TakeOut(1);
                slot.MarkDirty();

                return true;
            } 
                

            return false;
        }
    }
}
