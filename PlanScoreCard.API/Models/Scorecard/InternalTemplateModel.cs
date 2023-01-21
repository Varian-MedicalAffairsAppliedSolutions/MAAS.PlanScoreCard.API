using System.Collections.Generic;

namespace PlanScoreCard.API.Models.ScoreCard
{
    public class InternalTemplateModel
    {

        public string TemplateName;
        public string Site;
        public string Creator;
        public List<ScoreTemplateModel> ScoreTemplates;
        public double DosePerFraction;
        public int NumberOfFractions;
        public InternalTemplateModel()
        {
            ScoreTemplates = new List<ScoreTemplateModel>();
        }
    }
}
