using CoreOfArts.Systems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CoreOfArts.Systems
{
    public class COARecipeLoader : ModSystem
    {
        ICoreServerAPI api;

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.api = sapi;

            LoadRecipes<COADoughFormingRecipe>("dough forming recipe", "recipes/doughforming", (r) => sapi.RegisterDoughFormingRecipe(r));

            LoadRecipes<COALiquidMixingRecipe>("liquid mixing recipe", "recipes/liquidmixing", (r) => sapi.RegisterLiquidMixingRecipe(r));

            sapi.World.Logger.StoryEvent(Lang.Get("Kneaded dough..."));
        }

        public void LoadRecipes<T>(string name, string path, Action<T> RegisterMethod) where T : IRecipeBase
        {
            Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, path);
            int recipeQuantity = 0;
            int quantityRegistered = 0;
            int quantityIgnored = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    LoadGenericRecipe(name, val.Key, val.Value.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
                    recipeQuantity++;
                }

                if (val.Value is JArray)
                {
                    foreach (var token in val.Value as JArray)
                    {
                        LoadGenericRecipe(name, val.Key, token.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
                        recipeQuantity++;
                    }
                }
            }

            api.World.Logger.Event("{0} {1}s loaded{2}", quantityRegistered, name, quantityIgnored > 0 ? string.Format(" ({0} could not be resolved)", quantityIgnored) : "");
        }

        void LoadGenericRecipe<T>(string className, AssetLocation path, T recipe, Action<T> RegisterMethod, ref int quantityRegistered, ref int quantityIgnored) where T : IRecipeBase
        {
            if (!recipe.Enabled) return;
            if (recipe.Name == null) recipe.Name = path;

            recipe.OnParsed(api.World);

            bool generatedAny = false;

            foreach (IRecipeBase generatedRecipeBase in recipe.GenerateRecipesForAllIngredientCombinations(api.World))
            {
                generatedAny = true;

                if (generatedRecipeBase.Name == null)
                {
                    generatedRecipeBase.Name = path;
                }

                if (!generatedRecipeBase.Resolve(api.World, className + " " + path))
                {
                    quantityIgnored++;
                    continue;
                }

                RegisterMethod((T)generatedRecipeBase);
                quantityRegistered++;
            }

            if (!generatedAny)
            {
                api.World.Logger.Warning("{1} file {0} could not generate any recipe variants.", path, className);
            }
        }
    }

    public static class AOCApiAdditions
    {
        public static List<COADoughFormingRecipe> GetDoughformingRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<COARecipeRegistrySystem>().DoughFormingRecipes;
        }

        public static List<COALiquidMixingRecipe> GetLiquidMixingRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<COARecipeRegistrySystem>().LiquidMixingRecipes;
        }

        public static void RegisterDoughFormingRecipe(this ICoreServerAPI api, COADoughFormingRecipe r)
        {
            api.ModLoader.GetModSystem<COARecipeRegistrySystem>().RegisterDoughFormingRecipe(r);
        }

        public static void RegisterLiquidMixingRecipe(this ICoreServerAPI api, COALiquidMixingRecipe r)
        {
            api.ModLoader.GetModSystem<COARecipeRegistrySystem>().RegisterLiquidMixingRecipe(r);
        }
    }

    public class AOCDisableRecipeRegisteringSystem : ModSystem
    {
        public override double ExecuteOrder() => 99999;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void AssetsFinalize(ICoreAPI api)
        {
            COARecipeRegistrySystem.canRegister = false;
        }
    }

    public class COARecipeRegistrySystem : ModSystem
    {
        public static bool canRegister = true;

        public List<COADoughFormingRecipe> DoughFormingRecipes = new List<COADoughFormingRecipe>();
        public List<COALiquidMixingRecipe> LiquidMixingRecipes = new List<COALiquidMixingRecipe>();

        public override double ExecuteOrder()
        {
            return 0.6;
        }

        public override void StartPre(ICoreAPI api)
        {
            canRegister = true;
        }

        public override void Start(ICoreAPI api)
        {
            DoughFormingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<COADoughFormingRecipe>>("doughformingrecipes").Recipes;

            LiquidMixingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<COALiquidMixingRecipe>>("liquidmixingrecipes").Recipes;
        }

        public void RegisterDoughFormingRecipe(COADoughFormingRecipe recipe)
        {
            if (!canRegister) throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            recipe.RecipeId = DoughFormingRecipes.Count + 1;

            DoughFormingRecipes.Add(recipe);
        }

        public void RegisterLiquidMixingRecipe(COALiquidMixingRecipe recipe)
        {
            if (!canRegister) throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");

            if (recipe.Code == null)
            {
                throw new ArgumentException("LiquidMixing recipes must have a non-null code! (choose freely)");
            }

            foreach (var ingred in recipe.Ingredients)
            {
                if (ingred.ConsumeQuantity != null && ingred.ConsumeQuantity > ingred.Quantity)
                {
                    throw new ArgumentException("Liquid Mixing recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
                }
            }

            LiquidMixingRecipes.Add(recipe);
        }
    }
}
