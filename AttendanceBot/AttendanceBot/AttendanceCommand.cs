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

namespace AttendanceBot
{
    class AttendanceCommand : BaseCommandModule
    {
        public List<string> PresentStudents { get; set; } = new List<string>();
        public List<Student> AllStudents { get; set; } = new List<Student>();
        public int Section { get; set; }

        [Command("attendance")]
        public async Task Poll(CommandContext ctx, int section) // Takes in all information about the command
        {
            Section = section;

            TimeSpan duration = new TimeSpan(0, 0, 5); // How long the poll remains active for

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
                PresentStudents.Add(studentInfoSplit[0]); // Stores each username in the List<> of names
            }

            await ReadStudentList();

            await GenerateAttendanceReport();
        }

        public Task ReadStudentList()
        {
            string file = "../../../../../Year1StudentList.csv";

            StreamReader sr = new StreamReader(file);

            string line = sr.ReadLine();
            for (int i = 0; (line = sr.ReadLine()) != null; i++)
            {
                string[] values = line.Split(",");
                AllStudents.Add(new Student(values[0], values[1], int.Parse(values[2]), values[3], int.Parse(values[4])));
            }

            if (sr != null)
                sr.Close();

            return Task.CompletedTask;
        }

        public Task GenerateAttendanceReport()
        {
            int i;
            StringBuilder report = new StringBuilder();
            StringBuilder outOfSectionStudents = null;

            report.Append(string.Format("{0}\nPresent Students\n\n", DateTime.Now.ToString("MM-dd")));
           
            // Add present students to report
            for (i = 0; i < PresentStudents.Count; i++)
            {
                for (int j = 0; j < AllStudents.Count; j++)
                {
                    if (PresentStudents[i] == AllStudents[j].UserName)
                    {
                        AllStudents[j].Present = true;
                        if (AllStudents[j].Section == Section)
                            report.Append(string.Format("{0},{1}\n", AllStudents[j].LastName, AllStudents[j].FirstName));
                        else
                        {
                            if (outOfSectionStudents == null)
                                outOfSectionStudents = new StringBuilder();

                            outOfSectionStudents.Append(string.Format("{0},{1}\n", AllStudents[j].LastName, AllStudents[j].FirstName));
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
            for (i = 0; i < AllStudents.Count; i++)
            {
                if (AllStudents[i].Present == false && AllStudents[i].Section == Section)
                    report.Append(string.Format("{0},{1}\n", AllStudents[i].LastName, AllStudents[i].FirstName));
            }

            // Write the report
            string filename = string.Format("../../../../../AttendanceReport-{0}.csv", DateTime.Now.ToString("MM-dd"));
            StreamWriter sr = new StreamWriter(filename, true);
            sr.Write(Convert.ToString(report));
            sr.Close();

            return Task.CompletedTask;
        }
    }
}