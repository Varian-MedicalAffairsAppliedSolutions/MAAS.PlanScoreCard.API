namespace PlanScoreCard.API.Models.ScoreCard
{
    public class ScoreValueModel
    {
        public string PlanId { get; set; }
        public double Value { get; set; }
        public double Score { get; set; }
        public double Variation { get; set; }//wnated the variation in here so I could use it later . 
        public string CourseId { get; set; }
        public string OutputUnit { get; set; }
        public string PatientId { get; set; }
        public string StructureId { get; set; }
    }
}
