using Newtonsoft.Json;

namespace Economics
{
    public class News
    {
        public string Link { get; set; }

        public string Headline { get; set; }

        public string Category { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        public string Authors { get; set; }

        public DateTime Date { get; set; }
    }
}