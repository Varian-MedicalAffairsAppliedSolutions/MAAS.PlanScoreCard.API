using Newtonsoft.Json;
using PlanScoreCard.API.Extensions;
using PlanScoreCard.API.Models;
using PlanScoreCard.API.Models.ScoreCard;
using PlanScoreCard.Services;
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
        private bool _isWriteable;
        private PlanModel _planModel;
        private InternalTemplateModel _currentTemplate;
        public ScoreCenter(Application application, bool isWriteable)
        {
            _Application = application;
            _isWriteable = isWriteable;
        }
        public void SetPlanModel(PlanSetup plan)
        {
            _planModel = new PlanModel(plan);
        }
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
                _currentTemplate = template;
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
        public List<PlanScoreModel> ScorePlan(bool canBuildStructure, out string output)
        {
            //get plan setup from plan.
            _Application.ClosePatient();
            Patient patient = _Application.OpenPatientById(_planModel.PatientId);
            if (patient == null) { output = $"No patient with Id {_planModel.PatientId}"; return null; }
            Course course = patient.Courses.FirstOrDefault(c => c.Id.Equals(_planModel.CourseId));
            if (course == null) { output = $"No course with Id {_planModel.CourseId}"; return null; }
            PlanSetup planSetup = course.PlanSetups.FirstOrDefault(ps => ps.Id.Equals(_planModel.PlanId));
            if (_planModel == null) { output = $"No Plan with Id {_planModel.PlanId}"; return null; }
            List<PlanScoreModel> planScores = new List<PlanScoreModel>();
            int metricId = 0;
            foreach (var metric in _currentTemplate.ScoreTemplates)
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
        /// <summary>
        /// Score Plan with Plan Setup (good if you don't want to take the step to convert from the Plan Model)
        /// </summary>
        /// <param name="plan">Plan Setup to score</param>
        /// <param name="canBuildStructure">Build optimization structure (requires write-enabled and approval)</param>
        /// <param name="output">Return string</param>
        /// <returns>List of Plan Score Models (scores are in the ScoreValues property</returns>
        public List<PlanScoreModel> ScorePlan(PlanSetup plan, bool canBuildStructure, out string output)
        {
            //get plan setup from plan.
            List<PlanScoreModel> planScores = new List<PlanScoreModel>();
            int metricId = 0;
            foreach (var metric in _currentTemplate.ScoreTemplates)
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
        /// <summary>
        /// Normalize score to achieve maxixmum dose.
        /// </summary>
        /// <param name="normIndex">Allow normalization to score Index parameters (i.e. HI, CI,...). Sometimes poor plans from normalizating to these metrics</param>
        /// <param name="savePlan">Allow new plan to be saved (true). Find normalization but don't actually save plan (false)</param>
        /// <returns>Plan Model of newly created plan.</returns>
        public PlanModel NormPlan(bool normIndex, bool savePlan)
        {
            if (_planModel == null) { Console.WriteLine("No Plan model set"); return null; }
            if (_currentTemplate == null) { Console.WriteLine("No plan template set"); return null; }
            var normService = new NormalizationService(_Application, _planModel, _currentTemplate.ScoreTemplates, normIndex, savePlan);
            var planModel = normService.GetPlan();
            return planModel;
        }
    }
}
