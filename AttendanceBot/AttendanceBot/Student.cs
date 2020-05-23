using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AttendanceBot
{
    class Student
    {
        public string NickName { get; set; }
        public int Year { get; set; }
        public int Section { get; set; }
        public ulong IdNum { get; set; }
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
