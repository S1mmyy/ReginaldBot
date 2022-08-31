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
		private DiscordSocketClient client;
		private Dictionary<ulong, ulong> guildSettings = new Dictionary<ulong, ulong>();
		private const string imgLink = "https://i.kym-cdn.com/photos/images/newsfeed/001/455/239/daa.jpg";
		private DateTime lastPostDate, nextPostDate = new DateTime();

		public static Task Main()
		{
			return new Program().MainAsync();
		}

		// Starts program into an async context
		public async Task MainAsync()
		{
			var token = Environment.GetEnvironmentVariable("DiscordToken");

			DiscordSocketConfig config = new DiscordSocketConfig()
			{
				UseInteractionSnowflakeDate = false
			};

			client = new DiscordSocketClient(config);
			client.Log += Log;

			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			client.Ready += ClientReady;
			client.SlashCommandExecuted += SlashCommandHandler;
			client.JoinedGuild += JoinedServer;
			client.LeftGuild += LeftServer;

			// Block this task until the program is closed
			await Task.Delay(-1);
		}

		// Basic log method using Discord's proprietary LogMessage parameter
		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}
		
		private Task JoinedServer(SocketGuild newGuild)
		{
			// Appear in the welcome channel by default
			guildSettings.Add(newGuild.Id, newGuild.DefaultChannel.Id);
			WriteSettings();
			return Task.CompletedTask;
		}

		private Task LeftServer(SocketGuild guildLeft)
		{
			guildSettings.Remove(guildLeft.Id);
			WriteSettings();
			return Task.CompletedTask;
		}

		private async Task ClientReady()
		{
			ReadSettingsAndDates();
			await StartupChecks();

			var globalCommandSetChannel = new SlashCommandBuilder()
				.WithName("choose-channel")
				.WithDescription("Choose what channel Reginald will appear in.")
				.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel you want Reginald to appear in",
					isRequired: true, channelTypes: new List<ChannelType> { 0 }) // Only show text channels as options
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
				await client.CreateGlobalApplicationCommandAsync(globalCommandSetChannel.Build());
				await client.CreateGlobalApplicationCommandAsync(globalCommandGetChannel.Build());
				await client.CreateGlobalApplicationCommandAsync(globalCommandAppear.Build());

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
			WriteSettings();
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
			var appearChannel = client.GetChannel(currentGuildChannelSetting) as ITextChannel;
			await appearChannel.SendMessageAsync(imgLink);
		}

		private async Task StartupChecks()
		{
			if (DateTime.Now.Day == nextPostDate.Day && DateTime.Now.Month == nextPostDate.Month && DateTime.Now.Year == nextPostDate.Year)
			{
				Console.WriteLine("TODAY'S THE DAY!!!");
				await AppearInAllServers();
			}
		}

		private async Task AppearInAllServers()
		{
			ITextChannel currentGuildChannel;
			foreach (ulong guildData in guildSettings.Values)
			{
				currentGuildChannel = client.GetChannel(guildData) as ITextChannel;
				await currentGuildChannel.SendMessageAsync(imgLink);
			}
			ShiftDates();
		}

		private void ShiftDates()
		{
			lastPostDate = DateTime.Now;
			nextPostDate = nextPostDate.AddDays(14);
			WriteDates();
		}

		private void ReadSettingsAndDates()
		{
			string json = File.ReadAllText("guild_channel_settings.json");
			guildSettings = JsonConvert.DeserializeObject<Dictionary<ulong, ulong>>(json);

			using (StreamReader sr = File.OpenText("post_dates.txt"))
			{
				lastPostDate = DateTime.Parse(sr.ReadLine());
				nextPostDate = DateTime.Parse(sr.ReadLine());
			}
			Console.WriteLine("Saved dates read in");
			Console.WriteLine("Last posted at " + lastPostDate);
			Console.WriteLine("Next post is scheduled for " + nextPostDate);
		}

		private void WriteSettings()
		{
			string json = JsonConvert.SerializeObject(guildSettings, Formatting.Indented);
			File.WriteAllText("guild_channel_settings.json", json);
		}

		private void WriteDates()
		{
			using (StreamWriter sw = File.CreateText("post_dates.txt"))
			{
				sw.WriteLine(lastPostDate);
				sw.WriteLine(nextPostDate);
			}
			Console.WriteLine("New dates saved to file");
			Console.WriteLine("Last posted at " + lastPostDate);
			Console.WriteLine("Next post is scheduled for " + nextPostDate);
		}
	}
}