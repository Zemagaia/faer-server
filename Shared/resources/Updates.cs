using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Shared.resources
{
    public class Update
    {
        public int Id;
        [JsonProperty("Add")] public string AddTime;
        [JsonProperty("Ends")] public string EndTime;
        public string Content;
    }

    public class Updates
    {
        public static List<Update> ReadFile(string fileName)
        {
            using (var r = new StreamReader(fileName))
            {
                return JsonConvert.DeserializeObject<List<Update>>(r.ReadToEnd());
            }
        }

        public List<Update> Load(string path)
        {
            var updates = ReadFile(path);
            return updates;
        }
    }
}