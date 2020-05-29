/*
    Author: Brent Pereira, Maxence Roy
    Latest Update: May 28th, 2020
    Description: Methods and properties of the class 'Student' for a student Discord user.
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AttendanceBot
{
    /// <summary>
    /// Class for a Discord user who attends a teacher's class.
    /// </summary>
    class Student
    {
        /// <summary>
        /// The student's discord username (not to be confused with the ID).
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// The student's current year.
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// The student's current section (class number).
        /// </summary>
        public int Section { get; set; }

        /// <summary>
        /// The unique ID value of the student's discord account.
        /// </summary>
        public ulong IdNum { get; set; }

        /// <summary>
        /// The amount of times the student has been present during a class (resets every class).
        /// </summary>
        public int TimesPresent { get; set; }


        public Student()
        {
            NickName = string.Empty;
            Year = 0;
            Section = 0;
            IdNum = 0;
            TimesPresent = 0;
        }
        public Student(string nn_, int y_, int s_, ulong id_)
        {
            NickName = nn_;
            Year = y_;
            Section = s_;
            IdNum = id_;
            TimesPresent = 0;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", NickName, Year, Section, IdNum, TimesPresent);
        }
    }
}
