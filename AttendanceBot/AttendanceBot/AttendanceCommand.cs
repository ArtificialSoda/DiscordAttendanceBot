/*
    Author: Jordan McIntyre, Fabian Dimitrov, Brent Pereira
    Latest Update: May 27th, 2020
    Description: This program contains all methods related to the Attendance Command.
*/

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceBot
{
    /// <summary>
    /// The 'AttendanceCommand' class contains the necessary methods for the Attendance Command's functionality.
    /// </summary>
    class AttendanceCommand : BaseCommandModule
    {
        [Command("attendance")]
        [Description("Takes periodic class attendance via student reaction to the attendance poll.")]
        [RequireRoles(RoleCheckMode.Any, "Teacher")]
        public async Task Poll(CommandContext ctx,
                              [Description("Duration of poll (e.g. 90m, 1h30m)")] TimeSpan classDuration,
                              [Description("Duration of poll (e.g: 90s, 3m)")] TimeSpan pollDuration, 
                              [Description("Student section")] int currentSection,
                              [Description("Student year")]int currentYear)
        [RequireRoles(RoleCheckMode.Any, "Teacher", "Professor", "Admin", "Administrator")] // Roles can be changed, if need be
        public async Task Poll(CommandContext ctx,
                              [Description("Duration of poll (e.g. 90m, 1h30m)")] TimeSpan? classDuration,
                              [Description("Duration of poll (e.g: 90s, 3m)")] TimeSpan? pollDuration)
        {
            string reportFile = string.Format("../../../../../AttendanceReportS{0}Y{1}-{2}.csv", currentSection, currentYear, DateTime.Now.ToString("MM-dd"));
            
            List<string> presentStudents = new List<string>();
            List<Student> allStudents = new List<Student>();

            int year, section;
            string reportFile = string.Format("../../../../../AttendanceReport-{0}.csv", DateTime.Now.ToString("MM-dd"));


            
            // Generates a list of all students in the Discord Server 
            await GetStudents(ctx, allStudents);

            // Generates the attendance poll
            await CreatePoll(ctx, pollDuration, presentStudents);

            // Sorts students who reacted to the poll by current year and section
            await SortPresentStudents(ctx, allStudents, presentStudents, out year, out section);

            // Generates the attendance report (CSV format)
            await GenerateAttendanceReport(allStudents, reportFile, year, section);

            // Sends attedance report file to teacher via DM after it's made
            await ctx.Member.SendFileAsync(reportFile); 

            // Sends message via DM to students who were absent 
            await MessageAbsentStudents(ctx, allStudents, year, section);
        }

            var attendanceEmoji = DiscordEmoji.FromName(ctx.Client, ":raised_hand:");

        #region Method - CreatePoll
        /// <summary>
        /// Generates the attendance poll, periodically
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="pollDuration"></param>
        /// <param name="presentStudents"></param>
        /// <returns></returns>
        static async Task CreatePoll(CommandContext ctx, TimeSpan? pollDuration, List<ulong> presentStudents)
        {
            var attendanceEmoji = DiscordEmoji.FromName(ctx.Client, ":raised_hand:");
            var pollEmbed = new DiscordEmbedBuilder
            {
                Title = "Attendance Poll",
                Description = $"React with '{attendanceEmoji}' to confirm your attendance.",
                ThumbnailUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = DiscordColor.Green
            };

            #region Create Poll

            // Generates poll
            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);
            await pollMessage.CreateReactionAsync(attendanceEmoji).ConfigureAwait(false);

            // Extracts the usernames of people who reacted to the poll
            var result = await interactivity.CollectReactionsAsync(pollMessage, pollDuration).ConfigureAwait(false);
            var results = result.Select(x => $"{x.Users.ToArray()[0]}");

            await ctx.Channel.SendMessageAsync($"Attendance successfully taken on {DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm")}.");
            await pollMessage.DeleteAsync();

            string[] reactInfo = string.Join("\n", results).Split("\n");
            string[] usersWhoReacted = new string[reactInfo.Length];

            const int START_INDEX = 27;
            for (int i = 0; i < reactInfo.Length; i++)
            {
                string[] user = reactInfo[i].Substring(START_INDEX).Split("#");
                usersWhoReacted[i] = (user[0]); // Stores usernames of people who reacted
            }

            // Extracts the usernames of people in the VC (if the teacher is in VC)
            var teacherVC = ctx.Member?.VoiceState?.Channel; //VC channel

            if (teacherVC != null)
            {
                var resultsVC = teacherVC.Users.ToArray()[0];

                string[] userInfo = string.Join("\n", resultsVC).Split("\n");
                string[] usersInVC = new string[reactInfo.Length];

                for (int i = 0; i < userInfo.Length; i++)
                {
                    string[] user = userInfo[i].Substring(START_INDEX).Split("#");
                    usersInVC[i] = (user[0]); // Stores usernames of people in the VC
                }

                // Filters out reactions made by people not in VC
                foreach (string user in usersWhoReacted)
                {
                    if (usersInVC.Contains(user))
                    {
                        presentStudents.Add(user);
                        await ctx.Channel.SendMessageAsync($"Student is present: {user}").ConfigureAwait(false);
            var interactivity = ctx.Client.GetInteractivity();

            if (pollDuration == null)
                pollDuration = TimeSpan.FromSeconds(60); //Sets default poll duration if it was omitted at command call

            // Generates poll
            var pollMessage = await ctx.Channel.SendMessageAsync(embed: pollEmbed).ConfigureAwait(false);
            await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":raised_hand:")).ConfigureAwait(false); // Defines the emojis to be used in the poll

            // Extracts the info of Discord users who reacted to the poll
            var pollResults = await interactivity.CollectReactionsAsync(pollMessage, pollDuration).ConfigureAwait(false); // Collect poll reactions
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

            string memberNickName;
            string memberYrRaw;
            string memberSectRaw;
            ulong memberID;
            int memberYear;
            int memberSection;

                    }
                    else
                        await ctx.Channel.SendMessageAsync($"Student is not present because absent in the {teacherVC.Name}: {user}").ConfigureAwait(false);
                }
            }
            else
            {
                foreach (string user in usersWhoReacted)
                {
                    presentStudents.Add(user);
                    await ctx.Channel.SendMessageAsync($"Student is present: {user}").ConfigureAwait(false);
                }
            }
            #endregion
=======
                if (allMembers.ToArray()[i].IsBot == false)
                {
                    string[] split = memberInfo[i].Split("(");

                    // Get nickname from string member info
                    memberNickName = split[1].Substring(0, split[1].Length - 1);

                    // Get member ID from split string of member info
                    memberID = Convert.ToUInt64(split[0].Substring(7, 18));

                    // Get member year from first role
                    split = allMembers.ToArray()[i].Roles.ToArray()[0].ToString().Split("; ");
                    memberYrRaw = split[1];

                    // Get year as an integer from the string role
                    if (memberYrRaw == "First Year")
                        memberYear = 1;
                    else if (memberYrRaw == "Second Year")
                        memberYear = 2;
                    else if (memberYrRaw == "Third Year")
                        memberYear = 3;
                    else
                        memberYear = 0;

            await ReadStudentList(allStudents);

            await GenerateAttendanceReport(presentStudents, allStudents, currentSection, reportFile);

            await ctx.Member.SendFileAsync(reportFile); // Sends attedance report file to teacher via DM after it's made

            await MessageAbsentStudents(ctx, allStudents, currentSection);
        }
        #endregion

        #region Method - SortPresentStudents
        /// <summary>
        /// Reads the student list and stores the information in the list of Students
        /// </summary>
        public Task ReadStudentList(List<Student> allStudents)
        {
            string file = "../../../../../Year1StudentList.csv"; // Student list to be read
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

            StreamReader sr = new StreamReader(file);

            string line = sr.ReadLine(); // reads titles but doesn't store them
            for (int i = 0; (line = sr.ReadLine()) != null; i++)
            {
                string[] values = line.Split(",");
                allStudents.Add(new Student(values[0], values[1], int.Parse(values[2]), values[3], int.Parse(values[4]))); // Creates a new students using the information 
            }                                                                                                              // from the read line as arguments

            if (sr != null)
                sr.Close();

            return Task.CompletedTask;
        }
        #endregion

        #region Method - GenerateAttendanceReport
        /// <summary>
        /// Builds and saves an attendance report consisting of present students from both class sections, and all absent students from the specified section
        /// </summary>
        public Task GenerateAttendanceReport(List<string> presentStudents, List<Student> allStudents, int section, string reportFile)
        {
            StringBuilder report = new StringBuilder();
            StringBuilder outOfSectionStudents = new StringBuilder();

            report.Append(string.Format("{0}\nPresent Students\n\n", DateTime.Now.ToString("MM-dd")));
           
            // Add present students to report
            for (int i = 0; i < presentStudents.Count; i++)
            int numPresent = 0; // Number of students in the CORRECT section whom are present
            int numAbsent = 0; //  Number of students in the CORRECT section whom are absent

            // Builder different parts of report
            for (int i = 0; i < allStudents.Count; i++)
            {
                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (presentStudents[i] == allStudents[j].UserName)
                    if (allStudents[i].Section == section)
                    {
                        report.Append(string.Format("{0}\n", allStudents[i].NickName));
                        numPresent++;
                    }
                    else
                    {
                        allStudents[j].Present = true;
                        if (allStudents[j].Section == section)
                            report.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
                        else
                            outOfSectionStudents.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
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
            if (outOfSectionStudents != null)
            {
                report.Append("\nPresent Out Of Section Students\n\n");
                report.Append(outOfSectionStudents);
            }

            // Add absent students to report
            report.Append("\n\nAbsent Students\n\n");
            for (int i = 0; i < allStudents.Count; i++)
            {
                if (allStudents[i].Present == false && allStudents[i].Section == section)
                    report.Append(string.Format("{0},{1}\n", allStudents[i].LastName, allStudents[i].FirstName));
            }

            // Adds attendance stats
 

            // Write the report
            StreamWriter sr = new StreamWriter(reportFile, true);
            sr.Write(Convert.ToString(report));
            sr.Close();

            return Task.CompletedTask;
        }
        #endregion

        #region Method - MessageAbsentStudents
        /// <summary>
        /// Auto-sends a message vs DM telling students that their were absent from class
        /// Auto-sends a message via DM telling students that they were absent from class
        /// </summary>
        static async Task MessageAbsentStudents(CommandContext ctx, List<Student> allStudents, int section)
        {
            string[] memberInfoSplit;
            var allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            string membersInfo = string.Join("\n", allMembers); // Makes a list of all server members
            string[] memberInfo = membersInfo.Split("\n"); // Seperates the list into individual lines and stores them

            for (int i = 0; i < memberInfo.Length; i++)
            {
                memberInfoSplit = memberInfo[i].Substring(27).Split("#"); // Seperates the username from other info

                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (memberInfo[0] == allStudents[j].UserName && allStudents[j].Section == section && allStudents[j].Present == false) // Finds absent students from current section
                    {
                        await allMembers.ToArray()[i].SendMessageAsync(string.Format("You were absent from {0} on {1}", ctx.Guild.Name, DateTime.Now.ToString("MM-dd"))); 
                    }
                }
            }
        }
        #endregion
    }
}