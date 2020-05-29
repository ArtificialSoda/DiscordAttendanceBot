/*
    Author: Jordan McIntyre, Fabian Dimitrov, Brent Pereira
    Latest Update: May 28th, 2020
    Description: This program contains all methods related to the Attendance Command.
*/

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
using System.Diagnostics;

namespace AttendanceBot
{
    /// <summary>
    /// The 'AttendanceCommand' class contains the necessary methods for the Attendance Command's functionality.
    /// </summary>
    class AttendanceCommand : BaseCommandModule
    {

        [Command("attendance")]
        [Description("Takes periodic class attendance via student reaction to the attendance poll.")]
        public async Task Poll(CommandContext ctx,
                              [Description("OPTIONAL: Duration of class (default: 60m)")] TimeSpan classDuration = default(TimeSpan),
                              [Description("OPTIONAL: Frequency of polls (default: 20m)")] TimeSpan pollFrequency = default(TimeSpan))
        {
            bool b = false;
            DiscordMember user = ctx.Member;
            foreach (DiscordRole role in user.Roles)
                if (role.Name.ToString().ToLower() == "teacher")
                    b = true;
            if (!b)
            {
                var error = await ErrorMessage("You must be a 'teacher' to access this command!");
                await ctx.Channel.SendMessageAsync(null, false, error);
                return;
            }
            List<Student> allStudents = new List<Student>();
            List<ulong> presentStudents = new List<ulong>();

            int year = 0, section = 0;
            string reportFile = string.Format("../../../../../AttendanceReport-{0}.csv", DateTime.Now.ToString("MM-dd"));

            if (classDuration == default)
                classDuration = TimeSpan.FromMinutes(60); //Sets default class duration if it was omitted at command call

            Stopwatch classTime = Stopwatch.StartNew();
            Stopwatch timeSincePoll = Stopwatch.StartNew();

            // Generates a list of all students in the Discord Server 
            await GetStudents(ctx, allStudents);

            bool isFirstPoll = true;
            int numPolls = 0;
            do
            { 
                if (isFirstPoll || timeSincePoll.Elapsed >= pollFrequency)
                {
                    isFirstPoll = false;
                    numPolls++;

                    // Generates the attendance poll
                    timeSincePoll.Restart();
                    await CreatePoll(ctx, pollFrequency, presentStudents);

                    // Sorts students who reacted to the poll by current year and section
                    await SortPresentStudents(ctx, allStudents, presentStudents, out year, out section);

                    presentStudents.Clear();
                }
            }
            while (classTime.Elapsed < classDuration);

            timeSincePoll.Stop();
            classTime.Stop();

            // Generates the attendance report (CSV format)
            await GenerateAttendanceReport(ctx, allStudents, reportFile, year, section, numPolls);

            // Sends attedance report file to teacher via DM after it's made
            await ctx.Member.SendFileAsync(reportFile);

            // Sends message via DM to students who were absent 
            await MessageAbsentStudents(ctx, allStudents, year, section);
        }

        #region Method - CreatePoll
        /// <summary>
        /// Generates the attendance poll, periodically
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pollFrequency"></param>
        /// <param name="presentStudents"></param>
        /// <returns></returns>
        static async Task CreatePoll(CommandContext ctx, TimeSpan pollFrequency, List<ulong> presentStudents)
        {
            var attendanceEmoji = DiscordEmoji.FromName(ctx.Client, ":raised_hand:");
            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Attendance Poll",
                Description = $"React with '{attendanceEmoji}' to confirm your attendance.",
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            var interactivity = ctx.Client.GetInteractivity();

            if (pollFrequency == default)
                pollFrequency = TimeSpan.FromMinutes(20);  //Sets default poll frequency if it was omitted at command call

            // Generates poll
            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);
            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":raised_hand:")).ConfigureAwait(false); // Defines the emojis to be used in the poll

            // Extracts the info of Discord users who reacted to the poll
            var pollResults = await interactivity.CollectReactionsAsync(pollMessage, pollFrequency).ConfigureAwait(false); // Collect poll reactions
            var results = pollResults.Select(x => $"{x.Users.ToArray()[0]}");
            string[] attendanceInfo = string.Join("\n", results).Split("\n"); // Makes a list of the info of the students who answered the poll

            // Deletes poll + Sends attendance confirmation message
            await ctx.Channel.SendMessageAsync($"Attendance successfully taken on {DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm")}.");
            await pollMessage.DeleteAsync();

            // Extracts the Discord ID of users who reacted to the poll
            ulong[] usersWhoReacted = new ulong[attendanceInfo.Length];

            for (int i = 0; i < attendanceInfo.Length; i++)
            {
                ulong userID = Convert.ToUInt64(attendanceInfo[i].Substring(7, 18));
                usersWhoReacted[i] = userID; // Stores ID of people who reacted
            }

            // Extracts the usernames of people in the VC (if the teacher is in VC)
            // Filters out students who reacted to the poll but were not actually in class (i.e. in teacher's VC)
            var teacherVC = ctx.Member?.VoiceState?.Channel; //VC channel

            if (teacherVC != null)
            {
                var resultsVC = teacherVC.Users.ToArray()[0];

                string[] userInfo = string.Join("\n", resultsVC).Split("\n");
                ulong[] usersInVC = new ulong[attendanceInfo.Length];

                for (int i = 0; i < userInfo.Length; i++)
                {
                    ulong userID = Convert.ToUInt64(userInfo[i].Substring(7, 18));
                    usersInVC[i] = userID; // Stores ID of people in the VC
                }

                // Filters out reactions made by people not in VC
                foreach (ulong userID in usersWhoReacted)
                {
                    if (usersInVC.Contains(userID))
                        presentStudents.Add(userID);
                }
            }
            // Uses no filtering if the teacher is not in a VC
            else
            {
                foreach (ulong userID in usersWhoReacted)
                {
                    presentStudents.Add(userID);
                }
            }
           
        }
        #endregion

        #region Method - GetStudents
        /// <summary>
        /// Adds all of the students in the server to the List of Students
        /// </summary>
        static async Task GetStudents(CommandContext ctx, List<Student> allStudents)
        {
            var allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);

            string membersInfo = string.Join("\n", allMembers); // Makes a list of all server members
            string[] memberInfo = membersInfo.Split("\n"); // Seperates the list into individual lines and stores them

            string memberYrRaw = string.Empty;
            string memberSectRaw = string.Empty;
            int memberYear;
            int memberSection;

            for (int i = 0; i < memberInfo.Length; i++)
            {
                if (!allMembers.ToArray()[i].IsBot)
                {
                    string[] split = memberInfo[i].Split("(");

                    // Get nickname from string member info
                    string memberNickName = split[1].Substring(0, split[1].Length - 1);

                    // Get member ID from split string of member info
                    const int START_CHAR = 7;
                    const int ID_LENGTH = 18;
                    ulong memberID = Convert.ToUInt64(split[0].Substring(START_CHAR, ID_LENGTH));

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
        #endregion

        #region Method - SortPresentStudents
        /// <summary>
        /// Sorts present students who reacted to the poll (current year and section) and adds 1 to their times present
        static Task SortPresentStudents(CommandContext ctx, List<Student> allStudents, List<ulong> presentStudents, out int year, out int section)
        {
            int[] years = new int[3];
            int[] sections = new int[2];

            // Get present students 
            for (int i = 0; i < allStudents.Count; i++)
            {
                if (presentStudents.Contains(allStudents[i].IdNum))
                {
                    allStudents[i].TimesPresent++;
                    years[allStudents[i].Year - 1]++;
                    sections[allStudents[i].Section - 1]++;
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
        #endregion

        #region Method - GenerateAttendanceReport
        /// <summary>
        /// Builds and saves an attendance report consisting of present students from both class sections, and all absent students from the current section
        /// </summary>
        public Task GenerateAttendanceReport(CommandContext ctx, List<Student> allStudents, string reportFile, int year, int section, int numPolls)
        {
            StringBuilder report = new StringBuilder();
            StringBuilder outOfSectionStudents = null;
            StringBuilder absentStudents = null;

            report.Append(string.Format("\n\nYear {0} Section {1} - {2}\n=== PRESENT STUDENTS ===\n\n", year, section, DateTime.Now.ToString("MM-dd")));

            int numPresent = 0; // Number of students in the CORRECT section whom are present
            int numAbsent = 0; //  Number of students in the CORRECT section whom are absent
            int numFullAttendance = 0; //Number of students in any section whom answered ALL the polls

            const int ACCEPTABLE_RATIO = 3 / 5;
            int acceptableTimesPresent = (int)Math.Ceiling((double)(ACCEPTABLE_RATIO * numPolls)); // If a student has answered the polls 60%+ of the time, he/she is present

            // Builder different parts of report
            for (int i = 0; i < allStudents.Count; i++)
            {
                if (allStudents[i].TimesPresent == numPolls)
                    numFullAttendance++;   

                if (allStudents[i].TimesPresent > acceptableTimesPresent)
                {
                    if (allStudents[i].Section == section)
                    {
                        report.Append(string.Format("{0}\n", allStudents[i].NickName));
                        numPresent++;
                    }
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
                    numAbsent++;
                }
            }

            // Add present out of section students to report
            report.Append("\n=== PRESENT OUT-OF-SECTION STUDENTS ===\n\n");
            if (outOfSectionStudents != null)
                report.Append(outOfSectionStudents);
            else
                report.Append("n/a\n");

            // Add absent students to report
            report.Append("\n=== ABSENT STUDENTS ===\n\n");
            if (absentStudents != null)
                report.Append(absentStudents);
            else
                report.Append("n/a\n");

            // Adds attendance stats
            const int ONE_HUNDRED = 100;
            int numStudents = numPresent + numAbsent;

            double percentagePresent = (double)numPresent / numStudents * ONE_HUNDRED;
            double percentageAbsent  = ONE_HUNDRED - percentagePresent;

            report.Append("\n=== ATTENDANCE STATS ===\n\n" +
                          $"Percentage of people in Section {section} whom were present: {percentagePresent}%\n" +
                          $"Percentage of people in Section {section} whom were absent: {percentageAbsent}%\n" +
                          $"Number of people who had 100% attendance: {numFullAttendance}\n");

            // Write the report
            StreamWriter sr = new StreamWriter(reportFile, true);
            sr.Write(Convert.ToString(report));
            sr.Close();

            return Task.CompletedTask;
        }
        #endregion

        #region Method - MessageAbsentStudents
        /// <summary>
        /// Auto-sends a message via DM telling students that they were absent from class
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

                const int START_CHAR = 7;
                const int ID_LENGTH = 18;
                memberID = Convert.ToUInt64(split[0].Substring(START_CHAR, ID_LENGTH));

                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (memberID == allStudents[j].IdNum && allStudents[j].Section == section && allStudents[j].Year == year && allStudents[j].TimesPresent == 0) // Finds absent students from current section
                    {
                        await allMembers.ToArray()[i].SendMessageAsync(string.Format("You were absent from {0} on {1}", ctx.Guild.Name, DateTime.Now.ToString("MM-dd"))); 
                    }
                }
            }
        }
        #endregion

        #region Method - ErrorMessage
        public Task<DiscordEmbedBuilder> ErrorMessage(string error)
        {
            return Task.FromResult(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkRed,
                Title = "Error",
                Description = error
            });
        }
        #endregion
    }
}