using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BannerWebCrawler
{
    class Evaluation
    {
        public int Year { get; set; }
        public string Subject { get; set; }
        public string Course { get; set; }
        public string Section { get; set; }
        public string Instructor { get; set; }
        public double CourseQuality { get; set; }
        public double InstructorQuality { get; set; }
        public int ClassTime { get; set; }
        public string Grade { get; set; }

        public string ToJS()
        {
            return String.Format("{{yr:{0},su:\"{1}\",cr:\"{2}\",sc:\"{3}\",in:\"{4}\",rc:{5:F1},ri:{6:F1},wl:{7},gr:\"{8}\"}}",
                    Year, Subject, Course, Section, Instructor, CourseQuality, InstructorQuality, ClassTime, Grade);
        }

        public string ToFile()
        {
            return String.Format("{0}-{1}-{2}-{3}.html", Year, Subject, Course, Section);
        }
    }
}
