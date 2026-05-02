using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ArtOfGrowing
{
    [HarmonyPatch(typeof(BlockBehaviorCuttableTallGrass), "OnBlockBroken")]
    public class PatchCuttableTallGrass
    {
        static void Postfix(BlockBehavior __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            var grass = "grass";
            string tallgrass = __instance.block.Variant["tallgrass"];

            if (tallgrass == null || tallgrass == "eaten") return;

            if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Knife)
            {
                var blockCode = "artofgrowing:haylayer-eaten-veryshort-" + grass + "-free";
                var blockPos = pos.Copy();
                world.RegisterCallback((dt) => {
                    var haylayerBlock = world.GetBlock(new AssetLocation(blockCode));
                    if (haylayerBlock != null && haylayerBlock.Id != 0)
                    world.BlockAccessor.SetBlock(haylayerBlock.Id, blockPos);
                }, 50);
            }
            else if (byPlayer?.InventoryManager.ActiveTool == EnumTool.Scythe)
            {
                bool trimMode = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.GetInt("toolMode", 0) == 0;
                var blockCode = trimMode
                    ? "artofgrowing:haylayer-eaten-" + tallgrass + "-" + grass + "-free"
                    : "artofgrowing:haylayer-free-" + tallgrass + "-" + grass + "-free";
                var blockPos = pos.Copy();
                world.RegisterCallback((dt) => {
                    var haylayerBlock = world.GetBlock(new AssetLocation(blockCode));
                    if (haylayerBlock != null && haylayerBlock.Id != 0)
                        world.BlockAccessor.SetBlock(haylayerBlock.Id, blockPos);
                }, 50);
            }
        }
    }
}