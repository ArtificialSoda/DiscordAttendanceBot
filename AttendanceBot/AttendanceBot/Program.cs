/*
    Author: Brent Pereira
    Latest Update: May 16th, 2020
    Description: Program file that creates an instance of the Attendance Bot.
*/

using System;

namespace AttendanceBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult();
        }
    }
}