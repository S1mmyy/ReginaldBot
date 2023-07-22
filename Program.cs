using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Discord;
using Discord.Net;
using Discord.WebSocket;

using Serilog;
using Serilog.Events;

namespace ReginaldBot
{
	public class Program
	{
		private DiscordSocketClient client;
		private DateTime lastPostDate, nextPostDate = new DateTime();
		private Dictionary<ulong, ulong> guildSettings = new Dictionary<ulong, ulong>();
		private const string imgLink = "https://i.kym-cdn.com/photos/images/newsfeed/001/455/239/daa.jpg";
		private Timer postTimer;
		private bool timerEventBoundToMethod = false;

		public static Task Main() => new Program().MainAsync();

		// Starts program into an async context
		public async Task MainAsync()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateLogger();

			var token = Environment.GetEnvironmentVariable("DiscordToken");
			postTimer = new Timer() { AutoReset = false };

			DiscordSocketConfig config = new DiscordSocketConfig()
			{
				UseInteractionSnowflakeDate = false,
				GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildScheduledEvents & ~GatewayIntents.GuildInvites,
			};
			client = new DiscordSocketClient(config);

			client.Log += LogAsync;

			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			client.Ready += ClientIsReady;
			client.SlashCommandExecuted += SlashCommandHandler;
			client.JoinedGuild += JoinedServer;
			client.LeftGuild += LeftServer;

			// Block this task until the program is closed
			await Task.Delay(-1);
		}

		private static async Task LogAsync(LogMessage message)
		{
			var severity = message.Severity switch
			{
				LogSeverity.Critical => LogEventLevel.Fatal,
				LogSeverity.Error => LogEventLevel.Error,
				LogSeverity.Warning => LogEventLevel.Warning,
				LogSeverity.Info => LogEventLevel.Information,
				LogSeverity.Verbose => LogEventLevel.Verbose,
				LogSeverity.Debug => LogEventLevel.Debug,
				_ => LogEventLevel.Information
			};
			Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
			await Task.CompletedTask;
		}
		
		private async Task JoinedServer(SocketGuild newGuild)
		{
			// Appear in the welcome channel by default
			guildSettings.Add(newGuild.Id, newGuild.DefaultChannel.Id);
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Joined server: {newGuild.Name}"));
			WriteSettings();
		}

		private async Task LeftServer(SocketGuild guildLeft)
		{
			guildSettings.Remove(guildLeft.Id);
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Left server: {guildLeft.Name}"));
			WriteSettings();
		}

		// Things that happen on startup
		private async Task ClientIsReady()
		{
			ReadSettingsAndDates();
			await StartupTasks();
			// await BuildSlashCommands();
			client.Connected += ClientConnected;
		}

		// Doesn't run on initial connection
		private async Task ClientConnected()
		{
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", string.Format("{0:%d} days {0:%h} hours and {0:%m} minutes left until posting", nextPostDate - DateTime.Now)));
		}

		private async Task BuildSlashCommands()
		{
			var globalCommandSetChannel = new SlashCommandBuilder()
				.WithName("choose-channel")
				.WithDescription("Choose what channel Reginald will appear in.")
				.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel you want Reginald to appear in",
					isRequired: true, channelTypes: new List<ChannelType> { 0 }) // Only show text channels as options
				.WithDefaultMemberPermissions(GuildPermission.Administrator);
			var globalCommandGetChannel = new SlashCommandBuilder()
				.WithName("wheres-reginald")
				.WithDescription("Tells you where and when Reginald will next appear.");
			var globalCommandAppear = new SlashCommandBuilder()
				.WithName("appear")
				.WithDescription("Makes Reginald appear in his channel.")
				.WithDefaultMemberPermissions(GuildPermission.Administrator);

			try
			{
				await client.CreateGlobalApplicationCommandAsync(globalCommandSetChannel.Build());
				await client.CreateGlobalApplicationCommandAsync(globalCommandGetChannel.Build());
				await client.CreateGlobalApplicationCommandAsync(globalCommandAppear.Build());
			}
			catch (HttpException e)
			{
				await LogAsync(new LogMessage(LogSeverity.Error, "Reginald", e.Message));
			}
		}

		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			try
			{
				switch (command.Data.Name)
				{
					case "choose-channel":
						await HandleChannelChoiceCommand(command);
						break;
					case "wheres-reginald":
						await HandleGetChannelCommand(command);
						break;
					case "appear":
						await HandleAppearCommand(command);
						break;
				}
			}
			catch (HttpException e)
			{
				await LogAsync(new LogMessage(LogSeverity.Error, "Reginald", e.Message));
			}
		}

		private async Task HandleChannelChoiceCommand(SocketSlashCommand command)
		{
			SocketGuildChannel newChannelChosen = (SocketGuildChannel)command.Data.Options.First().Value;
			if (!guildSettings.ContainsKey(newChannelChosen.Guild.Id))
				guildSettings.Add(newChannelChosen.Guild.Id, newChannelChosen.Id);
			else
				guildSettings[newChannelChosen.Guild.Id] = newChannelChosen.Id;
			WriteSettings();
			await command.RespondAsync($"Reginald will now appear in <#{newChannelChosen.Id}>");
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"{newChannelChosen.Guild.Name} told Reginald to appear in #{newChannelChosen.Name}"));
		}

		private async Task HandleGetChannelCommand(SocketSlashCommand command)
		{
			ulong currentGuildChannelSetting = guildSettings[command.GuildId.Value];
			await command.RespondAsync($"Reginald will next appear in <#{currentGuildChannelSetting}> on {nextPostDate:MM/dd/yyyy}", ephemeral: true);
		}

		private async Task HandleAppearCommand(SocketSlashCommand command)
		{
			ulong currentGuildChannelSetting = guildSettings[command.GuildId.Value];
			await command.RespondAsync($"Made Reginald appear in <#{currentGuildChannelSetting}>", ephemeral: true);
			var appearChannel = client.GetChannel(currentGuildChannelSetting) as ITextChannel;
			await appearChannel.SendMessageAsync(imgLink);
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Reginald forced to appear in #{appearChannel} in {client.GetGuild(command.GuildId.Value).Name}"));
		}

		private async Task StartupTasks()
		{
			if (DateTime.Now.Hour >= nextPostDate.Hour && DateTime.Now.Day == nextPostDate.Day && DateTime.Now.Month == nextPostDate.Month && DateTime.Now.Year == nextPostDate.Year)
			{
				await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", "TODAY\'S THE DAY!!!!!"));
				await AppearInAllServers();
			}
			else if (DateTime.Now > nextPostDate)
			{
				while (DateTime.Now > nextPostDate)
				{
					nextPostDate = nextPostDate.AddDays(14);
				}
				WriteDates();
			}

			if (!timerEventBoundToMethod)
			{
				postTimer.Elapsed += OnPostTimerEnd;
				timerEventBoundToMethod = true;
				ResetTimer();
			}

			if (client.Guilds.Count != guildSettings.Count)
			{
				UpdateGuildSettingsAtStartup();
			}
		}

		private async void OnPostTimerEnd(object source, ElapsedEventArgs e)
		{
			await AppearInAllServers();
		}

		private async void ResetTimer()
		{
			postTimer.Stop();
			postTimer.Interval = (nextPostDate - DateTime.Now).TotalMilliseconds;
			postTimer.Start();
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", string.Format("{0:%d} days {0:%h} hours and {0:%m} minutes left until posting", nextPostDate - DateTime.Now)));
		}

		private async Task AppearInAllServers()
		{
			ITextChannel currentGuildChannel;
			foreach (ulong guildChannelSettingId in guildSettings.Values)
			{
				currentGuildChannel = client.GetChannel(guildChannelSettingId) as ITextChannel;
				try
				{
					await currentGuildChannel.SendMessageAsync(imgLink);
					await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Posted in #{currentGuildChannel} in {currentGuildChannel.Guild.Name}"));
				}
				catch (Exception e)
				{
					await LogAsync(new LogMessage(LogSeverity.Error, "Reginald", e.Message.ToString()));
					await LogAsync(new LogMessage(LogSeverity.Error, "Reginald", $"Error attempting to post in channel #{currentGuildChannel} with ID of {guildChannelSettingId}"));
				}
			}
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Finished appearing everywhere at {DateTime.Now}"));
			SetNewDates();
		}

		private void SetNewDates()
		{
			lastPostDate = DateTime.Now;
			nextPostDate = nextPostDate.AddDays(14);
			WriteDates();
			ResetTimer();
		}

		private async void ReadSettingsAndDates()
		{
			string json = File.ReadAllText("guild_channel_settings.json");
			guildSettings = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(json);

			using (StreamReader sr = File.OpenText("post_dates.txt"))
			{
				lastPostDate = DateTime.Parse(sr.ReadLine());
				nextPostDate = DateTime.Parse(sr.ReadLine());
			}
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", "Saved dates read in"));
		}

		private void WriteSettings()
		{
			string json = JsonConvert.SerializeObject(guildSettings, Formatting.Indented);
			File.WriteAllText("guild_channel_settings.json", json);
		}

		private async void WriteDates()
		{
			using (StreamWriter sw = File.CreateText("post_dates.txt"))
			{
				sw.WriteLine(lastPostDate);
				sw.WriteLine(nextPostDate);
			}
			await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", "New dates saved to file"));
		}

		private async void UpdateGuildSettingsAtStartup()
		{
			// Check for servers Reginald has left while offline
			foreach (ulong guildIdFromSettings in guildSettings.Keys)
			{
				if (!client.Guilds.Contains(client.GetGuild(guildIdFromSettings)))
				{
					guildSettings.Remove(guildIdFromSettings);
					await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Left server while offline: {client.GetGuild(guildIdFromSettings).Name}"));
				}
			}

			// Check for servers Reginald has joined while offline
			foreach (SocketGuild currJoinedGuild in client.Guilds)
			{
				if (!guildSettings.ContainsKey(currJoinedGuild.Id))
				{
					guildSettings.Add(currJoinedGuild.Id, currJoinedGuild.DefaultChannel.Id);
					await LogAsync(new LogMessage(LogSeverity.Info, "Reginald", $"Joined server while offline: {currJoinedGuild.Name}"));
				}
			}
			WriteSettings();
		}
	}
}