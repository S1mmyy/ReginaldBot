namespace ReginaldBot
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class ReginaldBotContext
    {
        private readonly string _dbPath;
        public ConcurrentBag<User> Users { get; set; }
        public ConcurrentBag<Guild> Guilds { get; set; }

        public User GetUser(ulong id)
        {
            var user = Users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                user = new User()
                {
                    Id = id,
                };
                Users.Add(user);
            }
            return user;
        }
        public Guild GetGuild(ulong id)
        {
            var guild = Guilds.FirstOrDefault(x => x.Id == id);
            if (guild == null)
            {
                guild = new Guild()
                {
                    Id = id,
                    
                };
                Guilds.Add(guild);
            }
            return guild;
        }
        public ReginaldBotContext(string path = "startup")
        {
            if (string.IsNullOrWhiteSpace(path) || path.Equals("startup"))
                _dbPath = $"{Directory.GetCurrentDirectory()}\\reginald_Db.json";
            else _dbPath = path;

            Users = new ConcurrentBag<User>();
            Guilds = new ConcurrentBag<Guild>();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
            };

            if (!File.Exists(_dbPath))
                SaveChanges();

        }
        public ReginaldBotContext GetDatabase()
        {
            var content = File.ReadAllText(_dbPath);
            var json = JsonConvert.DeserializeObject<ReginaldBotContext>(content);
            return json;
        }
        public void SaveChanges()
        {
            var content = JsonConvert.SerializeObject(this);
            File.WriteAllText(_dbPath, content);
        }
    }
}
