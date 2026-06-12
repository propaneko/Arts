using Newtonsoft.Json;

namespace CoreOfArts.Config
{
    public class COAConfig
    {
        [JsonProperty]
        public bool EnableLiquidMixing { get; set; } = true;
    }
}
