using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReginaldBot
{
    public class BotConfiguration
    {
        public ulong OwnerId { get; set; }
        public ulong DebugGuildId { get; set; }
        public string BotToken { get; set; }
        public string BotStatus { get; set; }
        public string ImageUrl { get; set; }
        public int SpawnTime { get; set; }
        public int CheckSpawnerTime { get; set; }
        public static BotConfiguration GetBotConfiguration()
        {
            var content = File.ReadAllText(@"config.json");
            var json = JsonConvert.DeserializeObject<BotConfiguration>(content);
            return json;
        }
    }
}
