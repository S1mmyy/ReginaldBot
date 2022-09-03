namespace ReginaldBot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class Guild
    {
        public Guild()
        {
            //SpawnItems = new ConcurrentBag<SpawnItem>();
            SpawnAt = DateTime.UtcNow;
        }
        public ulong Id { get; set; }
        //public ConcurrentBag<SpawnItem> SpawnItems { get; set; }
        public ulong SpawnChannel { get; set; }
        public DateTime SpawnAt { get; set; }
        //public ConcurrentBag<Channel> Channels { get; set; }

        //public Channel GetChanenl(ulong Id)
        //{
        //    var channel = Channels.FirstOrDefault(a => a.ChannelId == Id);
        //    if (channel is null)
        //    {
        //        channel = new Channel()
        //        {
        //            ChannelId = Id,
        //            SpawnItems = new ConcurrentBag<SpawnItem>()
        //        };
        //        Channels.Add(channel);
        //    }
        //    return channel;
        //}
    }
}
