using Newtonsoft.Json;
using PlanScoreCard.API.Extensions;
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
    public class ScoreCenter
    {
        private Application _Application;
        public ScoreCenter(Application application)
        {
            _Application = application;
        }
        //public Core()
        //{
        //    _Application = Application.CreateApplication();
        //}
        ///// <summary>
        ///// Dispose of ESAPI application object
        ///// </summary>
        //public void DisposeApplication()
        //{
        //    _Application.Dispose();
        //}
        /// <summary>
        /// Convert JSON template into a TemplateObject.
        /// </summary>
        /// <param name="fileName">Full file name for scorecard template</param>
        /// <returns>Internal Template Model</returns>
        /// <exception cref="ApplicationException"></exception>
        public InternalTemplateModel LoadScorecardFromFile(string fileName, out string returnDetails)
        {
            try
            {
                InternalTemplateModel template = JsonConvert.DeserializeObject<InternalTemplateModel>(File.ReadAllText(fileName));
                returnDetails = "Success";
                return template;
            }
            catch (Exception ex)
            {
                returnDetails = ex.Message;
                return null;
                //throw new ApplicationException(ex.Message);
            }
        }
        /// <summary>
        /// Finds PlanModel object from JSON file
        /// </summary>
        /// <param name="fileName">File contains Plan Id, Course Id, and Patient Id</param>
        /// <param name="returnDetails">String to help user with debugging. </param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public List<PlanModel> GetPlanFromFile(string fileName, out string returnDetails)
        {
            try
            {
                var plan = JsonConvert.DeserializeObject<List<PatientPlanModel>>(File.ReadAllText(fileName));
                List<PlanModel> planModels = new List<PlanModel>();
                foreach (var patientPlanModel in plan)
                {
                    _Application.ClosePatient();
                    var patient = _Application.OpenPatientById(patientPlanModel.PatientId);
                    if (patient == null) { returnDetails = $"Could not find patient with Id: {patientPlanModel.PatientId}"; return null; }
                    var course = patient.Courses.FirstOrDefault(c => c.Id.Equals(patientPlanModel.CourseId));
                    if (course == null) { returnDetails = $"Could not find course with Id: {patientPlanModel.CourseId}"; return null; }
                    var planSetup = course.PlanSetups.FirstOrDefault(ps => ps.Id.Equals(patientPlanModel.PlanId));
                    if (planSetup == null) { returnDetails = $"Could not find plan with Id: {patientPlanModel.PlanId}"; return null; }
                    PlanModel planModel = new PlanModel(planSetup);
                    planModels.Add(planModel);
                }
                returnDetails = "Success";
                return planModels;
            }
            catch (Exception e)
            {
                returnDetails = e.Message;
                return null;
                //throw new ApplicationException(e.Message);
            }
        }
        public List<PlanScoreModel> ScorePlan(PlanModel plan, InternalTemplateModel scorecard, bool canBuildStructure, out string output)
        {
            //get plan setup from plan.
            _Application.ClosePatient();
            Patient patient = _Application.OpenPatientById(plan.PatientId);
            if(patient == null) { output = $"No patient with Id {plan.PatientId}";return null; }
            Course course = patient.Courses.FirstOrDefault(c => c.Id.Equals(plan.CourseId));
            if(course == null) { output = $"No course with Id {plan.CourseId}"; return null; }
            PlanSetup planSetup = course.PlanSetups.FirstOrDefault(ps => ps.Id.Equals(plan.PlanId));
            if(plan == null) { output = $"No Plan with Id {plan.PlanId}"; return null; }
            List<PlanScoreModel> planScores = new List<PlanScoreModel>();
            int metricId = 0;
            foreach(var metric in scorecard.ScoreTemplates)
            {
                PlanScoreModel psm = new PlanScoreModel(_Application);
                psm.InputTemplate(metric);
                psm.BuildPlanScoreFromTemplate(planSetup, metric, metricId, canBuildStructure);
                planScores.Add(psm);
                metricId++;
            }
            output = "Success";
            return planScores;
        }
        public List<PlanScoreModel> ScorePlan(PlanSetup plan, InternalTemplateModel scorecard, bool canBuildStructure, out string output)
        {
            //get plan setup from plan.
            List<PlanScoreModel> planScores = new List<PlanScoreModel>();
            int metricId = 0;
            foreach (var metric in scorecard.ScoreTemplates)
            {
                PlanScoreModel psm = new PlanScoreModel(_Application);
                psm.InputTemplate(metric);
                psm.BuildPlanScoreFromTemplate(plan, metric, metricId, canBuildStructure);
                planScores.Add(psm);
                metricId++;
            }
            output = "Success";
            return planScores;
        }
    }
}
