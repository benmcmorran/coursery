using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BannerWebCrawler
{
    class Program
    {
        private Uri home = new Uri("https://bannerweb.wpi.edu");
        private Uri validateLogin = new Uri("https://bannerweb.wpi.edu/pls/prod/twbkwbis.P_ValLogin");
        private Uri selectYear = new Uri("https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_Year");
        private Uri selectCourseInstructor = new Uri("https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_CrseInst");
        private Uri selectCourse = new Uri("https://bannerweb.wpi.edu/pls/prod/hwwkscevrp.P_Select_CrseSect");

        private CookieContainer cookies = new CookieContainer();
        private CookieAwareWebClient client;

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((obj, cert, chain, errors) => true);
            new Program().Run3();
        }

        public void Run()
        {
            string username = Console.ReadLine();
            string password = Console.ReadLine();

            for (var yearnum = 2006; yearnum >= 2006; yearnum--)
            {

                client = new CookieAwareWebClient(cookies);
                ConfigureCookies();

                var year = yearnum.ToString();

                Login(username, password);

                using (var file = new StreamWriter("C:\\CourseEvaluations\\" + year + "uris.txt"))
                {
                    string resultsPath = @"C:\CourseEvaluations";
                    //foreach (var year in SelectYear())
                    {
                        System.Threading.Thread.Sleep(100);
                        string resultsYearPath = Path.Combine(resultsPath, year);
                        bool first = true;
                        foreach (var course in SelectCourseInstructor(year, "X"))
                        {
                            if (first) { first = false; continue; }
                            System.Threading.Thread.Sleep(100);
                            string resultsYearCoursePath = Path.Combine(resultsYearPath, course);
                            //Directory.CreateDirectory(resultsYearCoursePath);
                            var count = 0;
                            foreach (var section in SelectCourse(course, "", year, "X"))
                            {
                                //System.Threading.Thread.Sleep(100);
                                Log("Requesting evaluation number " + count.ToString() + " for course " + course + " of academic year " + year);
                                string resultsYearCourseSectionPath = Path.Combine(resultsYearCoursePath, count.ToString() + ".htm");
                                /*using (var file = new FileStream(resultsYearCourseSectionPath, FileMode.CreateNew))
                                using (var stream = client.OpenRead(section))
                                    stream.CopyTo(file);*/
                                file.WriteLine(section.AbsoluteUri);
                                count++;
                            }
                        }
                    }
                }
            }

            Log("Complete");

            Console.ReadLine();
        }

        public void Run2()
        {
            string[] paths = Console.ReadLine().Split(';');
            client = new CookieAwareWebClient(cookies);
            ConfigureCookies();
            Login(Console.ReadLine(), Console.ReadLine());

            foreach (var path in paths)
            {
                var count = 0;
                using (var reader = new StreamReader(path))
                {
                    var resultPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                    Directory.CreateDirectory(resultPath);
                    while (!reader.EndOfStream)
                    {
                        var uri = reader.ReadLine();
                        var filePath = Path.Combine(resultPath, count.ToString() + ".htm");
                        if (File.Exists(filePath)) { count++; continue; }
                        using (var file = new FileStream(filePath, FileMode.CreateNew))
                        {
                            Log("Requesting number " + count.ToString() + " " + uri.Substring(uri.Length - 50, 50));
                            using (var stream = client.OpenRead(uri))
                                stream.CopyTo(file);
                            count++;
                        }
                    }
                }
            }
        }

        public void Run3()
        {
            StringBuilder result = new StringBuilder();
            StringBuilder script = new StringBuilder();
            var errors = 0;
            var processed = 0;

            string[] paths = Console.ReadLine().Split(';');
            foreach (var dirPath in paths)
            {
                if (!Directory.Exists(dirPath))
                {
                    Log("Invalid directory " + dirPath);
                    break;
                }

                foreach (var filePath in Directory.EnumerateFiles(dirPath))
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        Log("Processing " + filePath);
                        var file = reader.ReadToEnd();
                        try
                        {
                            var eval = ProcessFile(file);

                            result.Append(eval.ToJS());
                            result.AppendLine(",");

                            script.AppendLine("ren " + filePath + " " + eval.ToFile());
                        }
                        catch (ArgumentException e)
                        {
                            Log("Error at " + filePath + ": " + e.Message);
                            script.AppendLine("del " + filePath);
                            errors++;
                            /*System.Diagnostics.Process.Start(filePath);
                            Console.Write("Exclude? (y/n)");
                            if (Console.ReadKey().KeyChar == 'y') continue;
                            Console.WriteLine("\nManually enter json");
                            result.Append(Console.ReadLine());
                            result.Append(',');*/
                        }
                        processed++;
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(@"C:\CourseEvaluations\result.js"))
            {
                Log("Writing output");
                writer.WriteLine("courses=[");
                writer.Write(result.ToString());
                writer.Write("];");
            }

            using (StreamWriter writer = new StreamWriter(@"C:\CourseEvaluations\renamer.bat"))
            {
                Log("Writing script");
                writer.Write(script.ToString());
            }

            Log(errors + " errors, " + processed + " items (" + (float)errors * 100 / processed + "%)");

            Console.ReadKey();
        }

        private Evaluation ProcessFile(string file)
        {
            var REyear = @"Academic Year \d{4}-(\d{4})";
            var REsubjectCourse = @"([A-Z]+)-(\w+)";
            var REsection = @"Section (\w+)";
            var REinstructor = @"Prof\. ([^<]+)";
            var REquality = @"<p.*?> ([\d\.]+)</p>";
            var REaGrade = @"A</p>.*?(\d+)";
            var REbGrade = @"B</p>.*?(\d+)";
            var REcGrade = @"C</p>.*?(\d+)";
            var REnrGrade = @"NR/D/F</p>.*?(\d+)";
            var REotherGrade = @"Other/Don't know</p>.*?(\d+)";

            var REinClassTime = @"26A\..*?3 hr/wk or less</p>.*?(\d+).*?4 hr/wk</p>.*?(\d+).*?5 hr/wk</p>.*?(\d+).*?6 hr/wk</p>.*?(\d+).*?7 hr/wk or more</p>.*?(\d+)";
            var REoutClassTime = @"26B\..*?0 hr/wk</p>.*?(\d+).*?1-5 hr/wk</p>.*?(\d+).*?6-10 hr/wk</p>.*?(\d+).*?11-15 hr/wk</p>.*?(\d+).*?16-20 hr/wk</p>.*?(\d+).*?21 hr/wk or more</p>.*?(\d+)";

            var REoldClassTime = @"26\..*?8 hrs\. or fewer</p>.*?(\d+).*?9-12 hrs\.</p>.*?(\d+).*?13-16 hrs\.</p>.*?(\d+).*?17-20 hrs\.</p>.*?(\d+).*?21 hrs\. or more</p>.*?(\d+)";

            int year, aGrade, bGrade, cGrade, nrGrade, otherGrade, classTime;
            string subject, course, section, instructor;
            double courseQuality, instructorQuality;
            Match match;

            match = Regex.Match(file, REyear, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find year.");
            year = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REsubjectCourse, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find subject or course.");
            subject = match.Groups[1].Captures[0].Value;
            course = match.Groups[2].Captures[0].Value;

            match = Regex.Match(file, REsection, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find section.");
            section = match.Groups[1].Captures[0].Value;

            match = Regex.Match(file, REinstructor, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find instructor.");
            instructor = match.Groups[1].Captures[0].Value;

            match = Regex.Match(file, REquality, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find course quality.");
            courseQuality = double.Parse(match.Groups[1].Captures[0].Value);

            match = match.NextMatch();
            if (!match.Success) throw new ArgumentException("Could not find instructor quality.");
            instructorQuality = double.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REaGrade, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find A grades.");
            aGrade = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REbGrade, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find B grades.");
            bGrade = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REcGrade, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find C grades.");
            cGrade = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REnrGrade, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find NR grades.");
            nrGrade = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REotherGrade, RegexOptions.Singleline);
            if (!match.Success) throw new ArgumentException("Could not find Other grades.");
            otherGrade = int.Parse(match.Groups[1].Captures[0].Value);

            match = Regex.Match(file, REinClassTime, RegexOptions.Singleline);
            if (match.Success)
            {
                int count = int.Parse(match.Groups[1].Captures[0].Value) +
                            int.Parse(match.Groups[2].Captures[0].Value) +
                            int.Parse(match.Groups[3].Captures[0].Value) +
                            int.Parse(match.Groups[4].Captures[0].Value) +
                            int.Parse(match.Groups[5].Captures[0].Value);

                classTime = int.Parse(match.Groups[1].Captures[0].Value) * 2 +
                            int.Parse(match.Groups[2].Captures[0].Value) * 4 +
                            int.Parse(match.Groups[3].Captures[0].Value) * 5 +
                            int.Parse(match.Groups[4].Captures[0].Value) * 6 +
                            int.Parse(match.Groups[5].Captures[0].Value) * 7;
                classTime /= count;

                match = Regex.Match(file, REoutClassTime, RegexOptions.Singleline);
                if (!match.Success) throw new ArgumentException("Could not find class time.");

                count = int.Parse(match.Groups[1].Captures[0].Value) +
                        int.Parse(match.Groups[2].Captures[0].Value) +
                        int.Parse(match.Groups[3].Captures[0].Value) +
                        int.Parse(match.Groups[4].Captures[0].Value) +
                        int.Parse(match.Groups[5].Captures[0].Value) +
                        int.Parse(match.Groups[6].Captures[0].Value);

                int outClassTime = int.Parse(match.Groups[1].Captures[0].Value) * 0 +
                                   int.Parse(match.Groups[2].Captures[0].Value) * 3 +
                                   int.Parse(match.Groups[3].Captures[0].Value) * 8 +
                                   int.Parse(match.Groups[4].Captures[0].Value) * 13 +
                                   int.Parse(match.Groups[5].Captures[0].Value) * 18 +
                                   int.Parse(match.Groups[6].Captures[0].Value) * 22;
                outClassTime /= count;

                classTime += outClassTime;
            }
            else
            {
                match = Regex.Match(file, REoldClassTime, RegexOptions.Singleline);
                if (!match.Success) throw new ArgumentException("Could not find class time.");

                int count = int.Parse(match.Groups[1].Captures[0].Value) +
                            int.Parse(match.Groups[2].Captures[0].Value) +
                            int.Parse(match.Groups[3].Captures[0].Value) +
                            int.Parse(match.Groups[4].Captures[0].Value) +
                            int.Parse(match.Groups[5].Captures[0].Value);

                classTime = int.Parse(match.Groups[1].Captures[0].Value) * 6 +
                            int.Parse(match.Groups[2].Captures[0].Value) * 10 +
                            int.Parse(match.Groups[3].Captures[0].Value) * 15 +
                            int.Parse(match.Groups[4].Captures[0].Value) * 18 +
                            int.Parse(match.Groups[5].Captures[0].Value) * 22;
                classTime /= count;
            }

            string grade = "";
            if (nrGrade >= aGrade && nrGrade >= bGrade && nrGrade >= cGrade) grade = "NR";
            if (cGrade >= aGrade && cGrade >= bGrade && cGrade >= nrGrade) grade = "C";
            if (bGrade >= aGrade && bGrade >= cGrade && bGrade >= nrGrade) grade = "B";
            if (aGrade >= bGrade && aGrade >= cGrade && aGrade >= nrGrade) grade = "A";

            return new Evaluation
            {
                Year = year,
                Subject = subject,
                Course = course,
                Section = section,
                Instructor = instructor,
                CourseQuality = courseQuality,
                InstructorQuality = instructorQuality,
                ClassTime = classTime,
                Grade = grade
            };
        }

        private void ConfigureCookies()
        {
            cookies.Add(home, new Cookie("TESTID", "set"));
            cookies.Add(home, new Cookie("sto-id-bannerweb-47873", "NMCBNHICLLAB"));
        }

        private void Login(string username, string password)
        {
            Log("Validating login");

            var parameters = new System.Collections.Specialized.NameValueCollection();
            parameters.Add("sid", username);
            parameters.Add("PIN", password);
            client.UploadValues(validateLogin, parameters);
        }

        private string TestMainMenu()
        {
            return new StreamReader(client.OpenRead("https://bannerweb.wpi.edu/pls/prod/twbkwbis.P_GenMenu?name=bmenu.P_MainMnu")).ReadToEnd();
        }

        private IEnumerable<string> SelectYear()
        {
            Log("Requesting available academic years");

            using (var reader = new StreamReader(client.OpenRead(selectYear)))
            {
                var response = reader.ReadToEnd();
                var years = Regex.Match(response, "<SELECT NAME=\"IN_ACYR\".*?(?:VALUE=\"(\\d+)\".*?)+?</SELECT>", RegexOptions.Singleline).Groups[1].Captures;
                foreach (Capture year in years)
                    yield return year.Value;
            }
        }

        private IEnumerable<string> SelectCourseInstructor(string academicYear, string inADLN)
        {
            Log("Requesting courses for academic year " + academicYear.ToString());

            var parameters = new System.Collections.Specialized.NameValueCollection();
            parameters.Add("IN_ACYR", academicYear);
            parameters.Add("IN_ADLN_OIX", inADLN);
            var response = Encoding.UTF8.GetString(client.UploadValues(selectCourseInstructor, parameters));
            var courses = Regex.Match(response, "<SELECT NAME=\"IN_SUBCRSE\".*?(?:VALUE=\"(.+?)\".*?)+?</SELECT>", RegexOptions.Singleline).Groups[1].Captures;
            foreach (Capture course in courses)
                yield return course.Value;
        }

        private IEnumerable<Uri> SelectCourse(string course, string instructor, string academicYear, string inADLN)
        {
            Log("Requesting sections for course " + course + " in academic year " + academicYear);

            var parameters = new System.Collections.Specialized.NameValueCollection();
            parameters.Add("IN_SUBCRSE", course);
            parameters.Add("IN_PIDM", instructor);
            parameters.Add("IN_ACYR", academicYear);
            parameters.Add("IN_ADLN_OIX", inADLN);
            var response = Encoding.UTF8.GetString(client.UploadValues(selectCourse, parameters));
            var matches = Regex.Matches(response, "<TR>.(?:<TD CLASS=\"dddefault\".*?){5}HREF=\"(.*?)\"", RegexOptions.Singleline);
            foreach (Match match in matches)
                foreach (Capture capture in match.Groups[1].Captures)
                    yield return new Uri(home, capture.Value.Replace("&amp;", "&"));
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
