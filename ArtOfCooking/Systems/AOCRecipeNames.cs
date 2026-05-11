using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ArtOfCooking.Systems
{
    public class AOCRecipeNames : ICookingRecipeNamingHelper
    {
        public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
        {
            Vintagestory.API.Datastructures.OrderedDictionary<ItemStack, int> quantitiesByStack = new Vintagestory.API.Datastructures.OrderedDictionary<ItemStack, int>();
            quantitiesByStack = mergeStacks(worldForResolve, stacks);

            CookingRecipe recipe = worldForResolve.Api.GetCookingRecipe(recipeCode);

            if (recipeCode == null || recipe == null || quantitiesByStack.Count == 0) return Lang.Get("unknown");

            int max = 1;
            string MealFormat = "meal";
            string topping = string.Empty;
            ItemStack PrimaryIngredient = null;
            ItemStack SecondaryIngredient = null;
            List<string> OtherIngredients = new List<string>();
            List<string> MashedNames = new List<string>();
            List<string> GarnishedNames = new List<string>();
            List<string> grainNames = new List<string>();
            string mainIngredients;
            string everythingelse = "";



            switch (recipeCode)
            {
                case "aocscrambledeggs":
                    {
                        max = 0;

                        foreach (var val in quantitiesByStack)
                        {
                            if (val.Key.Collectible.FirstCodePart() == "eggportion")
                            {
                                PrimaryIngredient = val.Key;
                                max += val.Value;
                                continue;
                            }
                            
                            MashedNames.Add(ingredientName(val.Key, true));
                        }


                        recipeCode = "aocscrambledeggs";
                        break;
                    }
                case "aoccompote":
                    {
                        max = 0;

                        foreach (var val in quantitiesByStack)
                        {
                            max += val.Value;
                            if (val.Key.Collectible.Code.Path.Contains("waterportion")) continue;
                            if (val.Key.Collectible.Code.Path.Contains("compoteportion")) continue;
                            
                            MashedNames.Add(ingredientName(val.Key, true));
                        }


                        recipeCode = "aoccompote";
                        break;
                    }
            }



            switch (max)
            {
                case 3:
                    MealFormat += "-hearty-" + recipeCode;
                    break;
                case 4:
                    MealFormat += "-hefty-" + recipeCode;
                    break;
                default:
                    MealFormat += "-normal-" + recipeCode;
                    break;
            }

            if (topping == "honeyportion")
            {
                MealFormat += "-honey";
            }
            //mealformat is done.  Time to do the main inredients.



            if (SecondaryIngredient != null && recipeCode != "aocscrambledeggs")
            {
                mainIngredients = Lang.Get("multi-main-ingredients-format", getMainIngredientName(PrimaryIngredient, recipeCode), getMainIngredientName(SecondaryIngredient, recipeCode, true));
            }
            else
            {
                mainIngredients = PrimaryIngredient == null ? "" : getMainIngredientName(PrimaryIngredient, recipeCode);
            }


            switch (recipeCode)
            {
                case "aocscrambledeggs":
                    if (MashedNames.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-porridge-mashed", MashedNames);
                    }
                    return Lang.Get(MealFormat, everythingelse).Trim().UcFirst();
                case "aoccompote":
                    if (MashedNames.Count > 0)
                    {
                        everythingelse = getMealAddsString("meal-adds-porridge-mashed", MashedNames);
                    }
                    return Lang.Get(MealFormat, everythingelse).Trim().UcFirst();
            }
            //everything else is done.

            return Lang.Get(MealFormat, mainIngredients, everythingelse).Trim().UcFirst();
        }
        private string ingredientName(ItemStack stack, bool InsturmentalCase = false)
        {
            string code;

            code = stack.Collectible.Code?.Domain + AssetLocation.LocationSeparator + "recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code?.Path;

            if (InsturmentalCase)
                code += "-insturmentalcase";

            if (Lang.HasTranslation(code))
            {
                return Lang.GetMatching(code);
            }

            code = stack.Collectible.Code?.Domain + AssetLocation.LocationSeparator + "recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.FirstCodePart();

            if (InsturmentalCase)
                code += "-insturmentalcase";

            return Lang.GetMatching(code);
        }
        private string getMainIngredientName(ItemStack itemstack, string code, bool secondary = false)
        {
            string t = secondary ? "secondary" : "primary";
            string langcode = $"meal-ingredient-{code}-{t}-{getInternalName(itemstack)}";

            if (Lang.HasTranslation(langcode, true))
            {
                return Lang.GetMatching(langcode);
            }

            langcode = $"meal-ingredient-{code}-{t}-{itemstack.Collectible.FirstCodePart()}";
            return Lang.GetMatching(langcode);
        }
        private string getInternalName(ItemStack itemstack)
        {
            return itemstack.Collectible.Code.Path;
        }

        private string getMealAddsString(string code, List<string> ingredients1, List<string> ingredients2 = null)
        {
            if (ingredients2 == null)
                return Lang.Get(code, Lang.Get($"meal-ingredientlist-{ingredients1.Count}", ingredients1.ToArray()));
            return Lang.Get(code, Lang.Get($"meal-ingredientlist-{ingredients1.Count}", ingredients1.ToArray()), Lang.Get($"meal-ingredientlist-{ingredients2.Count}", ingredients2.ToArray()));
        }
        private Vintagestory.API.Datastructures.OrderedDictionary<ItemStack, int> mergeStacks(IWorldAccessor worldForResolve, ItemStack[] stacks)
        {
            Vintagestory.API.Datastructures.OrderedDictionary<ItemStack, int> dict = new Vintagestory.API.Datastructures.OrderedDictionary<ItemStack, int>();

            List<ItemStack> stackslist = new List<ItemStack>(stacks);
            while (stackslist.Count > 0)
            {
                ItemStack stack = stackslist[0];
                stackslist.RemoveAt(0);
                if (stack == null) continue;

                int cnt = 1;

                while (true)
                {
                    ItemStack foundstack = stackslist.FirstOrDefault((otherstack) => otherstack != null && otherstack.Equals(worldForResolve, stack, GlobalConstants.IgnoredStackAttributes));

                    if (foundstack != null)
                    {
                        stackslist.Remove(foundstack);
                        cnt++;
                        continue;
                    }

                    break;
                }

                dict[stack] = cnt;
            }

            return dict;
        }

    }

}
