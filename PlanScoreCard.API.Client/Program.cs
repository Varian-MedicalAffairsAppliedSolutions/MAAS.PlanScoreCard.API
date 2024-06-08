using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

[assembly:ESAPIScript(IsWriteable =true)]
namespace PlanScoreCard.API.Client
{
    internal class Program
    {
        private static string _patientId;
        private static string _courseId;
        private static string _planId;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                _patientId = args.First().Split(';').First();
                _courseId = args.First().Split(';').ElementAt(1);
                _planId = args.First().Split(';').Last();
                using(Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        private static void Execute(Application app)
        {
            var scoreCenter = new ScoreCenter(app, true);
            //access patient, course and plan from stored details in Main.
            Patient patient = app.OpenPatientById(_patientId);
            Course course = patient.Courses.FirstOrDefault(c => c.Id.Equals(_courseId));
            PlanSetup plan = course.PlanSetups.FirstOrDefault(ps => ps.Id.Equals(_planId));
            Console.WriteLine($"Patient open {patient.Name}");
            Console.WriteLine($"Open plan {plan.Id} on {course.Id}");
            //Set plan and scorecard
            scoreCenter.SetPlanModel(plan);
            string details = String.Empty;
            scoreCenter.LoadScorecardFromFile(@"C:\Users\mschmidt\Desktop\PSC_old\PlanScoreCard\Scorecards\SC_Esophagus(50.4Gy)_2019RTOG1010_PlanStudy.json", out details);
            Console.WriteLine(details);
            //write initial score
            string scoreDetails = String.Empty;
            var scores = scoreCenter.ScorePlan(false, out scoreDetails);
            Console.WriteLine(scoreDetails);
            double score = scores.Sum(sc => sc.ScoreValues.First().Score);
            double max = scores.Sum(sc => sc.ScoreMax);
            Console.WriteLine($"Score: {score:F2} of {max:F2}");
            var planModel = scoreCenter.NormPlan(false, true);
            Console.WriteLine($"Plan with max score saved at {planModel.PlanId} on course {planModel.CourseId}");
        }
    }
}
