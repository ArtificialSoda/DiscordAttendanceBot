/*
    Author: Jordan McIntyre
    Latest Update: May 27th, 2020
    Description: This program contains all methods related to the Help Command.
*/

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;


namespace AttendanceBot
{
    /// <summary>
    /// The 'HelpCommand' class contains the necessary methods for the Help Command's functionality.
    /// </summary>
    class HelpCommand : BaseCommandModule
    {
        [Command("help")]
        /// <summary>
        /// The 'Help' task outputs a help message corresponding to a user's message.
        /// The 'Help' task contains one required parameter 'ctx' and one optional parameter 'cmd'
        /// The 'ctx' string is the message context, this is what will trigger the Task to run.
        /// The 'cmd' string is the optional command name that the help command will explain, when left empty it will display all commands.
        /// </summary>
        public async Task Help(CommandContext ctx, string cmd = "")
        {
            DiscordMember user = ctx.Member;
            if (cmd != "")
            {
                if ((new string[] { "help", "end", "attendance" }).Contains(cmd))
                {
                    if (await IsAccessible(user, cmd))
                    {
                        string desc = await GetDescription(cmd);
                        var msg = await HelpMessage(cmd, desc);
                        await ctx.Channel.SendMessageAsync(null, false, msg);
                    }
                    else
                    {
                        var error = await ErrorMessage("You must be a 'teacher' access that command!");
                        await ctx.Channel.SendMessageAsync(null, false, error);
                    }
                }
                else
                {
                    var error = await ErrorMessage("Command not found!");
                    await ctx.Channel.SendMessageAsync(null, false, error);
                }
            }
            else
            {
                var msg = await HelpMessage("attendance, end, help, start", "Please enter one of the following commands to learn more!");
                await ctx.Channel.SendMessageAsync(null, false, msg);
            }
        }

        /// <summary>
        /// The 'HelpMessage' task is responsible for the Message Embedding of a succesful instance of the help command.
        /// The 'HelpMessage' task contains two required parameters 'cmd' and 'desc'.
        /// The 'cmd' string is the command's name that will be used as the Embedded Message's title.
        /// The 'desc' string is the command's description that will be used as the Embedded Message's description.
        /// </summary>
        public async Task<DiscordEmbedBuilder> HelpMessage(string cmd, string desc)
        {
            return new DiscordEmbedBuilder()
            {
                Color = DiscordColor.CornflowerBlue,
                Title = cmd,
                Description = desc
            };
        }

        /// <summary>
        /// The 'ErrorMessage' task is responsible for the Message Embedding of a unsucessful instance of the help command.
        /// The 'ErrorMessage' task contains one required parameter 'error'.
        /// The 'error' string is the description of the triggered error.
        /// </summary>
        public async Task<DiscordEmbedBuilder> ErrorMessage(string error)
        {
            return new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkRed,
                Title = "Error",
                Description = error
            };
        }

        /// <summary>
        /// The 'IsAccessible' task is responsible for determining if a user has the necessary role to use a given command.
        /// The 'IsAccessible' task has two required parameters 'user' and 'cmd'.
        /// The 'user' DiscordMember is the user object who triggered the help command.
        /// The 'cmd' string is the command called by the user.
        /// </summary>
        public async Task<Boolean> IsAccessible(DiscordMember user, string cmd)
        {
            Dictionary<string, string> FuncFamily = new Dictionary<string, string>()
                {
                    {"end", "teacher"},
                    {"attendance", "teacher"},
                };

            if (cmd == "help")
                return true;

            foreach (DiscordRole role in user.Roles)
                if (FuncFamily[cmd] == role.Name.ToLower())
                    return true;
            return false;
        }

        /// <summary>
        /// The 'GetDescription' task is responsible for returning the description of a given command.
        /// The 'GetDescription' task contains one required parameter 'cmd'.
        /// The 'cmd' string is the command called by the user.
        /// </summary>
        public async Task<String> GetDescription(string cmd)
        {
            Dictionary<string, string> descriptions = new Dictionary<string, string>()
                {
                    {"help", "The `help` command has one optional parameter `cmd` which will output either the given command's description, or it will output a description for all commands."},
                    {"end", "The `end` command has no parameters, once called class will end."},
                    {"attendance", "The `attendance` command has four parameters; `class time`, `poll time`, `section number` and `year number`."}
                };

            if (cmd == "")
                return string.Format("{0}\n{1}\n{2}\n{3}", descriptions["attendance"], descriptions["end"], descriptions["info"], descriptions["start"]);
            else
                return descriptions[cmd];
        }
    }
}
