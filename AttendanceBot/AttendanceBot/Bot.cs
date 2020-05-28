/*
    Author: Jordan McIntyre, Fabian Dimitrov, Brent Pereira
    Latest Update: May 27th, 2020
    Description: This program contains all methods related to the bot creation and configuration
*/

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
        #region Properties
        public DiscordClient Client { get; private set; } // Represents the bot itself
        public InteractivityExtension Interactivity { get; private set; } // Allows interactivity (i.e. polls) to be set
        public CommandsNextExtension Commands { get; private set; } // Allows for commands to be set
        #endregion

        /// <summary>
        /// Runs the bot.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            #region Bot Configuration

            // Load in the JSON configuration (token & prefix)
            var json = string.Empty;

            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            // Configures the bot 
            var config = new DiscordConfiguration()
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true, // Reconnects automatically if the bot turns off
                LogLevel = LogLevel.Debug, // Gets all logs rather than just errors
                UseInternalLogHandler = true
            };

            Client = new DiscordClient(config);
            #endregion

            #region Enable Interactivity

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(5)
            });
            #endregion

            #region Commands Configuration

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix }, // Prefix used to communicate with bot
                EnableMentionPrefix = true, // Allows bot to be communicated with by mentioning it
                EnableDefaultHelp = false // Disables the default help command
            };
            Commands = Client.UseCommandsNext(commandsConfig); // Automatically handles commands

            // Enables user-created commands
            Commands.RegisterCommands<AttendanceCommand>();
            Commands.RegisterCommands<HelpCommand>();
            #endregion

            await Client.ConnectAsync(); // Connects the bot
            await Task.Delay(-1); // Stops the bot from quitting early 
        }
    }
}