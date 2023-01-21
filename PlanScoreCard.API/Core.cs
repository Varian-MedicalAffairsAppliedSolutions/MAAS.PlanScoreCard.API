using Newtonsoft.Json;
using PlanScoreCard.API.Models;
using PlanScoreCard.API.Models.ScoreCard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace PlanScoreCard.API
{
    public class Core
    {
        private Application _Application;
        public Core()
        {
            _Application = Application.CreateApplication();
        }
        /// <summary>
        /// Dispose of ESAPI application object
        /// </summary>
        public void DisposeApplication()
        {
            _Application.Dispose();
        }
        /// <summary>
        /// Convert JSON template into a TemplateObject.
        /// </summary>
        /// <param name="fileName">Full file name for scorecard template</param>
        /// <returns>Internal Template Model</returns>
        /// <exception cref="ApplicationException"></exception>
        public InternalTemplateModel LoadScorecardFromFile(string fileName)
        {
            try
            {
                InternalTemplateModel template = JsonConvert.DeserializeObject<InternalTemplateModel>(File.ReadAllText(fileName));
                return template;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }
        /// <summary>
        /// Finds PlanModel object from JSON file
        /// </summary>
        /// <param name="fileName">File contains Plan Id, Course Id, and Patient Id</param>
        /// <param name="returnDetails">String to help user with debugging. </param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public PlanModel GetPlanFromFile(string fileName, out string returnDetails)
        {
            try
            {
                PatientPlanModel plan = JsonConvert.DeserializeObject<PatientPlanModel>(File.ReadAllText(fileName));
                _Application.ClosePatient();
                var patient = _Application.OpenPatientById(plan.PatientId);
                if(patient == null) { returnDetails = $"Could not find patient with Id: {plan.PatientId}"; return null; }
                var course = patient.Courses.FirstOrDefault(c => c.Id.Equals(plan.CourseId));
                if(course == null) { returnDetails = $"Could not find course with Id: {plan.CourseId}"; return null; }
                var planSetup = course.PlanSetups.FirstOrDefault(ps => ps.Id.Equals(plan.PlanId));
                if (planSetup == null) { returnDetails = $"Could not find plan with Id: {plan.PlanId}"; return null; }
                PlanModel planModel = new PlanModel(planSetup);
                returnDetails = "Success";
                return planModel;
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }

    }
}
