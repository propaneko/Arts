using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static HarmonyLib.Code;

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
    bool ok = true;

    if (Ingredient != null)
    {
        ok &= Ingredient.Resolve(world, sourceForErrorLogging);
    }
    else
    {
        ok = false;
    }

    if (Output != null)
    {
        ok &= Output.Resolve(world, sourceForErrorLogging);
    }
    else
    {
        ok = false;
    }

    return ok;
}
protected override Dictionary<string, HashSet<string>> GetNameToCodeMapping(IWorldAccessor world)
{
    Dictionary<string, HashSet<string>> mappings = new Dictionary<string, HashSet<string>>();

    if (Ingredient?.Code == null || Ingredient.Name == null || !Ingredient.Code.Path.Contains("*"))
    {
        return mappings;
    }

    HashSet<string> codes = new HashSet<string>();

    int wildcardStartLen = Ingredient.Code.Path.IndexOf("*");
    int wildcardEndLen = Ingredient.Code.Path.Length - wildcardStartLen - 1;

    if (Ingredient.Type == EnumItemClass.Block)
    {
        foreach (Block block in world.Blocks)
        {
            if (block.Code == null || block.IsMissing) continue;

            if (WildcardUtil.Match(Ingredient.Code, block.Code))
            {
                string code = block.Code.Path.Substring(wildcardStartLen);
                string codepart = code.Substring(0, code.Length - wildcardEndLen);

                if (Ingredient.AllowedVariants != null && !Ingredient.AllowedVariants.Contains(codepart)) continue;

                codes.Add(codepart);
            }
        }
    }
    else
    {
        foreach (Item item in world.Items)
        {
            if (item.Code == null || item.IsMissing) continue;

            if (WildcardUtil.Match(Ingredient.Code, item.Code))
            {
                string code = item.Code.Path.Substring(wildcardStartLen);
                string codepart = code.Substring(0, code.Length - wildcardEndLen);

                if (Ingredient.AllowedVariants != null && !Ingredient.AllowedVariants.Contains(codepart)) continue;

                codes.Add(codepart);
            }
        }
    }

    mappings[Ingredient.Name] = codes;

    return mappings;
}
    public Dictionary<string, HashSet<string>> GetNameToCodeMappingForLoader(IWorldAccessor world)
        {
    return GetNameToCodeMapping(world);
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
            writer.Write(RecipeId);
            base.ToBytes(writer);
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
            writer.Write(Pattern?.Length ?? 0);
            if (Pattern != null)
            {
                for (int y = 0; y < Pattern.Length; y++)
                {
                    writer.Write(Pattern[y].Length);
                    for (int z = 0; z < Pattern[y].Length; z++)
                    {
                        writer.Write(Pattern[y][z]);
                    }
                }
            }
        }

        public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            RecipeId = reader.ReadInt32();
            base.FromBytes(reader, resolver);
            Ingredient = new CraftingRecipeIngredient();
            Ingredient.FromBytes(reader, resolver);
            Ingredient.Resolve(resolver, "DoughForming FromBytes");
            Output = new JsonItemStack();
            Output.FromBytes(reader, resolver.ClassRegistry);
            Output.Resolve(resolver, "DoughForming FromBytes");
            int layerCount = reader.ReadInt32();
            if (layerCount > 0)
            {
                Pattern = new string[layerCount][];
                for (int y = 0; y < layerCount; y++)
                {
                    int rowCount = reader.ReadInt32();
                    Pattern[y] = new string[rowCount];
                    for (int z = 0; z < rowCount; z++)
                    {
                        Pattern[y][z] = reader.ReadString();
                    }
                }
            }
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
                int width = Pattern[0].Length;
                int length = Pattern[0][0].Length;
                int startX = (16 - width) / 2;
                int startZ = (16 - length) / 2;
                for (int y = 0; y < Pattern.Length; y++)
                {
                    string[] rows = Pattern[y];
                    for (int z = 0; z < rows.Length; z++)
                    {
                        string row = rows[z];
                        for (int x = 0; x < row.Length; x++)
                        {
                            voxels[x + startX, y, z + startZ] = row[x] != '_' && row[x] != ' ';
                        }
                    }
                }
                return voxels;
            }
        }
    }
}
