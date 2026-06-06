using Vintagestory.API.Common;
using ArtsXSlills;

namespace ArtsXSkills
{
    public class ArtsXSkillsModSystem : ModSystem
    {        
        public ICoreAPI Api { get; private set; }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ArtsXSkillsItemPlantableSeed", typeof(ArtsXSkillsItemPlantableSeed));
        }
        
    }
}
