using CoreOfArts.BlockEntityRenderer;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CoreOfArts.Blocks
{
    public class COABlockCookingContainer : BlockCookingContainer, IInFirepitRendererSupplier
    {  
        public new IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
        {
            return new COAPotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
        }     
    }
}
