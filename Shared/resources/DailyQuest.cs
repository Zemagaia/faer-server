using System.IO;
using Newtonsoft.Json;

namespace common.resources
{
    public class DailyQuest
    {
        public static QuestData ReadFile(string fileName)
        {
            using (var r = new StreamReader(fileName))
            {
                return JsonConvert.DeserializeObject<QuestData>(r.ReadToEnd());
            }
        }

        public QuestData Load(string path)
        {
            return ReadFile(path);
        }
    }
}