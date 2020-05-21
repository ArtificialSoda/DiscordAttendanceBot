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
        public async Task Poll(CommandContext ctx, int currentSection) // Takes in all information about the command
        {
            List<string> presentStudents = new List<string>();
            List<Student> allStudents = new List<Student>();
            string reportFile = string.Format("../../../../../AttendanceReport-{0}.csv", DateTime.Now.ToString("MM-dd"));

            TimeSpan duration = new TimeSpan(0, 0, 5); // How long the poll remains active for   *needs to be changed (5 seconds was used for testing) 

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
                presentStudents.Add(studentInfoSplit[0]); // Stores each username in the List<> of names
            }

            await ReadStudentList(allStudents);

            await GenerateAttendanceReport(presentStudents, allStudents, currentSection, reportFile);

            await ctx.Member.SendFileAsync(reportFile); // Sends attedance report file to teacher via DM after it's made

            await MessageAbsentStudents(ctx, allStudents, currentSection);
        }

        /// <summary>
        /// Reads the student list and stores the information in the list of Student's
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
            int i;
            StringBuilder report = new StringBuilder();
            StringBuilder outOfSectionStudents = null;

            report.Append(string.Format("{0}\nPresent Students\n\n", DateTime.Now.ToString("MM-dd")));
           
            // Add present students to report
            for (i = 0; i < presentStudents.Count; i++)
            {
                for (int j = 0; j < allStudents.Count; j++)
                {
                    if (presentStudents[i] == allStudents[j].UserName)
                    {
                        allStudents[j].Present = true;
                        if (allStudents[j].Section == section)
                            report.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
                        else
                        {
                            if (outOfSectionStudents == null)
                                outOfSectionStudents = new StringBuilder();

                            outOfSectionStudents.Append(string.Format("{0},{1}\n", allStudents[j].LastName, allStudents[j].FirstName));
                        }
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
            for (i = 0; i < allStudents.Count; i++)
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