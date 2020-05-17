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
        public async Task Poll(CommandContext ctx)
        {
            TimeSpan duration = new TimeSpan(0, 0, 5);

            var interactivity = ctx.Client.GetInteractivity();

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Attendance Poll",
            };

            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);

            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":raised_hand:")).ConfigureAwait(false);

            var result = await interactivity.CollectReactionsAsync(pollMessage, duration).ConfigureAwait(false);

            var results = result.Select(x => $"{x.Users.ToArray()[0]}");

            string attendanceInfo = string.Join("\n", results);

            string[] studentInfo = attendanceInfo.Split("\n");

            string[] studentInfoSplit;

            foreach (string item in studentInfo)
            {
                studentInfoSplit = item.Substring(27).Split("#");
                _names.Add(studentInfoSplit[0]);
            }

            foreach (string item in _names)
            {
                Console.WriteLine(item);
            }
        }
    }
}
