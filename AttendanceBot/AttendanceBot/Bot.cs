using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace AttendanceBot
{
    class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; } // Allows for commands to be set in the bot

        public async Task RunAsync()
        {
            // load in the json config
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration()
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true, // Reconnect automatically if the bot turns off
                LogLevel = LogLevel.Debug, // Get all logs rather than just errors
                UseInternalLogHandler = true
            };

            Client = new DiscordClient(config);

            Client.UseInteractivity(new InteractivityConfiguration { });

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix }, // Prefix used to communicate with bot
                EnableDms = false, // Can only use commands on the server 
                EnableMentionPrefix = true // Allows bot to be communicated with by mentioning it
            };

            Commands = Client.UseCommandsNext(commandsConfig); // Automatically handles commands

            Commands.RegisterCommands<AttendanceCommand>();

            await Client.ConnectAsync(); // Connects the bot

            await Task.Delay(-1); // Stops the bot from quitting early 
        }
    }
}