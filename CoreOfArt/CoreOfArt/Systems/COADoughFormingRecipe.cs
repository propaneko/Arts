using System.IO;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CoreOfArts.Systems
{
    public class COADoughFormingRecipe : RecipeBase, IByteSerializable, ICOARecipe
    {
        public int QuantityLayers => 16;
        public string RecipeCategoryCode => "dough forming";

        public string[][] Pattern;
        public CraftingRecipeIngredient Ingredient;
        public JsonItemStack Output;

        public override IRecipeIngredient[] RecipeIngredients => Ingredient != null
            ? new IRecipeIngredient[] { Ingredient }
            : System.Array.Empty<IRecipeIngredient>();

        public override IRecipeOutput RecipeOutput => Output;

        public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if (Ingredient != null)
                return Ingredient.Resolve(world, sourceForErrorLogging);
            return false;
        }

        public override RecipeBase Clone()
        {
            COADoughFormingRecipe recipe = new COADoughFormingRecipe();
            recipe.Pattern = new string[Pattern.Length][];
            for (int i = 0; i < recipe.Pattern.Length; i++)
            {
                recipe.Pattern[i] = (string[])Pattern[i].Clone();
            }
            recipe.Ingredient = Ingredient.Clone();
            recipe.Output = Output.Clone();
            recipe.Name = Name;
            return recipe;
        }

        public override void ToBytes(BinaryWriter writer)
{
    Ingredient.ToBytes(writer);

    if (Output?.ResolvedItemstack != null)
    {
        JsonItemStack resolvedOutput = new JsonItemStack()
        {
            Type = Output.ResolvedItemstack.Class,
            Code = Output.ResolvedItemstack.Collectible.Code,
            StackSize = Output.ResolvedItemstack.StackSize
        };

        resolvedOutput.ToBytes(writer);
    }
    else
    {
        Output.ToBytes(writer);
    }
}

        public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            Ingredient = new CraftingRecipeIngredient();
            Ingredient.FromBytes(reader, resolver);
            Ingredient.Resolve(resolver, "DoughForming FromBytes");
            Output = new JsonItemStack();
            Output.FromBytes(reader, resolver.ClassRegistry);
            Output.Resolve(resolver, "DoughForming FromBytes");
        }

        IRecipeIngredient[] ICOARecipe.Ingredients => Ingredient != null
            ? new IRecipeIngredient[] { Ingredient }
            : Array.Empty<IRecipeIngredient>();
        IRecipeOutput ICOARecipe.Output => RecipeOutput;
        ICOARecipe ICOARecipe.Clone() => (COADoughFormingRecipe)Clone();

        public bool[,,] Voxels
        {
            get
            {
                bool[,,] voxels = new bool[16, 16, 16];
                if (Pattern == null) return voxels;
                for (int y = 0; y < Pattern.Length; y++)
                {
                    string[] rows = Pattern[y];
                    for (int z = 0; z < rows.Length; z++)
                    {
                        string row = rows[z];
                        for (int x = 0; x < row.Length; x++)
                        {
                            voxels[x, y, z] = row[x] != '_';
                        }
                    }
                }
                return voxels;
            }
        }
    }
}
