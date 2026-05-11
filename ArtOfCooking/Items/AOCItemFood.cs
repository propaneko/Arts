using ArtOfCooking.BlockEntities;
using ArtOfCooking.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Items
{
    public class AOCItemFood : Item
    {
        public virtual FoodNutritionProperties[] GetExtraNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            FoodNutritionProperties[] extraNutritions = itemstack.ItemAttributes?["extraNutritionProps"]?.AsObject<FoodNutritionProperties[]>(null, itemstack.Collectible.Code.Domain);
            return extraNutritions;
        }        
        protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            FoodNutritionProperties[] nutriProperties = GetExtraNutritionProperties(byEntity.World, slot.Itemstack, byEntity);

            if (byEntity.World is IServerWorldAccessor && nutriProperties != null && secondsUsed >= 0.95f)
            {
                foreach (var nutriProps in nutriProperties)
                {
                    TransitionState state = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                    float spoilState = state != null ? state.TransitionLevel : 0;

                    float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
                    float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

                    byEntity.ReceiveSaturation(nutriProps.Satiety * satLossMul, nutriProps.FoodCategory);

                    IPlayer player = null;
                    if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                    if (nutriProps.EatenStack != null)
                    {
                        if (slot.Empty)
                        {
                            slot.Itemstack = nutriProps.EatenStack.ResolvedItemstack.Clone();
                        }
                        else
                        {
                            if (player == null || !player.InventoryManager.TryGiveItemstack(nutriProps.EatenStack.ResolvedItemstack.Clone(), true))
                            {
                                byEntity.World.SpawnItemEntity(nutriProps.EatenStack.ResolvedItemstack.Clone(), byEntity.Pos.XYZ);
                            }
                        }
                    }

                    float healthChange = nutriProps.Health * healthLossMul;

                    float intox = byEntity.WatchedAttributes.GetFloat("intoxication");
                    byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intox + nutriProps.Intoxication));

                    if (healthChange != 0)
                    {
                        byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
                    }
                }
            }
            base.tryEatStop(secondsUsed, slot, byEntity);
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {   
            ItemStack stack = inSlot.Itemstack;

            string descText = GetItemDescText();

            if (withDebugInfo)
            {
                dsc.AppendLine("<font color=\"#bbbbbb\">Id:" + Id + "</font>");
                dsc.AppendLine("<font color=\"#bbbbbb\">Code: " + Code + "</font>");
                if (api?.Side == EnumAppSide.Client && (api as ICoreClientAPI).Input.KeyboardKeyStateRaw[(int)GlKeys.ShiftLeft])
                {
                    dsc.AppendLine("<font color=\"#bbbbbb\">Attributes: " + inSlot.Itemstack.Attributes.ToJsonToken() + "</font>\n");
                }
            }

            int durability = GetMaxDurability(stack);

            if (durability > 1)
            {
                dsc.AppendLine(Lang.Get("Durability: {0} / {1}", stack.Collectible.GetRemainingDurability(stack), durability));
            }


            if (MiningSpeed != null && MiningSpeed.Count > 0)
            {
                dsc.AppendLine(Lang.Get("Tool Tier: {0}", ToolTier));

                dsc.Append(Lang.Get("item-tooltip-miningspeed"));
                int i = 0;
                foreach (var val in MiningSpeed)
                {
                    if (val.Value < 1.1) continue;

                    if (i > 0) dsc.Append(", ");
                    dsc.Append(Lang.Get(val.Key.ToString()) + " " + val.Value.ToString("#.#") + "x");
                    i++;
                }

                dsc.Append("\n");
            }

            var bag = GetCollectibleInterface<IHeldBag>();
            if (bag != null)
            {
                dsc.AppendLine(Lang.Get("Storage Slots: {0}", bag.GetQuantitySlots(stack)));

                bool didPrint = false;
                var stacks = bag.GetContents(stack, world);
                if (stacks != null)
                {
                    foreach (var cstack in stacks)
                    {
                        if (cstack == null || cstack.StackSize == 0) continue;

                        if (!didPrint)
                        {
                            dsc.AppendLine(Lang.Get("Contents: "));
                            didPrint = true;
                        }
                        cstack.ResolveBlockOrItem(world);
                        dsc.AppendLine("- " + cstack.StackSize + "x " + cstack.GetName());
                    }

                    if (!didPrint)
                    {
                        dsc.AppendLine(Lang.Get("Empty"));
                    }
                }
            }

            EntityPlayer entity = world.Side == EnumAppSide.Client ? (world as IClientWorldAccessor).Player.Entity : null;

            float spoilState = AppendPerishableInfoText(inSlot, dsc, world);
                        
            dsc.AppendLine(Lang.Get("Nutrition Facts"));


            FoodNutritionProperties nutriProps = GetNutritionProperties(world, stack, entity);
            FoodNutritionProperties[] extraNutriProperties = GetExtraNutritionProperties(world, stack, entity);
            if (nutriProps != null)
            {
                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, stack, entity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, stack, entity);
                
                dsc.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + nutriProps.FoodCategory.ToString().ToLowerInvariant()), Math.Round(nutriProps.Satiety * satLossMul)));
                float extraNutriHealth = 0;
                foreach (var extraNutriProps in extraNutriProperties)
                {
                    dsc.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + extraNutriProps.FoodCategory.ToString().ToLowerInvariant()), Math.Round(extraNutriProps.Satiety * satLossMul)));
                    extraNutriHealth += extraNutriProps.Health;
                }
                
                if (Math.Abs((nutriProps.Health + extraNutriHealth) * healthLossMul) > 0.001f)
                {
                    dsc.AppendLine("- " + Lang.Get("Health: {0}{1} hp", 
                        (Math.Round((nutriProps.Health + extraNutriHealth) * healthLossMul, 2) > 0f) ? "+" : "", 
                        Math.Round((nutriProps.Health + extraNutriHealth) * healthLossMul, 2)));
                }
            }       

            if (GrindingProps?.GroundStack?.ResolvedItemstack != null)
            {
                dsc.AppendLine(Lang.Get("When ground: Turns into {0}x {1}", GrindingProps.GroundStack.ResolvedItemstack.StackSize, GrindingProps.GroundStack.ResolvedItemstack.GetName()));
            }

            if (CrushingProps != null)
            {
                float quantity = CrushingProps.Quantity.avg * CrushingProps.CrushedStack.ResolvedItemstack.StackSize;
                dsc.AppendLine(Lang.Get("When pulverized: Turns into {0:0.#}x {1}", quantity, CrushingProps.CrushedStack.ResolvedItemstack.GetName()));
                dsc.AppendLine(Lang.Get("Requires Pulverizer tier: {0}", CrushingProps.HardnessTier));
            }

            if (GetAttackPower(stack) > 0.5f)
            {
                dsc.AppendLine(Lang.Get("Attack power: -{0} hp", GetAttackPower(stack).ToString("0.#")));
                dsc.AppendLine(Lang.Get("Attack tier: {0}", ToolTier));
            }

            if (GetAttackRange(stack) > GlobalConstants.DefaultAttackRange)
            {
                dsc.AppendLine(Lang.Get("Attack range: {0} m", GetAttackRange(stack).ToString("0.#")));
            }

            if (CombustibleProps != null)
            {
                string smelttype = CombustibleProps.SmeltingType.ToString().ToLowerInvariant();
                if (smelttype == "fire")
                {
                    // Custom for clay items - do not show firing temperature as that is irrelevant to Pit kilns

                    dsc.AppendLine(Lang.Get("itemdesc-fireinkiln"));
                }
                else
                {
                    if (CombustibleProps.BurnTemperature > 0)
                    {
                        dsc.AppendLine(Lang.Get("Burn temperature: {0}°C", CombustibleProps.BurnTemperature));
                        dsc.AppendLine(Lang.Get("Burn duration: {0}s", CombustibleProps.BurnDuration));
                    }

                    if (CombustibleProps.MeltingPoint > 0)
                    {
                        dsc.AppendLine(Lang.Get("game:smeltpoint-" + smelttype, CombustibleProps.MeltingPoint));
                    }
                }

                if (CombustibleProps.SmeltedStack?.ResolvedItemstack != null)
                {
                    int instacksize = CombustibleProps.SmeltedRatio;
                    int outstacksize = CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize;


                    string str = instacksize == 1 ?
                        Lang.Get("game:smeltdesc-" + smelttype + "-singular", outstacksize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName()) :
                        Lang.Get("game:smeltdesc-" + smelttype + "-plural", instacksize, outstacksize, CombustibleProps.SmeltedStack.ResolvedItemstack.GetName())
                    ;

                    dsc.AppendLine(str);
                }
            }

            foreach (var bh in CollectibleBehaviors)
            {
                bh.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            }

            if (descText.Length > 0 && dsc.Length > 0) dsc.Append("\n");
            dsc.Append(descText);

            float temp = GetTemperature(world, stack);
            if (temp > 20)
            {
                dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
            }

            if (Code != null && Code.Domain != "game")
            {
                var mod = api.ModLoader.GetMod(Code.Domain);
                dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? Code.Domain));
            }
        }
    }
}
