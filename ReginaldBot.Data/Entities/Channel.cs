namespace ReginaldBot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class Channel
    {
        public Channel()
        {
            SpawnItems = new ConcurrentBag<SpawnItem>();
        }
        public ulong ChannelId { get; set; }
        public ConcurrentBag<SpawnItem> SpawnItems { get; set; }
    }
}
