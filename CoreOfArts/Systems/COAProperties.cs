using System;
using Vintagestory.API.Common;
using Vintagestory.API;

namespace CoreOfArts.Systems
{
    
    [DocumentAsJson]
    public class COAInLiquidMixingProperties
    {
        [DocumentAsJson] public string InitialCode;
        [DocumentAsJson] public int SourceSize = 1;
        [DocumentAsJson] public float InputLitres = 1;
        [DocumentAsJson] public float? ConsumeInputLitres;
        [DocumentAsJson] public float? OutputLitres;
        [DocumentAsJson] public JsonItemStack InputStack;
        [DocumentAsJson] public JsonItemStack OutputLiquid;
        [DocumentAsJson] public JsonItemStack[] OutputStacks;
        [DocumentAsJson] public bool NeedExactLitres = false;
        [DocumentAsJson] public bool CanBulk = false;
        [DocumentAsJson] public float MixingTime = 0;
        [DocumentAsJson] public string Animation;
        [DocumentAsJson] public string Sound;
    }
}
