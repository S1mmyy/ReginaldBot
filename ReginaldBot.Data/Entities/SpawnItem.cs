namespace ReginaldBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class SpawnItem
    {
        public SpawnItem()
        {
            CreatedAt = DateTime.UtcNow;
        }
        public ulong CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime SpawnAt { get; set; }

    }
}
