using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace ReginaldBot
{
	public class Program
	{
		private DiscordSocketClient _client;
		private Dictionary<ulong, ulong> guildSettings = new Dictionary<ulong, ulong>();
		private const string imgLink = "https://i.kym-cdn.com/photos/images/newsfeed/001/455/239/daa.jpg";

		public static Task Main(string[] args)
		{
			return new Program().MainAsync();
		}

		// Starts program into an async context
		public async Task MainAsync()
		{
			var token = Environment.GetEnvironmentVariable("DiscordToken");
			ReadSettings();

			DiscordSocketConfig config = new DiscordSocketConfig()
			{
				UseInteractionSnowflakeDate = false
			};

			// Create the actual client
			_client = new DiscordSocketClient(config);
			_client.Log += Log;

			// Log the client in and connect to Discord
			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			_client.Ready += Client_Ready;
			_client.SlashCommandExecuted += SlashCommandHandler;
			_client.JoinedGuild += JoinedServer;

			// Block this task until the program is closed
			await Task.Delay(-1);
		}

		// Basic log method using Discord's proprietary LogMessage parameter
		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		// Use the welcome channel as the default
		private Task JoinedServer(SocketGuild newGuild)
		{
			guildSettings.Add(newGuild.Id, newGuild.DefaultChannel.Id);
			return Task.CompletedTask;
		}

		// Build slash commands
		private async Task Client_Ready()
		{
			var guild = _client.GetGuild(1009147201362067566);
			List<ApplicationCommandProperties> appCmdProperties = new List<ApplicationCommandProperties>();

			var globalCommandSetChannel = new SlashCommandBuilder()
				.WithName("choose-channel")
				.WithDescription("Choose what channel Reginald will appear in.")
				.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel you want Reginald to appear in", isRequired: true)
				.WithDefaultMemberPermissions(GuildPermission.Administrator);

			var globalCommandGetChannel = new SlashCommandBuilder()
				.WithName("wheres-reginald")
				.WithDescription("Tells you where Reginald appears.");

			var globalCommandAppear = new SlashCommandBuilder()
				.WithName("appear")
				.WithDescription("Makes Reginald appear in his channel.")
				.WithDefaultMemberPermissions(GuildPermission.Administrator);

			try
			{
				//await _client.CreateGlobalApplicationCommandAsync(globalCommandSetChannel.Build());
				//await _client.CreateGlobalApplicationCommandAsync(globalCommandGetChannel.Build());
				//await _client.CreateGlobalApplicationCommandAsync(globalCommandAppear.Build());

				//await guild.DeleteApplicationCommandsAsync();
			}
			catch (HttpException e)
			{
				var json = JsonConvert.SerializeObject(e.Errors, Formatting.Indented);
				Console.WriteLine(json);
			}
		}

		private async Task SlashCommandHandler(SocketSlashCommand command)
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

		private async Task HandleChannelChoiceCommand(SocketSlashCommand command)
		{
			SocketGuildChannel newChannelChosen = (SocketGuildChannel)command.Data.Options.First().Value;
			if (!guildSettings.ContainsKey(newChannelChosen.Guild.Id))
				guildSettings.Add(newChannelChosen.Guild.Id, newChannelChosen.Id);
			else
				guildSettings[newChannelChosen.Guild.Id] = newChannelChosen.Id;
			UpdateSettings();
			await command.RespondAsync($"Reginald will now appear in <#{newChannelChosen.Id}>");
		}

		private async Task HandleGetChannelCommand(SocketSlashCommand command)
		{
			ulong currentGuildChannelSetting = guildSettings[command.GuildId.Value];
			await command.RespondAsync($"Reginald appears in <#{currentGuildChannelSetting}>", ephemeral: true);
		}

		private async Task HandleAppearCommand(SocketSlashCommand command)
		{
			ulong currentGuildChannelSetting = guildSettings[command.GuildId.Value];
			await command.RespondAsync($"Made Reginald appear in <#{currentGuildChannelSetting}>", ephemeral: true);
			var appearChannel = _client.GetChannel(currentGuildChannelSetting) as ITextChannel;
			await appearChannel.SendMessageAsync(imgLink);
		}

		private void UpdateSettings()
		{
			string json = JsonConvert.SerializeObject(guildSettings, Formatting.Indented);
			File.WriteAllText("guild channel settings.json", json);
		}

		private void ReadSettings()
		{
			string json = File.ReadAllText("guild channel settings.json");
			guildSettings = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(json);
		}
	}
}
