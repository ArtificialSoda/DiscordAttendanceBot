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
    /// When command is called, a poll is built and outputted, and an attendance report is built based on reactions. Report is then sent to teacher via DM
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
        {
            string reportFile = string.Format("../../../../../AttendanceReportS{0}Y{1}-{2}.csv", currentSection, currentYear, DateTime.Now.ToString("MM-dd"));
            
            List<string> presentStudents = new List<string>();
            List<Student> allStudents = new List<Student>();

            var interactivity = ctx.Client.GetInteractivity();

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

            await ReadStudentList(allStudents);

            await GenerateAttendanceReport(presentStudents, allStudents, currentSection, reportFile);

            await ctx.Member.SendFileAsync(reportFile); // Sends attedance report file to teacher via DM after it's made

            await MessageAbsentStudents(ctx, allStudents, currentSection);
        }

        /// <summary>
        /// Reads the student list and stores the information in the list of Students
        /// </summary>
        public Task ReadStudentList(List<Student> allStudents)
        {
            string file = "../../../../../Year1StudentList.csv"; // Student list to be read

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
            {
                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (presentStudents[i] == allStudents[j].UserName)
                    {
                        allStudents[j].Present = true;
                        if (allStudents[j].Section == section)
                            report.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
                        else
                            outOfSectionStudents.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
                    }
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

            // Write the report
            StreamWriter sr = new StreamWriter(reportFile, true);
            sr.Write(Convert.ToString(report));
            sr.Close();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Auto-sends a message vs DM telling students that their were absent from class
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
    }
}