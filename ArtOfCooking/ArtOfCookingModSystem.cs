using ArtOfCooking.BlockBehaviors;
using ArtOfCooking.BlockEntities;
using ArtOfCooking.Blocks;
using ArtOfCooking.Items;
using ArtOfCooking.Systems;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ArtOfCooking;
public class ArtOfCooking : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        
        api.RegisterItemClass("AOCItemDough", typeof(AOCItemDough));
        api.RegisterBlockClass("BlockDoughForm", typeof(BlockDoughForm));
        api.RegisterBlockEntityClass("DoughForm", typeof(BlockEntityDoughForm));

        api.RegisterItemClass("AOCItemLavash", typeof(AOCItemLavash));
        api.RegisterBlockClass("AOCBlockShawarma", typeof(AOCBlockShawarma));
        api.RegisterBlockEntityClass("AOCBEShawarma", typeof(AOCBEShawarma));
        
        api.RegisterBlockClass("AOCBlockSpoon", typeof(AOCBlockSpoon));
        api.RegisterBlockClass("AOCBlockEmptySpoon", typeof(AOCBlockEmptySpoon));
        api.RegisterBlockEntityClass("AOCBlockEntitySpoon", typeof(AOCBlockEntitySpoon));
        
        api.RegisterBlockBehaviorClass("AOCTable", typeof(AOCBlockBehaviorTable));
        
        api.RegisterItemClass("AOCItemFood", typeof(AOCItemFood));
        api.RegisterItemClass("AOCItemEgg", typeof(AOCItemEgg));
        
        api.RegisterItemClass("AOCItemRollingPin", typeof(AOCItemRollingPin));
    }
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        api.GetCookingRecipes().ForEach(recipe =>
        {
            var code = recipe?.Code;
            var isFromAoc = code is not null && (code.StartsWith("aoc") || code.Contains("-aoc"));
            if (isFromAoc && !CookingRecipe.NamingRegistry.ContainsKey(code))
            {
                CookingRecipe.NamingRegistry[code] = new AOCRecipeNames();
            }
        });
    }
}
