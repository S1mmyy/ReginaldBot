namespace ReginaldBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.Interactions;
    using Discord.WebSocket;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
    {
        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();
        public async Task MainAsync()
        {
            var config = BotConfiguration.GetBotConfiguration();
            var database = new ReginaldBotContext().GetDatabase();
            var services = new ServiceCollection()
                    .AddSingleton(config)
                    .AddSingleton(database)
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                    .AddSingleton<InteractionHandler>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<EventHandler>()
                    .BuildServiceProvider();

            await RunAsync(services);
        }
        private readonly DiscordSocketConfig _socketConfig = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages,
            AlwaysDownloadUsers = true,
            AlwaysDownloadDefaultStickers = false,
            DefaultRetryMode = RetryMode.AlwaysRetry,
        };
        public async Task RunAsync(IServiceProvider _services)
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;

            _services.GetRequiredService<EventHandler>().Initialize();
            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>().InitializeAsync();

            // Bot token can be provided from the BotConfiguration object we set up earlier
            await client.LoginAsync(TokenType.Bot, _services.GetRequiredService<BotConfiguration>().BotToken);
            await client.StartAsync();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }
        private Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
        public static void Log(string service, string text)
        {
            Console.WriteLine(DateTime.UtcNow.ToString("T") + " " + service.PadRight(20) + text);
        }
        public static void Log(string service, string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.UtcNow.ToString("T") + " " + service.PadRight(20) + text);
            Console.ResetColor();
        }
        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}
