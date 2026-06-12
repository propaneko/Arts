using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ArtOfGrowing
{
    [HarmonyPatch]
    public class PatchWildgrassCompat
    {
        private static Type wildgrassBlockType;

        static bool Prepare()
        {
            wildgrassBlockType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == "BlockWildgrass");

            return wildgrassBlockType != null;
        }

        static MethodBase TargetMethod()
        {
            return wildgrassBlockType?.GetMethod("OnBlockBroken",
                new[] { typeof(IWorldAccessor), typeof(BlockPos), typeof(IPlayer), typeof(float) });
        }

        static void Postfix(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            if (byPlayer == null) return;

            EnumTool? tool = byPlayer.InventoryManager.ActiveTool;
            if (tool != EnumTool.Scythe && tool != EnumTool.Knife) return;

            bool trimMode = tool == EnumTool.Scythe &&
                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack
                    .Attributes.GetInt("toolMode", 0) == 0;

            string tallgrass = __instance.Variant.ContainsKey("tallgrass")
                ? __instance.Variant["tallgrass"]
                : "tall";

            if (tallgrass == "eaten") return;

            string grass = "grass";
            string blockCode;

            if (tool == EnumTool.Knife)
            {
                blockCode = "artofgrowing:haylayer-eaten-veryshort-" + grass + "-free";
            }
            else
            {
                blockCode = trimMode
                    ? "artofgrowing:haylayer-eaten-" + tallgrass + "-" + grass + "-free"
                    : "artofgrowing:haylayer-free-" + tallgrass + "-" + grass + "-free";
            }

            var blockPos = pos.Copy();
            world.RegisterCallback((dt) =>
            {
                var haylayerBlock = world.GetBlock(new AssetLocation(blockCode));
                if (haylayerBlock != null && haylayerBlock.Id != 0)
                    world.BlockAccessor.SetBlock(haylayerBlock.Id, blockPos);
            }, 50);
        }
    }

    public class WildgrassCompatSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override double ExecuteOrder() => 1.1;

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            bool wildgrassLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .Any(t => t.Name == "BlockWildgrass");

            if (!wildgrassLoaded) return;

            foreach (var block in api.World.Blocks)
            {
                if (block?.Code?.Domain != "wildgrass") continue;
                if (block.Drops == null) continue;

                // Keep only non-drygrass drops (seeds)
                block.Drops = block.Drops
                    .Where(d => d?.Code == null ||
                        !(d.Code.Domain == "game" && d.Code.Path == "drygrass"))
                    .ToArray();
            }
        }
    }
}
