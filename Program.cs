using System;
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
		// LINK TO SEND https://i.kym-cdn.com/photos/images/newsfeed/001/455/239/daa.jpg

		private DiscordSocketClient _client;
		private ulong[][] GuildChannels;

		public static Task Main(string[] args)
		{
			return new Program().MainAsync();
		}

		// Starts program into an async context
		public async Task MainAsync()
		{
			var token = Environment.GetEnvironmentVariable("DiscordToken");

			// Create the actual client
			_client = new DiscordSocketClient();
			_client.Log += Log;

			// Log the client in and connect to Discord
			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			// Listening to slash command events
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

		private async Task JoinedServer(SocketGuild newGuild)
		{
			/**
			foreach (SocketTextChannel guildChannel in newGuild.TextChannels.ToArray())
			{
				if (guildChannel.PermissionOverwrites.
			}
			**/
		}

		// Build slash commands
		private async Task Client_Ready()
		{
			var globalCommand = new SlashCommandBuilder()
				.WithName("choose-channel")
				.WithDescription("Choose what channel Reginald will appear in.")
				.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel you want Reginald to appear in", isRequired: true);

			try
			{
				await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
			}
			catch(HttpException exception)
			{
				var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
				Console.WriteLine(json);
			}
		}

		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			switch(command.Data.Name)
			{
				case "choose-channel":
					await HandleChannelChoiceCommand(command);
					break;
			}
		}

		private async Task HandleChannelChoiceCommand(SocketSlashCommand command)
		{
			SocketGuildChannel newChannelChosen = (SocketGuildChannel) command.Data.Options.First().Value;
			await command.RespondAsync($"Reginald will now appear in <#{newChannelChosen.Id}>");
		}
	}
}
