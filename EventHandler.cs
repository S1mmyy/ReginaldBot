namespace ReginaldBot
{
    using Discord.WebSocket;
    using Discord;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    public class EventHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly BotConfiguration _configuration;
        private readonly ReginaldBotContext _dbContext;

        public EventHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _configuration = services.GetRequiredService<BotConfiguration>();
            _dbContext = services.GetRequiredService<ReginaldBotContext>();
            _services = services;
        }
        public void Initialize()
        {
            _client.Ready += _client_Ready;
        }

        private async Task _client_Ready()
        {
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            await _client.SetGameAsync(_configuration.BotStatus, "", ActivityType.Playing);

            await RunSpawwners();
        }
        public Task RunSpawwners()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var guilds = _dbContext.Guilds.Where(a => a.SpawnChannel != 0 && a.SpawnAt < DateTime.UtcNow);
                    foreach (var g in guilds)
                    {
                        try
                        {
                            var channel = _client.GetGuild(g.Id).GetTextChannel(g.SpawnChannel);
                            await channel.SendMessageAsync(_configuration.ImageUrl);
                        }
                        catch
                        {
                            g.SpawnChannel = 0;
                        }
                        finally
                        {
                            g.SpawnAt = DateTime.UtcNow.AddSeconds(_configuration.SpawnTime);
                        }
                    }
                    _dbContext.SaveChanges();
                    await Task.Delay(TimeSpan.FromSeconds(_configuration.CheckSpawnerTime));
                }

            });
            return Task.CompletedTask;
        }
    }
}
