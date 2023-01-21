using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanScoreCard.API.Models.ScoreCard
{
    public class ScorePointInternalModel
    {
        public ScorePointInternalModel(double pointX, double score, bool variation)
        {
            PointX = pointX;
            Score = score;
            Variation = variation;
        }

        public double PointX { get; set; }
        public double Score { get; set; }
        public bool Variation { get; set; }
    }
}
