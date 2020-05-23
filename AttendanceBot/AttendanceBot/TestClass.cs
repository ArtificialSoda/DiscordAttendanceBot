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
using System.IO;
using System.Globalization;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using DSharpPlus;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


namespace AttendanceBot
{
    class TestClass : BaseCommandModule
    {
        [Command("channel")]
        public async Task Channel(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var attendanceEmoji = DiscordEmoji.FromName(ctx.Client, ":raised_hand:");

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Check which channel you're in",
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            // Generates poll
            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);
            await pollMessage.CreateReactionAsync(attendanceEmoji).ConfigureAwait(false);

            // Collects reactions from poll
            var reaction = await interactivity.WaitForReactionAsync(x => x.Channel == ctx.Channel).ConfigureAwait(false);

            var chn = ctx.Member?.VoiceState?.Channel;
            var send = await ctx.Channel.SendMessageAsync($"Channel of execution: {ctx.Channel} \n VC: {chn}\n");
        }
    }
}
