using ArtOfCooking.BlockEntities;
using ArtOfCooking.Items;
using ArtOfCooking.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Blocks
{
    public class AOCBlockShawarma : BlockMeal
    {
        public string State => Variant["state"];
        protected override bool PlacedBlockEating => false;

        AOCMeshCache ms;
        ICoreClientAPI capi;

        WorldInteraction[] interactions;
        BlockFacing[] AttachedToFaces = new BlockFacing[] { BlockFacing.DOWN };

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            capi = api as ICoreClientAPI;

            InteractionHelpYOffset = 0.375f;

            interactions = ObjectCacheUtil.GetOrCreate(api, "shawarmaInteractions-", () =>
            {
                List<ItemStack> fillStacks = new List<ItemStack>();
                List<ItemStack> lavashStacks = new List<ItemStack>();

                if (fillStacks.Count == 0 && lavashStacks.Count == 0)
                {
                    foreach (CollectibleObject obj in api.World.Collectibles)
                    {
                        if (obj is AOCItemLavash)
                        {
                            lavashStacks.Add(new ItemStack(obj, 1));
                        }

                        var shawarmaProps = obj.Attributes?["inCookedMealProperties"]?.AsObject<inCookedMealProperties>(null, obj.Code.Domain);
                        if (shawarmaProps != null && !(obj is AOCItemLavash) && !(obj is AOCItemDough) && !(obj is ItemDough))
                        {
                            fillStacks.Add(new ItemStack(obj, 1));
                        }
                    }
                }


                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:blockhelp-shawarma-addfilling",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = fillStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            AOCBEShawarma bec = api.World.BlockAccessor.GetBlockEntity(bs.Position) as AOCBEShawarma;
                            if (bec?.Inventory[0]?.Itemstack != null && (bec.Inventory[0].Itemstack.Collectible as AOCBlockShawarma).State != "raw" && !bec.HasAllFilling)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "artofcooking:blockhelp-shawarma-wrapping",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "ctrl",
                        RequireFreeHand = true,
                        ShouldApply = (wi, bs, es) =>
                        {
                            AOCBEShawarma bec = api.World.BlockAccessor.GetBlockEntity(bs.Position) as AOCBEShawarma;
                            if (bec?.Inventory[0]?.Itemstack != null && (bec.Inventory[0].Itemstack.Collectible as AOCBlockShawarma).State != "raw" && bec.HasAllFilling && bec.Inventory[0].Itemstack.Attributes.GetAsBool("wrapped") != true)
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                };
            });

            ms = api.ModLoader.GetModSystem<AOCMeshCache>();

            displayContentsInfo = false;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!canEat(slot)) return;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (!canEat(slot)) return false;

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (!canEat(slot)) return;

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (slot.Itemstack == null) return;

            if (!slot.Itemstack.Attributes.HasAttribute("quantityServings"))
            {
                slot.Itemstack.Attributes.SetFloat("quantityServings", 1);
            }

            float servingsLeft = slot.Itemstack.Attributes.GetFloat("quantityServings");

            if (servingsLeft == 1)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 0);
            }
            if (servingsLeft < 1)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 1);
            }
            if (servingsLeft <= 0.85f)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 2);
            }
            if (servingsLeft <= 0.7f)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 3);
            }
            if (servingsLeft <= 0.5f)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 4);
            }
            if (servingsLeft <= 0.35f)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 5);
            }
            if (servingsLeft <= 0.15f)
            {
                slot.Itemstack.Attributes.SetInt("shawarmaParts", 6);
            }
        }


        protected bool canEat(ItemSlot slot)
        {
            return
                slot.Itemstack.Attributes.GetAsBool("wrapped") == true
                && State != "raw"
            ;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

            renderinfo.ModelRef = ms.GetOrCreateShawarmaMeshRef(itemstack);
        }

       public override MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos = null)
        {
        ItemStack itemstack = slot.Itemstack;
        return ms.GetShawarmaMesh(itemstack);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            AOCBEShawarma bec = world.BlockAccessor.GetBlockEntity(pos) as AOCBEShawarma;
            if (bec?.Inventory[0]?.Itemstack != null) return bec.Inventory[0].Itemstack.Clone();

            return base.OnPickBlock(world, pos);
        }

        public void TryPlaceShawarma(EntityAgent byEntity, BlockSelection blockSel)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            BlockPos abovePos = blockSel.Position.UpCopy();

            Block atBlock = api.World.BlockAccessor.GetBlock(abovePos);
            if (atBlock.Replaceable < 6000) return;

            api.World.BlockAccessor.SetBlock(Id, abovePos);

            AOCBEShawarma beshawarma = api.World.BlockAccessor.GetBlockEntity(abovePos) as AOCBEShawarma;
            beshawarma.OnPlaced(byPlayer);
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            AOCBEShawarma bec = world.BlockAccessor.GetBlockEntity(pos) as AOCBEShawarma;
            if (bec?.Inventory[0]?.Itemstack != null) return GetHeldItemName(bec.Inventory[0].Itemstack);

            return base.GetPlacedBlockName(world, pos);
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            ItemStack[] cStacks = GetContents(api.World, itemStack);
            if (cStacks.Length <= 1) return Lang.Get("artofcooking:shawarma");

            ItemStack lstack = cStacks[0];
            ItemStack cstack = cStacks[1];
            if (cstack == null)
            {
                return Lang.Get("artofcooking:item-lavash-unleavened-" + lstack.Item.Variant["type"] + "-" + lstack.Item.Variant["state"]);
            }
            string lType = Lang.Get("game:meal-ingredient-porridge-primary-grain-" + lstack.Item.Variant["type"]);
            return Lang.Get("artofcooking:{0} shawarma", lType);
        }


        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {

            StringBuilder filldsc = new StringBuilder();
            ItemStack[] cStacks = GetContents(api.World, inSlot.Itemstack);
            string state = Variant["state"];
            if (state == "raw") return;

            string fill1 = null;
            string fill2 = null;
            string fill3 = null;
            string fill4 = null;
            string and = Lang.Get("artofcooking:and");
            string fillAll = null;

            fill1 = Lang.Get("recipeingredient-" + cStacks[1].Class.ToString().ToLowerInvariant() + "-" + cStacks[1].Collectible.Code?.Path + "-insturmentalcase");
            fill2 = Lang.Get("recipeingredient-" + cStacks[2].Class.ToString().ToLowerInvariant() + "-" + cStacks[2].Collectible.Code?.Path + "-insturmentalcase");
            fill3 = Lang.Get("recipeingredient-" + cStacks[3].Class.ToString().ToLowerInvariant() + "-" + cStacks[3].Collectible.Code?.Path + "-insturmentalcase");
            fill4 = Lang.Get("recipeingredient-" + cStacks[4].Class.ToString().ToLowerInvariant() + "-" + cStacks[4].Collectible.Code?.Path + "-insturmentalcase");

            if (fill1 != null)
            {
                fillAll = Lang.Get(fill1);

                if (fill1 != fill2)
                    fillAll = Lang.Get(fill1 + and + fill2);
                if (fill1 != fill3)
                    fillAll = Lang.Get(fill1 + and + fill3);
                if (fill4 != null) if (fill1 != fill4)
                        fillAll = Lang.Get(fill1 + and + fill4);

                if (fill1 != fill2 && fill1 != fill3 && fill2 != fill3)
                    fillAll = Lang.Get(fill1 + ", " + fill2 + and + fill3);
                if (fill4 != null) if (fill1 != fill2 && fill1 != fill4 && fill2 != fill4)
                        fillAll = Lang.Get(fill1 + ", " + fill2 + and + fill4);
                if (fill1 != fill3 && fill1 != fill4 && fill3 != fill4)
                    fillAll = Lang.Get(fill1 + ", " + fill3 + and + fill4);


                if (fill1 != fill2 && fill1 != fill3 && fill1 != fill4 && fill2 != fill3 && fill2 != fill4 && fill3 != fill4)
                    fillAll = Lang.Get(fill1 + ", " + fill2 + ", " + fill3 + and + fill4);
            }

            ItemStack shawarmaStack = inSlot.Itemstack;
            float servingsLeft = GetQuantityServings(world, inSlot.Itemstack);
            if (!inSlot.Itemstack.Attributes.HasAttribute("quantityServings")) servingsLeft = 1;
            if (servingsLeft == 1)
            {
                dsc.AppendLine(Lang.Get("artofcooking:shawarma-full", fillAll));
            }
            else
            {
                dsc.AppendLine(Lang.Get("artofcooking:shawarma-left", servingsLeft, fillAll));
            }

            TransitionableProperties[] propsm = shawarmaStack.Collectible.GetTransitionableProperties(api.World, shawarmaStack, null);
            if (propsm != null && propsm.Length > 0)
            {
                shawarmaStack.Collectible.AppendPerishableInfoText(inSlot, dsc, api.World);
            }

            ItemStack[] stacks = GetContents(api.World, shawarmaStack);

            var forEntity = (world as IClientWorldAccessor)?.Player?.Entity;


            float[] nmul = GetNutritionHealthMul(null, inSlot, forEntity);
            dsc.AppendLine(GetContentNutritionFacts(api.World, inSlot, stacks, null, true, servingsLeft * nmul[0], servingsLeft * nmul[1]));
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            AOCBEShawarma bep = world.BlockAccessor.GetBlockEntity(pos) as AOCBEShawarma;
            if (bep?.Inventory == null || bep?.Inventory.Count < 1 || bep.Inventory.Empty) return "";

            BlockMeal mealblock = api.World.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;

            ItemStack shawarmaStack = bep.Inventory[0].Itemstack;
            ItemStack[] stacks = GetContents(api.World, shawarmaStack);

            ItemStack cstack = stacks[1];
            if (cstack == null) return "";

            StringBuilder sb = new StringBuilder();

            TransitionableProperties[] propsm = shawarmaStack.Collectible.GetTransitionableProperties(api.World, shawarmaStack, null);
            if (propsm != null && propsm.Length > 0)
            {
                shawarmaStack.Collectible.AppendPerishableInfoText(bep.Inventory[0], sb, api.World);
            }

            float servingsLeft = GetQuantityServings(world, bep.Inventory[0].Itemstack);
            if (!bep.Inventory[0].Itemstack.Attributes.HasAttribute("quantityServings")) servingsLeft = 1;

            float[] nmul = GetNutritionHealthMul(pos, null, forPlayer.Entity);

            sb.AppendLine(GetContentNutritionFacts(api.World, bep.Inventory[0], stacks, null, true, nmul[0] * servingsLeft, nmul[1] * servingsLeft));

            return sb.ToString();
        }

        protected override TransitionState[] UpdateAndGetTransitionStatesNative(IWorldAccessor world, ItemSlot inslot)
        {
            return base.UpdateAndGetTransitionStatesNative(world, inslot);
        }

        public override TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
        {
            ItemStack[] cstacks = GetContents(world, inslot.Itemstack);
            UnspoilContents(world, cstacks);
            SetContents(inslot.Itemstack, cstacks);

            return base.UpdateAndGetTransitionState(world, inslot, type);
        }

        public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
        {
            ItemStack[] cstacks = GetContents(world, inslot.Itemstack);
            UnspoilContents(world, cstacks);
            SetContents(inslot.Itemstack, cstacks);


            return base.UpdateAndGetTransitionStatesNative(world, inslot);
        }

        new public static FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, ItemStack[] contentStacks, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
        {
            List<FoodNutritionProperties> list = new List<FoodNutritionProperties>();
            if (contentStacks == null)
            {
                return list.ToArray();
            }

            for (int i = 0; i < contentStacks.Length; i++)
            {
                if (contentStacks[i] != null)
                {
                    CollectibleObject collectible = contentStacks[i].Collectible;
                    FoodNutritionProperties foodNutritionProperties = ((collectible.CombustibleProps == null || collectible.CombustibleProps.SmeltedStack == null) ? collectible.GetNutritionProperties(world, contentStacks[i], forEntity) : collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible.GetNutritionProperties(world, collectible.CombustibleProps.SmeltedStack.ResolvedItemstack, forEntity));

                    if (foodNutritionProperties != null)
                    {
                        float num = ((!mulWithStacksize) ? 1 : contentStacks[i].StackSize);
                        FoodNutritionProperties foodNutritionProperties2 = foodNutritionProperties.Clone();
                        DummySlot dummySlot = new DummySlot(contentStacks[i], inSlot.Inventory);
                        float spoilState = contentStacks[i].Collectible.UpdateAndGetTransitionState(world, dummySlot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
                        float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, dummySlot.Itemstack, forEntity);
                        float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, dummySlot.Itemstack, forEntity);
                        foodNutritionProperties2.Satiety *= num2 * nutritionMul * num;
                        foodNutritionProperties2.Health *= num3 * healthMul * num;
                        list.Add(foodNutritionProperties2);
                    }
                }
            }

            return list.ToArray();
        }

        new public FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity)
        {
            ItemStack[] nonEmptyContents = GetNonEmptyContents(world, inSlot.Itemstack);
            if (nonEmptyContents == null || nonEmptyContents.Length == 0)
            {
                return null;
            }

            float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, forEntity);
            return GetContentNutritionProperties(world, inSlot, nonEmptyContents, forEntity, GetRecipeCode(world, inSlot.Itemstack) == null, nutritionHealthMul[0], nutritionHealthMul[1]);
        }

        public override string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, ItemStack[] contentStacks, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
        {
            UnspoilContents(world, contentStacks);
            FoodNutritionProperties[] contentNutritionProperties = GetContentNutritionProperties(world, inSlotorFirstSlot, contentStacks, forEntity, mulWithStacksize, nutritionMul, healthMul);
            Dictionary<EnumFoodCategory, float> dictionary = new Dictionary<EnumFoodCategory, float>();
            float num = 0f;
            for (int i = 0; i < contentNutritionProperties.Length; i++)
            {
                FoodNutritionProperties foodNutritionProperties = contentNutritionProperties[i];
                if (foodNutritionProperties != null)
                {
                    dictionary.TryGetValue(foodNutritionProperties.FoodCategory, out var value);
                    DummySlot dummySlot = new DummySlot(contentStacks[i], inSlotorFirstSlot.Inventory);
                    float spoilState = contentStacks[i].Collectible.UpdateAndGetTransitionState(api.World, dummySlot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
                    float num2 = GlobalConstants.FoodSpoilageSatLossMul(spoilState, dummySlot.Itemstack, forEntity);
                    float num3 = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, dummySlot.Itemstack, forEntity);
                    num += foodNutritionProperties.Health * num3;
                    dictionary[foodNutritionProperties.FoodCategory] = value + foodNutritionProperties.Satiety * num2;
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Lang.Get("Nutrition Facts"));
            foreach (KeyValuePair<EnumFoodCategory, float> item in dictionary)
            {
                stringBuilder.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + item.Key.ToString().ToLowerInvariant()), Math.Round(item.Value)));
            }

            if (num != 0f)
            {
                stringBuilder.AppendLine("- " + Lang.Get("Health: {0}{1} hp", (num > 0f) ? "+" : "", num));
            }

            return stringBuilder.ToString();
        }

        new public string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity, bool mulWithStacksize = false)
        {
            float[] nutritionHealthMul = GetNutritionHealthMul(null, inSlot, forEntity);
            return GetContentNutritionFacts(world, inSlot, GetNonEmptyContents(world, inSlot.Itemstack), forEntity, mulWithStacksize, nutritionHealthMul[0], nutritionHealthMul[1]);
        }

        protected void UnspoilContents(IWorldAccessor world, ItemStack[] cstacks)
        {
            // Dont spoil the shawarma contents, the shawarma itself has a spoilage timer. Semi hacky fix reset their spoil timers each update

            for (int i = 0; i < cstacks.Length; i++)
            {
                ItemStack cstack = cstacks[i];
                if (cstack == null) continue;

                if (!(cstack.Attributes["transitionstate"] is ITreeAttribute))
                {
                    cstack.Attributes["transitionstate"] = new TreeAttribute();
                }
                ITreeAttribute attr = (ITreeAttribute)cstack.Attributes["transitionstate"];

                if (attr.HasAttribute("createdTotalHours"))
                {
                    attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
                    attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
                    var transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute)?.value;
                    for (int j = 0; transitionedHours != null && j < transitionedHours.Length; j++)
                    {
                        transitionedHours[j] = 0;
                    }
                }
            }
        }


        public override float[] GetNutritionHealthMul(BlockPos pos, ItemSlot slot, EntityAgent forEntity)
        {
            float satLossMul = 1f;

            if (slot == null && pos != null)
            {
                AOCBEShawarma bep = api.World.BlockAccessor.GetBlockEntity(pos) as AOCBEShawarma;
                slot = bep.Inventory[0];
            }

            if (slot != null)
            {
                TransitionState state = slot.Itemstack.Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                float spoilState = state != null ? state.TransitionLevel : 0;
                satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, forEntity);
            }

            return new float[] { Attributes["nutritionMul"].AsFloat(1) * satLossMul, satLossMul };
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            AOCBEShawarma bep = world.BlockAccessor.GetBlockEntity(blockSel.Position) as AOCBEShawarma;
            if (!bep.OnInteract(byPlayer))
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            // Don't call eating stuff from blockmeal
            //base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override int GetRandomContentColor(ICoreClientAPI capi, ItemStack[] stacks)
        {
            ItemStack[] cstacks = GetContents(capi.World, stacks[0]);
            if (cstacks.Length == 0) return 0;

            ItemStack rndStack = cstacks[capi.World.Rand.Next(stacks.Length)];
            return rndStack.Collectible.GetRandomColor(capi, rndStack);
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (EntityClass != null)
            {
                world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
            }

            world.BlockAccessor.SetBlock(0, pos);

        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var baseinteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            baseinteractions = baseinteractions.RemoveAt(1);

            var allinteractions = interactions.Append(baseinteractions);
            return allinteractions;
        }
    }
}

