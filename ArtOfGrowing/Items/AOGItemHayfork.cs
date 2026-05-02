using ArtOfGrowing.Blocks;
using Cairo;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using OrderedDictionaryBP = Vintagestory.API.Datastructures.OrderedDictionary<Vintagestory.API.MathTools.BlockPos, float>;

namespace ArtOfGrowing.Items
{
    public class AOGItemHayfork: ItemShears
    {
        public override int MultiBreakQuantity { get { return 5; } }
        
        SkillItem[] modes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            
            var capi = api as ICoreClientAPI;
            if (capi != null)
            {
                modes = ObjectCacheUtil.GetOrCreate(api, "hayforkToolModes", () =>
                {
                    SkillItem[] modes = new SkillItem[2];
                    
                    modes[0] = new SkillItem() { Code = new AssetLocation("several hayfork"), Name = Lang.Get("Collect several") }.WithIcon(capi, Drawcreate9_svg);
                    modes[1] = new SkillItem() { Code = new AssetLocation("one hayfork"), Name = Lang.Get("Collect one") }.WithIcon(capi, Drawcreate1_svg);

                    return modes;
                });
            }
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return base.GetHeldInteractionHelp(inSlot).Append(new WorldInteraction()
            {
                ActionLangCode = "heldhelp-settoolmode",
                HotKeyCode = "toolmodeselect"
            });
        }

        public override bool CanMultiBreak(Block block)
        {
            return block is AOGBlockHayLayer || block is AOGBlockGroundStorage;
        }        
        
        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            float newResist = base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
            int leftDurability = itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack);
            
            int mode = GetToolMode(itemslot, player, blockSel);
            if (mode == 0) DamageNearbyBlocks(player, blockSel, remainingResistance - newResist, leftDurability, mode);

            return newResist;
        }

        private void DamageNearbyBlocks(IPlayer player, BlockSelection blockSel, float damage, int leftDurability, int mode)
        {
            Block block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);

            if (!CanMultiBreak(block)) return;

            Vec3d hitPos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
            OrderedDictionaryBP dict = GetNearblyMultibreakables(player.Entity.World, blockSel.Position, hitPos);
            var orderedPositions = dict.OrderBy(x => x.Value).Select(x => x.Key);

            int q = Math.Min(MultiBreakQuantity, leftDurability);
            foreach (var pos in orderedPositions)
            {
                if (q == 0) break;
                BlockFacing facing = BlockFacing.FromNormal(player.Entity.ServerPos.GetViewVector()).Opposite;

                if (!player.Entity.World.Claims.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak)) continue;
                
                player.Entity.World.BlockAccessor.DamageBlock(pos, facing, damage);
                q--;
            }
        }    

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);

            if (byEntity as EntityPlayer == null || itemslot.Itemstack == null) return true;

            IPlayer plr = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

            //base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            breakMultiBlock(blockSel.Position, plr);

            if (!CanMultiBreak(block)) return true;

            Vec3d hitPos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
            var orderedPositions = GetNearblyMultibreakables(world, blockSel.Position, hitPos).OrderBy(x => x.Value);

            int leftDurability = itemslot.Itemstack.Collectible.GetRemainingDurability(itemslot.Itemstack);
            int q = 0;
            
            int mode = GetToolMode(itemslot, plr, blockSel);

            if (mode == 0) foreach (var val in orderedPositions)
            {
                if (!plr.Entity.World.Claims.TryAccess(plr, val.Key, EnumBlockAccessFlags.BuildOrBreak)) continue;

                breakMultiBlock(val.Key, plr);

                DamageItem(world, byEntity, itemslot);
                
                q++;
                if (q >= MultiBreakQuantity || itemslot.Itemstack == null) break;
            }

            return true;
        }     

        OrderedDictionaryBP GetNearblyMultibreakables(IWorldAccessor world, BlockPos pos, Vec3d hitPos)
        {
            OrderedDictionaryBP positions = new OrderedDictionaryBP();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        BlockPos dpos = pos.AddCopy(dx, dy, dz);
                        if (CanMultiBreak(world.BlockAccessor.GetBlock(dpos)))
                        {
                            positions.Add(dpos, hitPos.SquareDistanceTo(dpos.X + 0.5, dpos.Y + 0.5, dpos.Z + 0.5));
                        }
                    }
                }
            }

            return positions;
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
            if (blockSel == null) return;

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                return;
            }

            byEntity.Attributes.SetBool("didBreakBlocks", false);
            byEntity.Attributes.SetBool("didPlayHayforkSound", false);
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            performActions(secondsPassed, byEntity, slot, blockSelection);
            if (api.Side == EnumAppSide.Server) return true;

            return secondsPassed < 2f;
        }

        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            performActions(secondsPassed, byEntity, slot, blockSelection);
        }

        private void performActions(float secondsPassed, EntityAgent byEntity, ItemSlot slot, BlockSelection blockSelection)
        {
            if (blockSelection == null) return;

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            var canmultibreak = CanMultiBreak(api.World.BlockAccessor.GetBlock(blockSelection.Position));

            if (canmultibreak && secondsPassed > 0.75f && byEntity.Attributes.GetBool("didPlayHayforkSound") == false)
            {
                api.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), byEntity, byPlayer, true, 16);
                byEntity.Attributes.SetBool("didPlayHayforkSound", true);
            }

            if (canmultibreak && secondsPassed > 1.05f && byEntity.Attributes.GetBool("didBreakBlocks") == false)
            {
                if (byEntity.World.Side == EnumAppSide.Server && byEntity.World.Claims.TryAccess(byPlayer, blockSelection.Position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    OnBlockBrokenWith(byEntity.World, byEntity, slot, blockSelection);
                }

                byEntity.Attributes.SetBool("didBreakBlocks", true);
            }
        }
        protected override void breakMultiBlock(BlockPos pos, IPlayer plr)
        {
            var block = api.World.BlockAccessor.GetBlock(pos);  
            var alldrops = block.GetDrops(api.World, pos, plr);
            foreach (var drop in alldrops)
            {
                if (!plr.InventoryManager.TryGiveItemstack(drop, true))
                {
                    api.World.SpawnItemEntity(drop, pos.ToVec3d().AddCopy(0.5, 0.1, 0.5));
                }
            }
            if (block.Variant["overlay"] == "eaten") api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("game:tallgrass-eaten-free")).Id, pos);
            else api.World.BlockAccessor.SetBlock(0, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
        }
        
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return modes;
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
        {
            return slot.Itemstack.Attributes.GetInt("toolMode", 0);
        }        
        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; modes != null && i < modes.Length; i++)
            {
                modes[i]?.Dispose();
            }
        }
        
        #region Icons

        public static void Drawcreate1_svg(Context cr, int x, int y, float width, float height, double[] rgba)
        {
            Pattern pattern = null;
            Matrix matrix = cr.Matrix;

            cr.Save();
            float w = 129;
            float h = 129;
            float scale = Math.Min(width / w, height / h);
            matrix.Translate(x + Math.Max(0, (width - w * scale) / 2), y + Math.Max(0, (height - h * scale) / 2));
            matrix.Scale(scale, scale);
            cr.Matrix = matrix;

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(51.828125, 51.828125);
            cr.LineTo(76.828125, 51.828125);
            cr.LineTo(76.828125, 76.828125);
            cr.LineTo(51.828125, 76.828125);
            cr.ClosePath();
            cr.MoveTo(51.828125, 51.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(51.828125, 51.828125);
            cr.LineTo(76.828125, 51.828125);
            cr.LineTo(76.828125, 76.828125);
            cr.LineTo(51.828125, 76.828125);
            cr.ClosePath();
            cr.MoveTo(51.828125, 51.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Restore();
        }        
        public void Drawcreate9_svg(Context cr, int x, int y, float width, float height, double[] rgba)
        {
            Pattern pattern = null;
            Matrix matrix = cr.Matrix;

            cr.Save();
            float w = 129;
            float h = 129;
            float scale = Math.Min(width / w, height / h);
            matrix.Translate(x + Math.Max(0, (width - w * scale) / 2), y + Math.Max(0, (height - h * scale) / 2));
            matrix.Scale(scale, scale);
            cr.Matrix = matrix;

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 14.828125);
            cr.LineTo(40.328125, 14.828125);
            cr.LineTo(40.328125, 39.828125);
            cr.LineTo(15.328125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 14.828125);
            cr.LineTo(40.328125, 14.828125);
            cr.LineTo(40.328125, 39.828125);
            cr.LineTo(15.328125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 14.828125);
            cr.LineTo(77.828125, 14.828125);
            cr.LineTo(77.828125, 39.828125);
            cr.LineTo(52.828125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 14.828125);
            cr.LineTo(77.828125, 14.828125);
            cr.LineTo(77.828125, 39.828125);
            cr.LineTo(52.828125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 52.828125);
            cr.LineTo(40.328125, 52.828125);
            cr.LineTo(40.328125, 77.828125);
            cr.LineTo(15.328125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 52.828125);
            cr.LineTo(40.328125, 52.828125);
            cr.LineTo(40.328125, 77.828125);
            cr.LineTo(15.328125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 52.828125);
            cr.LineTo(77.828125, 52.828125);
            cr.LineTo(77.828125, 77.828125);
            cr.LineTo(52.828125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 52.828125);
            cr.LineTo(77.828125, 52.828125);
            cr.LineTo(77.828125, 77.828125);
            cr.LineTo(52.828125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 14.828125);
            cr.LineTo(115.328125, 14.828125);
            cr.LineTo(115.328125, 39.828125);
            cr.LineTo(90.328125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 14.828125);
            cr.LineTo(115.328125, 14.828125);
            cr.LineTo(115.328125, 39.828125);
            cr.LineTo(90.328125, 39.828125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 14.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 52.828125);
            cr.LineTo(115.328125, 52.828125);
            cr.LineTo(115.328125, 77.828125);
            cr.LineTo(90.328125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 52.828125);
            cr.LineTo(115.328125, 52.828125);
            cr.LineTo(115.328125, 77.828125);
            cr.LineTo(90.328125, 77.828125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 52.828125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 91.328125);
            cr.LineTo(40.328125, 91.328125);
            cr.LineTo(40.328125, 116.328125);
            cr.LineTo(15.328125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(15.328125, 91.328125);
            cr.LineTo(40.328125, 91.328125);
            cr.LineTo(40.328125, 116.328125);
            cr.LineTo(15.328125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(15.328125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 91.328125);
            cr.LineTo(77.828125, 91.328125);
            cr.LineTo(77.828125, 116.328125);
            cr.LineTo(52.828125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(52.828125, 91.328125);
            cr.LineTo(77.828125, 91.328125);
            cr.LineTo(77.828125, 116.328125);
            cr.LineTo(52.828125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(52.828125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 91.328125);
            cr.LineTo(115.328125, 91.328125);
            cr.LineTo(115.328125, 116.328125);
            cr.LineTo(90.328125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.FillRule = FillRule.Winding;
            cr.FillPreserve();
            if (pattern != null) pattern.Dispose();

            cr.Operator = Operator.Over;
            cr.LineWidth = 8;
            cr.MiterLimit = 10;
            cr.LineCap = LineCap.Butt;
            cr.LineJoin = LineJoin.Miter;
            pattern = new SolidPattern(rgba[0], rgba[1], rgba[2], rgba[3]);
            cr.SetSource(pattern);

            cr.NewPath();
            cr.MoveTo(90.328125, 91.328125);
            cr.LineTo(115.328125, 91.328125);
            cr.LineTo(115.328125, 116.328125);
            cr.LineTo(90.328125, 116.328125);
            cr.ClosePath();
            cr.MoveTo(90.328125, 91.328125);
            cr.Tolerance = 0.1;
            cr.Antialias = Antialias.Default;
            cr.StrokePreserve();
            if (pattern != null) pattern.Dispose();

            cr.Restore();
        }

        #endregion

    }
}
