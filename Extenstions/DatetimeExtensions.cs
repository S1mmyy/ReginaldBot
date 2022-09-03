namespace ReginaldBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class DatetimeExtensions
    {
        public static Int32 ToUnixTimestamp(this DateTime datetime)
        {
            return (int)datetime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
        public static string ToDiscordUnixTimestampFormat(this DateTime datetime)
        {
            return $"<t:{datetime.ToUnixTimestamp()}:R>";
        }
    }
}
