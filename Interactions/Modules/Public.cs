namespace ReginaldBot.Interactions
{
    using Discord;
    using Discord.Interactions;
    using Discord.Rest;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class PublicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public ReginaldBotContext DbContext { get; set; }
        public BotConfiguration BotConfiguration { get; set; }

        [SlashCommand("appear", "Makes Reginald appear in his channel.")]
        public async Task appear()
        {
            await DeferAsync();
            await FollowupAsync(BotConfiguration.ImageUrl);
        }
        [SlashCommand("details", "display details about Reginald")]
        public async Task details()
        {
            await DeferAsync();

            var guild = DbContext.GetGuild(Context.Guild.Id);
            //var spawnItems = guild.SpawnItems.ToList();

            //if (spawnItems == null || spawnItems.Count == 0)
            //{
            //    await FollowupAsync($"Reginald won't appears.");
            //    return;
            //}
            if (guild.SpawnChannel == 0)
            {
                await FollowupAsync($"Reginald won't appears, choose a channel first.");
                return;
            }
            //await FollowupAsync($"Reginald appears {spawnItems.Count} times in <#{guild.SpawnChannel}>.");

            await FollowupAsync($"Reginald appears in <#{guild.SpawnChannel}>.");
        }
        [Group("spawn", "spawn commands")]
        public class SpawnCommands : InteractionModuleBase<SocketInteractionContext>
        {
            public ReginaldBotContext DbContext { get; set; }
            public BotConfiguration BotConfiguration { get; set; }


            [SlashCommand("now", "Makes Reginald appear in his channel.")]
            public async Task now()
            {
                await DeferAsync();
                await FollowupAsync(BotConfiguration.ImageUrl);
            }

            [SlashCommand("time", "change spawn time.")]
            public async Task time(TimeType time, int value)
            {
                await DeferAsync();
               
                var totalSeconds = value * (int)time;

                if(totalSeconds < BotConfiguration.CheckSpawnerTime)
                {
                    await FollowupAsync($"Value can not be earlier than {DateTime.UtcNow.AddSeconds(BotConfiguration.CheckSpawnerTime).ToDiscordUnixTimestampFormat()}.");
                    return;
                }

                var datetime = DateTime.UtcNow.AddSeconds(totalSeconds);

                var guild = DbContext.GetGuild(Context.Guild.Id);
                guild.SpawnAt = datetime;

                DbContext.SaveChanges();

                await FollowupAsync($"Reginald will now appear {datetime.ToDiscordUnixTimestampFormat()}.");
            }
            public enum TimeType
            {
                None = 0,
                Secound = 1,
                Minute = 60,
                Hour = 3600,
                Day = 86400,
                Week = 604800,
                Month = 2419200,
                Year = 29030400
            }
        }
        [Group("channel", "channel commands")]
        public class ChannelCommands : InteractionModuleBase<SocketInteractionContext>
        {
            public ReginaldBotContext DbContext { get; set; }

            [SlashCommand("choose", "Choose what channel Reginald will appear in.")]
            public async Task choose(ITextChannel textChannel)
            {
                await DeferAsync();

                var guild = DbContext.GetGuild(Context.Guild.Id);
                guild.SpawnChannel = textChannel.Id;

                DbContext.SaveChanges();

                await FollowupAsync($"Reginald will now appear in <#{textChannel.Id}>.");
            }
        }
    }
}
