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
    /// <summary>
    /// When command is called, a poll is built and outputted, and an attendance report is built based on reactions. Report is then sent to teacher via DM
    /// </summary>
    class AttendanceCommand : BaseCommandModule
    {
        [Command("attendance")]
        public async Task Poll(CommandContext ctx) // Takes in all information about the command
        {
            List<Student> allStudents = new List<Student>();
            int year;
            int section;
            string reportFile = string.Format("../../../../../AttendanceReport-{0}.csv", DateTime.Now.ToString("MM-dd"));

            TimeSpan duration = new TimeSpan(0, 0, 5); // How long the poll remains active for   *needs to be changed (5 seconds was used for testing) 

            var interactivity = ctx.Client.GetInteractivity();

            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Attendance Poll"
            };

            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);

            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":raised_hand:")).ConfigureAwait(false); // Defines the emojis to be used in the poll

            var pollResults = await interactivity.CollectReactionsAsync(pollMessage, duration).ConfigureAwait(false); // Collect poll reactions

            var results = pollResults.Select(x => $"{x.Users.ToArray()[0]}");

            string attendanceInfo = string.Join("\n", results); // Makes a list of the info of the students who answered the poll

            string[] studentInfo = attendanceInfo.Split("\n"); // Seperates the list into individual lines and stores them

            await GetStudents(ctx, allStudents); // Get all students in the server

            await GetPresentStudents(ctx, allStudents, studentInfo, out year, out section); // Get students who reacted to the poll and fine current year and section

            await GenerateAttendanceReport(allStudents, reportFile, year, section);

            await ctx.Member.SendFileAsync(reportFile); // Sends attedance report file to teacher via DM after it's made

            await MessageAbsentStudents(ctx, allStudents, year, section);
        }


        /// <summary>
        /// Adds all of the students in the server to the List of Students
        /// </summary>
        static async Task GetStudents(CommandContext ctx, List<Student> allStudents)
        {
            var allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            string membersInfo = string.Join("\n", allMembers); // Makes a list of all server members
            string[] memberInfo = membersInfo.Split("\n"); // Seperates the list into individual lines and stores them

            string[] split;
            string memberNickName;
            string memberYrRaw = string.Empty;
            string memberSectRaw = string.Empty;
            ulong memberID;
            int memberYear;
            int memberSection;

            for (int i = 0; i < memberInfo.Length; i++)
            {
                if (allMembers.ToArray()[i].IsBot == false)
                {
                    // Get nickname from string member info
                    split = memberInfo[i].Split("(");
                    memberNickName = split[1].Substring(0, split[1].Length - 1);

                    // Get member ID from split string of member info
                    memberID = Convert.ToUInt64(split[0].Substring(7, 18));

                    for (int j = 0; j < allMembers.ToArray()[i].Roles.ToArray().Length; j++)
                    {
                        // Get member year from roles
                        if (allMembers.ToArray()[i].Roles.ToArray()[j].ToString().Contains("Year"))
                        {
                            split = allMembers.ToArray()[i].Roles.ToArray()[j].ToString().Split("; ");
                            memberYrRaw = split[1];
                        }

                        // Get member section from roles
                        if (allMembers.ToArray()[i].Roles.ToArray()[j].ToString().Contains("Section"))
                        {
                            split = allMembers.ToArray()[i].Roles.ToArray()[j].ToString().Split("; ");
                            memberSectRaw = split[1];
                        }
                    }

                    // Get year as an integer from the string role
                    if (memberYrRaw == "First Year")
                        memberYear = 1;
                    else if (memberYrRaw == "Second Year")
                        memberYear = 2;
                    else if (memberYrRaw == "Third Year")
                        memberYear = 3;
                    else
                        memberYear = 0;

                    // Get section as an integer from string role
                    if (memberSectRaw == "Section One")
                        memberSection = 1;
                    else if (memberSectRaw == "Section Two")
                        memberSection = 2;
                    else
                        memberSection = 0;

                    if (memberYear != 0 && memberSection != 0) // Only adds students with valid years and sections
                        allStudents.Add(new Student(memberNickName, memberYear, memberSection, memberID));
                }
            }
        }

        /// <summary>
        /// Finds present students who reacted to the poll and adds 1 to their times present, then finds the current year and section
        /// </summary>
        static Task GetPresentStudents(CommandContext ctx, List<Student> allStudents, string[] studentInfo, out int year, out int section)
        {
            string[] split;
            ulong studentID;
            int[] years = new int[3];
            int[] sections = new int[2];

            // Get present students 
            for (int i = 0; i < studentInfo.Length; i++)
            {
                split = studentInfo[i].Split("(");

                studentID = Convert.ToUInt64(split[0].Substring(7, 18));

                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (studentID == allStudents[j].IdNum)
                    {
                        allStudents[j].TimesPresent++; 
                        years[allStudents[j].Year - 1]++;
                        sections[allStudents[j].Section - 1]++;
                    }
                }
            }

            // Get class section
            int highestValue = sections[0];
            section = 0;
            for (int i = 1; i < sections.Length; i++)
            {
                if (sections[i] > highestValue)
                {
                    section = i;
                    highestValue = sections[i];
                }
            }

            // Get class year
            highestValue = years[0];
            year = 0;
            for (int i = 1; i < years.Length; i++)
            {
                if (years[i] > highestValue)
                {
                    year = i;
                    highestValue = years[i];
                }
            }

            section++;
            year++;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Builds and saves an attendance report consisting of present students from both class sections, and all absent students from the current section
        /// </summary>
        public Task GenerateAttendanceReport(List<Student> allStudents, string reportFile, int year, int section)
        {
            int i;
            StringBuilder report = new StringBuilder();
            StringBuilder outOfSectionStudents = null;
            StringBuilder absentStudents = null;

            report.Append(string.Format("Year {0} Section {1} - {2}\nPresent Students\n\n", year, section, DateTime.Now.ToString("MM-dd")));

            // Builder different parts of report
            for (i = 0; i < allStudents.Count; i++)
            {
                if (allStudents[i].TimesPresent > 0)
                {
                    if (allStudents[i].Section == section)
                        report.Append(string.Format("{0}\n", allStudents[i].NickName));
                    else
                    {
                        if (outOfSectionStudents == null)
                            outOfSectionStudents = new StringBuilder();

                        outOfSectionStudents.Append(string.Format("{0}\n", allStudents[i].NickName));
                    }
                }
                else if (allStudents[i].TimesPresent == 0 && allStudents[i].Section == section)
                {
                    if (absentStudents == null)
                        absentStudents = new StringBuilder();

                    absentStudents.Append(string.Format("{0}\n", allStudents[i].NickName));
                }
            }
            
            // Add present out of section students to report
            report.Append("\nPresent Out Of Section Students\n\n");
            if (outOfSectionStudents != null)
                report.Append(outOfSectionStudents);
            else
                report.Append("n/a\n");

            // Add absent students to report
            report.Append("\nAbsent Students\n\n");
            if (absentStudents != null)
                report.Append(absentStudents);
            else
                report.Append("n/a\n");


            // Write the report
            StreamWriter sr = new StreamWriter(reportFile, true);
            sr.Write(Convert.ToString(report));
            sr.Close();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Auto-sends a message via DM telling students that their were absent from class
        /// </summary>
        static async Task MessageAbsentStudents(CommandContext ctx, List<Student> allStudents, int year, int section)
        {
            var allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            string membersInfo = string.Join("\n", allMembers); // Makes a list of all server members
            string[] memberInfo = membersInfo.Split("\n"); // Seperates the list into individual lines and stores them
            string[] split;
            ulong memberID;

            for (int i = 0; i < memberInfo.Length; i++)
            {
                split = memberInfo[i].Split("(");

                memberID = Convert.ToUInt64(split[0].Substring(7, 18));

                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (memberID == allStudents[j].IdNum && allStudents[j].Section == section && allStudents[j].Year == year && allStudents[j].TimesPresent == 0) // Finds absent students from current section
                    {
                        await allMembers.ToArray()[i].SendMessageAsync(string.Format("You were absent from {0} on {1}", ctx.Guild.Name, DateTime.Now.ToString("MM-dd"))); 
                    }
                }
            }
        }
    }
}