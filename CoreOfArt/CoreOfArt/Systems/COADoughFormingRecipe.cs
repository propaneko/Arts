using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CoreOfArts.Systems
{
    public class COADoughFormingRecipe : LayeredVoxelRecipe, IByteSerializable
    {
        public override int QuantityLayers => 16;
        public override string RecipeCategoryCode => "dough forming";


        /// <summary>
        /// Creates a deep copy
        /// </summary>
        /// <returns></returns>
        public override COADoughFormingRecipe Clone()
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

        public void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);
        }

        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            base.FromBytes(reader, resolver);
        }
    }
}
