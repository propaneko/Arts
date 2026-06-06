using CoreOfArts.Blocks;
using CoreOfArts.CollectibleBehaviors;
using CoreOfArts.Systems;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace CoreOfArts
{
    public class CoreOfArtsModSystem : ModSystem
    {
        private readonly Harmony _harmony = new("coapatch");     
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("COABlockCookingContainer", typeof(COABlockCookingContainer));
            api.RegisterCollectibleBehaviorClass("COAInLiquidMixing", typeof(COAInLiquidMixing));

            ClassRegistry registry = (api as ServerCoreAPI)?.ClassRegistryNative ?? (api as ClientCoreAPI)?.ClassRegistryNative;
            if (registry != null)
            {
                registry.BlockClassToTypeMapping["BlockLiquidContainerTopOpened"] = typeof(COABlockLiquidContainer);
                registry.BlockClassToTypeMapping["BlockBucket"] = typeof(COABlockBucket);
            }

            api.World.Logger.StoryEvent(Lang.Get("It changes..."));       
        }    
        public override void StartClientSide(ICoreClientAPI  api)
        {
            base.StartClientSide(api);
            
            PatchGame();
        }
        public override void Dispose()
        {
            var harmony = new Harmony("coapatch");
            harmony.UnpatchAll("coapatch");
        }
        private void PatchGame()
        {
            Mod.Logger.Event("Applying Harmony patches");
            var harmony = new Harmony("coapatch");
            harmony.PatchAll();
        }
    
        [HarmonyPatch]   
        class Patches
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BlockBarrel), "getContentMeshFromAttributes")]        
            static void Patch_getContentMeshFromAttributes(ItemStack contentStack, ref ItemStack liquidContentStack, BlockPos forBlockPos)
            {
                liquidContentStack = contentStack;
            }
            
            [HarmonyPostfix]            
            [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.GetHandbookInfo))]
            static void Patch_GetHandbookInfo(ref RichTextComponentBase[] __result, ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
            {
                List<RichTextComponentBase> list = __result.ToList();

                list.COAcreatedByMixingInfo(inSlot, capi, allStacks, openDetailPageFor);
                list.COAaddMixingIngredientForInfo(inSlot, capi, allStacks, openDetailPageFor);
                list.COAaddCreatedByInfo(inSlot, capi, allStacks, openDetailPageFor);
                list.COACookingRecipes(inSlot, capi, allStacks, openDetailPageFor);
                __result = list.ToArray();
            }
        }   
    }
}