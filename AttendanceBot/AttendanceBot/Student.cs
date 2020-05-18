using System;
using System.Collections.Generic;
using System.Text;

namespace AttendanceBot
{
    class Student
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Section { get; set; }
        public string UserName { get; set; }
        public int IdNum { get; set; }
        public bool Present { get; set; }


        public Student()
        {
            LastName = string.Empty;
            FirstName = string.Empty;
            Section = 0;
            UserName = string.Empty;
            IdNum = 0;
            Present = false;
        }
        public Student(string ln_, string fn_, int s_, string us_, int id_, bool p_ = false)
        {
            LastName = ln_;
            FirstName = fn_;
            Section = s_;
            UserName = us_;
            IdNum = id_;
            Present = p_;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}", LastName, FirstName, Section, UserName, IdNum, Present);
        }
    }
}
