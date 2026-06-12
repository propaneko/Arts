using Newtonsoft.Json;

namespace ArtOfCooking.Config
{
    public class AOCConfig
    {
        [JsonProperty]
        public bool DisableAOCEggs { get; set; } = false;
    }
}
