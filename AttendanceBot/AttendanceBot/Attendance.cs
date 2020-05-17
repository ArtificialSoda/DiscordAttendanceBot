using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.EventHandling;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceBot
{
    class Attendance : BaseCommandModule
    {
        private List<string> _names = new List<string>();

        [Command("attendance")]
        public async Task Poll(CommandContext ctx) // Takes in all information about the command
        {
            TimeSpan duration = new TimeSpan(0, 0, 10); // How long the poll remains active for

            var interactivity = ctx.Client.GetInteractivity();

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Attendance Poll"
            };

            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);

            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":raised_hand:")).ConfigureAwait(false); // Defines the emojis to be used in the poll

            var result = await interactivity.CollectReactionsAsync(pollMessage, duration).ConfigureAwait(false); // Collect poll reactions

            var results = result.Select(x => $"{x.Users.ToArray()[0]}");

            string attendanceInfo = string.Join("\n", results); // Makes a list of the info of the students who answered the poll

            string[] studentInfo = attendanceInfo.Split("\n"); // Seperates the list into individual lines and stores them

            string[] studentInfoSplit;

            // Extracts just the username of each present student
            foreach (string item in studentInfo)
            {
                studentInfoSplit = item.Substring(27).Split("#");
                _names.Add(studentInfoSplit[0]); // Stores each username in the List<> of names
            }
        }

        public List<string> Names
        {
            get { return _names; }
        }
    }
}
