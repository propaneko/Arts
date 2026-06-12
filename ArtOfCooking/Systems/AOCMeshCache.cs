using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.GameContent;
using System.Security.Cryptography;
using ArtOfCooking.Blocks;
using ArtOfCooking.BlockEntities;


namespace ArtOfCooking.Systems
{
    public class AOCMeshCache : ModSystem, ITexPositionSource
    {
        Block mealtextureSourceBlock;
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        ICoreClientAPI capi;

        AssetLocation[] shawarmaShapeLocByFillLevel = new AssetLocation[]
        {
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-fill0"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-fill1"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-fill2"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-fill3"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-fill4"),
        };

        AssetLocation[] shawarmaShapeLocByWrapped = new AssetLocation[]
        {
            new AssetLocation("artofcooking:block/food/shawarma/shawarma"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step1"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step2"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step3"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step4"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step5"),
            new AssetLocation("artofcooking:block/food/shawarma/shawarma-step6")
        };

        #region Shawarma Stuff
        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
        protected Shape nowTesselatingShape;

        AOCBlockShawarma nowTesselatingBlock;
        ItemStack[] contentStacks;
        AssetLocation crustTextureLoc;
        AssetLocation filling1TextureLoc;
        AssetLocation filling2TextureLoc;
        AssetLocation filling3TextureLoc;
        AssetLocation filling4TextureLoc;

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                AssetLocation texturePath = crustTextureLoc;
                if (textureCode == "filling1") texturePath = filling1TextureLoc;
                if (textureCode == "filling2") texturePath = filling2TextureLoc;
                if (textureCode == "filling3") texturePath = filling3TextureLoc;
                if (textureCode == "filling4") texturePath = filling4TextureLoc;

                if (texturePath == null)
                {
                    capi.World.Logger.Warning("Missing texture path for shawarma mesh texture code {0}, seems like a missing texture definition or invalid shawarma block.", textureCode);
                    return capi.BlockTextureAtlas.UnknownTexturePosition;
                }

                TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];

                if (texpos == null)
                {
                    IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
                    if (texAsset != null)
                    {
                        BitmapRef bmp = texAsset.ToBitmap(capi);
                        capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out _, out texpos, () => bmp);
                    }
                    else
                    {
                        capi.World.Logger.Warning("Shawarma mesh texture {1} not found.", nowTesselatingBlock.Code, texturePath);
                        texpos = capi.BlockTextureAtlas.UnknownTexturePosition;
                    }
                }


                return texpos;
            }
        }

        #endregion

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

            api.Event.LeaveWorld += Event_LeaveWorld;
            api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
        }
        private void Event_BlockTexturesLoaded()
        {
            mealtextureSourceBlock = capi.World.Blocks.FirstOrDefault(b =>
                b?.Code?.Domain == "game" &&
                b.Code?.Path?.StartsWith("claypot-") == true &&
                b.Code.Path.EndsWith("-cooked")
            );
        }

        public MultiTextureMeshRef GetOrCreateShawarmaMeshRef(ItemStack shawarmaStack)
        {
            Dictionary<int, MultiTextureMeshRef> meshrefs;

            object obj;
            if (capi.ObjectCache.TryGetValue("shawarmaMeshRefs", out obj))
            {
                meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
            }
            else
            {
                capi.ObjectCache["shawarmaMeshRefs"] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
            }

            if (shawarmaStack == null) return null;


            ItemStack[] contentStacks = (shawarmaStack.Block as AOCBlockShawarma).GetContents(capi.World, shawarmaStack);

            string extrakey = "wr" + shawarmaStack.Attributes.GetAsInt("wrapped") + "-sp" + shawarmaStack.Attributes.GetAsInt("shawarmaParts");

            int mealhashcode = GetMealHashCode(shawarmaStack.Block, contentStacks, null, extrakey);

            MultiTextureMeshRef mealMeshRef;

            if (!meshrefs.TryGetValue(mealhashcode, out mealMeshRef))
            {
                MeshData mesh = GetShawarmaMesh(shawarmaStack);
                if (mesh == null) return null;

                meshrefs[mealhashcode] = mealMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
            }

            return mealMeshRef;
        }


        public MeshData GetShawarmaMesh(ItemStack shawarmaStack, ModelTransform transform = null)
        {
            // Slot 0: Base dough
            // Slot 1: Filling
            // Slot 2: Crust dough

            nowTesselatingBlock = shawarmaStack.Block as AOCBlockShawarma;
            if (nowTesselatingBlock == null) return null;  //This will occur if the shawarmaStack changed to rot

            contentStacks = nowTesselatingBlock.GetContents(capi.World, shawarmaStack);

            int shawarmaParts = shawarmaStack.Attributes.GetAsInt("shawarmaParts");

            var stackprops = contentStacks.Select(stack => stack?.ItemAttributes?["inCookedMealProperties"]?.AsObject<inCookedMealProperties>(null, stack.Collectible.Code.Domain)).ToArray();

            if (stackprops.Length == 0) return null;


            ItemStack cstack = contentStacks[1];
            for (int i = 2; i < contentStacks.Length - 1; i++)
            {
                if (contentStacks[i] == null || cstack == null) continue;
                cstack = contentStacks[i];
            }


            if (ContentsRotten(contentStacks))
            {
                crustTextureLoc = new AssetLocation("game:block/rot/rot");
                filling1TextureLoc = new AssetLocation("game:block/rot/rot");
                filling2TextureLoc = new AssetLocation("game:block/rot/rot");
                filling3TextureLoc = new AssetLocation("game:block/rot/rot");
                filling4TextureLoc = new AssetLocation("game:block/rot/rot");
            }
            else
            {
                if (stackprops[0] != null)
                {
                    crustTextureLoc = stackprops[0].Texture.Clone();
                    filling1TextureLoc = new AssetLocation("game:block/transparent");
                    filling2TextureLoc = new AssetLocation("game:block/transparent");
                    filling3TextureLoc = new AssetLocation("game:block/transparent");
                    filling4TextureLoc = new AssetLocation("game:block/transparent");
                }

                if (contentStacks[1] != null)
                {
                    EnumFoodCategory fillingFoodCat =
                        contentStacks[1].Collectible.NutritionProps?.FoodCategory
                        ?? contentStacks[1].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory
                        ?? EnumFoodCategory.Vegetable
                    ;
                    filling1TextureLoc = stackprops[1]?.Texture;
                }
                if (contentStacks[2] != null)
                {
                    EnumFoodCategory fillingFoodCat =
                        contentStacks[2].Collectible.NutritionProps?.FoodCategory
                        ?? contentStacks[2].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory
                        ?? EnumFoodCategory.Vegetable
                    ;
                    filling2TextureLoc = stackprops[2]?.Texture;
                }
                if (contentStacks[3] != null)
                {
                    EnumFoodCategory fillingFoodCat =
                        contentStacks[3].Collectible.NutritionProps?.FoodCategory
                        ?? contentStacks[3].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory
                        ?? EnumFoodCategory.Vegetable
                    ;
                    filling3TextureLoc = stackprops[3]?.Texture;
                }
                if (contentStacks[4] != null)
                {
                    EnumFoodCategory fillingFoodCat =
                        contentStacks[4].Collectible.NutritionProps?.FoodCategory
                        ?? contentStacks[4].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory
                        ?? EnumFoodCategory.Vegetable
                    ;
                    filling4TextureLoc = stackprops[4]?.Texture;
                }
            }


            int fillLevel = (contentStacks[1] != null ? 1 : 0) + (contentStacks[2] != null ? 1 : 0) + (contentStacks[3] != null ? 1 : 0) + (contentStacks[4] != null ? 1 : 0);
            bool isComplete = shawarmaStack.Attributes.GetAsBool("wrapped");

            AssetLocation shapeloc = isComplete ? shawarmaShapeLocByWrapped[shawarmaParts] : shawarmaShapeLocByFillLevel[fillLevel];

            shapeloc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            Shape shape = Shape.TryGet(capi, shapeloc);
            MeshData mesh;

            capi.Tesselator.TesselateShape("shawarma", shape, out mesh, this, null, 0, 0, 0, null, null);
            if (transform != null) mesh.ModelTransform(transform);

            return mesh;
        }

        public static bool ContentsRotten(ItemStack[] contentStacks)
        {
            for (int i = 0; i < contentStacks.Length; i++)
            {
                if (contentStacks[i]?.Collectible.Code.Path == "rot") return true;
            }
            return false;
        }
        public static bool ContentsRotten(InventoryBase inv)
        {
            foreach (var slot in inv)
            {
                if (slot.Itemstack?.Collectible.Code.Path == "rot") return true;
            }
            return false;
        }
        private void Event_LeaveWorld()
        {
            if (capi == null) return;

            object obj;
            if (capi.ObjectCache.TryGetValue("cookedMeshRefs", out obj))
            {
                Dictionary<int, MultiTextureMeshRef> meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;

                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }

                capi.ObjectCache.Remove("cookedMeshRefs");
            }
        }

        public int GetMealHashCode(ItemStack stack, Vec3f translate = null, string extraKey = "")
        {
            ItemStack[] contentStacks = (stack.Block as BlockContainer).GetContents(capi.World, stack);

            if (stack.Block is AOCBlockShawarma)
            {
                extraKey = "wr" + stack.Attributes.GetAsInt("wrapped") + "-sp" + stack.Attributes.GetAsInt("shawarmaParts");
            }

            return GetMealHashCode(stack.Block, contentStacks, translate, extraKey);
        }

        protected int GetMealHashCode(Block block, ItemStack[] contentStacks, Vec3f translate = null, string extraKey = null)
        {
            string shapestring = block.Shape.ToString() + block.Code.ToShortString();
            if (translate != null) shapestring += translate.X + "/" + translate.Y + "/" + translate.Z;

            string contentstring = "";
            for (int i = 0; i < contentStacks.Length; i++)
            {
                if (contentStacks[i] == null) continue;

                if (contentStacks[i].Collectible.Code.Path == "rot")
                {
                    return (shapestring + "rotten").GetHashCode();
                }

                contentstring += contentStacks[i].Collectible.Code.ToShortString();
            }

            return (shapestring + contentstring + extraKey).GetHashCode();
        }
        
        public MultiTextureMeshRef GetOrCreateMealInContainerMeshRef(Block containerBlock, CookingRecipe forRecipe, ItemStack[] contentStacks, Vec3f foodTranslate = null)
        {
            Dictionary<int, MultiTextureMeshRef> meshrefs;

            object obj;
            if (capi.ObjectCache.TryGetValue("cookedMeshRefs", out obj))
            {
                meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
            }
            else
            {
                capi.ObjectCache["cookedMeshRefs"] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
            }

            if (contentStacks == null) return null;

            int mealhashcode = GetMealHashCode(containerBlock, contentStacks, foodTranslate);

            MultiTextureMeshRef mealMeshRef;

            if (!meshrefs.TryGetValue(mealhashcode, out mealMeshRef))
            {
                MeshData mesh = GenMealInContainerMesh(containerBlock, forRecipe, contentStacks, foodTranslate);

                meshrefs[mealhashcode] = mealMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
            }

            return mealMeshRef;
        }

        public MeshData GenMealInContainerMesh(Block containerBlock, CookingRecipe forRecipe, ItemStack[] contentStacks, Vec3f foodTranslate = null)
        {
            CompositeShape cShape = containerBlock.Shape;
            var loc = cShape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");

            Shape shape = Shape.TryGet(capi, loc);
            MeshData wholeMesh;

            var texSource = capi.Tesselator.GetTextureSource(containerBlock, 0, true)
                ?? capi.Tesselator.GetTextureSource(mealtextureSourceBlock);

            capi.Tesselator.TesselateShape("meal", shape, out wholeMesh, texSource,
                new Vec3f(cShape.rotateX, cShape.rotateY, cShape.rotateZ));

            MeshData mealMesh = GenMealMesh(forRecipe, contentStacks, containerBlock, foodTranslate);
            if (mealMesh != null)
            {
                wholeMesh.AddMeshData(mealMesh);
            }

            return wholeMesh;
        }

        public MeshData GenMealMesh(CookingRecipe forRecipe, ItemStack[] contentStacks, Block containerBlock, Vec3f foodTranslate = null)
        {
            MealTextureSource source = new MealTextureSource(capi, mealtextureSourceBlock);

            if (forRecipe != null)
            {
                MeshData foodMesh = GenFoodMixMesh(contentStacks, forRecipe, containerBlock, foodTranslate);
                if (foodMesh != null)
                {
                    return foodMesh;
                }
            }

            if (contentStacks != null && contentStacks.Length > 0)
            {
                bool rotten = ContentsRotten(contentStacks);
                if (rotten)
                {
                    Shape contentShape = Shape.TryGet(capi, "shapes/block/food/meal/rot.json"); 
                    if(containerBlock.FirstCodePart().Equals("spoon") == true) contentShape = Shape.TryGet(capi, "shapes/block/food/meal/rot-spoon.json"); 

                    MeshData contentMesh;
                    capi.Tesselator.TesselateShape("rotcontents", contentShape, out contentMesh, source);

                    if (foodTranslate != null)
                    {
                        contentMesh.Translate(foodTranslate);
                    }

                    return contentMesh;
                }
                else
                {


                    JsonObject obj = contentStacks[0]?.ItemAttributes?["inContainerTexture"];
                    if (obj != null && obj.Exists)
                    {
                        source.ForStack = contentStacks[0];

                        CompositeShape cshape = contentStacks[0]?.ItemAttributes?["inBowlShape"].AsObject<CompositeShape>(new CompositeShape() { Base = new AssetLocation("shapes/block/food/meal/pickled.json") });
                        if(containerBlock.FirstCodePart().Equals("spoon") == true) cshape = new CompositeShape() { Base = new AssetLocation("shapes/block/food/meal/pickled-spoon.json") };

                        Shape contentShape = Shape.TryGet(capi, cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));

                        MeshData contentMesh;
                        capi.Tesselator.TesselateShape("picklednmealcontents", contentShape, out contentMesh, source);

                        return contentMesh;
                    }
                }
            }

            return null;
        }
        public MeshData GenFoodMixMesh(ItemStack[] contentStacks, CookingRecipe recipe, Block containerBlock, Vec3f foodTranslate)
        {
            MeshData mergedmesh = null;
            MealTextureSource texSource = new MealTextureSource(capi, mealtextureSourceBlock);
            
            var shapePath = recipe.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            if(containerBlock.FirstCodePart().Equals("spoon") == true) shapePath = recipe.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce("-spoon.json");

            bool rotten = ContentsRotten(contentStacks);
            if (rotten)
            {
                shapePath = new AssetLocation("shapes/block/food/meal/rot.json");
                if(containerBlock.FirstCodePart().Equals("spoon") == true) shapePath = new AssetLocation("shapes/block/food/meal/rot-spoon.json");
            }
            Shape shape = Shape.TryGet(capi, shapePath);
            if (shape == null)
            {
                shapePath = new AssetLocation("shapes/block/food/meal/pickled.json");
                if(containerBlock.FirstCodePart().Equals("spoon") == true) shapePath = new AssetLocation("shapes/block/food/meal/pickled-spoon.json");
                shape = Shape.TryGet(capi, shapePath);
            }
            Dictionary<CookingRecipeIngredient, int> usedIngredQuantities = new Dictionary<CookingRecipeIngredient, int>();

            if (rotten)
            {
                capi.Tesselator.TesselateShape(
                    "mealpart", shape, out mergedmesh, texSource,
                    new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ)
                );
            }
            else
            {
                HashSet<string> drawnMeshes = new HashSet<string>();

                for (int i = 0; i < contentStacks.Length; i++)
                {
                    texSource.ForStack = contentStacks[i];
                    CookingRecipeIngredient ingred = recipe.GetIngrendientFor(
                        contentStacks[i],
                        usedIngredQuantities.Where(val => val.Key.MaxQuantity <= val.Value).Select(val => val.Key).ToArray()
                    );

                    if (ingred == null)
                    {
                        ingred = recipe.GetIngrendientFor(contentStacks[i]);
                    }
                    else
                    {
                        int cnt = 0;
                        usedIngredQuantities.TryGetValue(ingred, out cnt);
                        cnt++;
                        usedIngredQuantities[ingred] = cnt;
                    }

                    if (ingred == null) continue;


                    MeshData meshpart;
                    string[] selectiveElements = null;

                    CookingRecipeStack recipestack = ingred.GetMatchingStack(contentStacks[i]);

                    if (recipestack.ShapeElement != null) selectiveElements = new string[] { recipestack.ShapeElement };
                    texSource.customTextureMapping = recipestack.TextureMapping;

                    if (drawnMeshes.Contains(recipestack.ShapeElement + recipestack.TextureMapping)) continue;
                    drawnMeshes.Add(recipestack.ShapeElement + recipestack.TextureMapping);

                    capi.Tesselator.TesselateShape(
                        "mealpart", shape, out meshpart, texSource,
                        new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, 0, null, selectiveElements
                    );

                    if (mergedmesh == null) mergedmesh = meshpart;
                    else mergedmesh.AddMeshData(meshpart);
                }

            }


            if (foodTranslate != null && mergedmesh != null) mergedmesh.Translate(foodTranslate);

            return mergedmesh;
        }
    }
}
