using Newtonsoft.Json;
using PlanScoreCard.API.Models.ScoreCard;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanScoreCard.API.Models
{
    public class PlanModel
    {
        public string PlanId { get; set; }
        public string CourseId { get; set; }
        public string PatientId { get; set; }
        public double PlanScore { get; set; }
        public double MaxScore { get; set; }
        public double DosePerFraction { get; set; }
        public DoseValue.DoseUnit DoseUnit { get; set; }
        public int NumberOfFractions { get; set; }
        public ObservableCollection<StructureModel> Structures { get; set; }
        public PlanModel(PlanSetup plan)
        {
            //Dose per Fraction is always in Gy
            if (plan is PlanSetup)
            {
                //Plan = plan as PlanSetup;
                if ((plan as PlanSetup).TotalDose.Unit == VMS.TPS.Common.Model.Types.DoseValue.DoseUnit.cGy)
                {
                    DosePerFraction = (plan as PlanSetup).DosePerFraction.Dose / 100.0;
                }
                else
                {
                    DosePerFraction = (plan as PlanSetup).DosePerFraction.Dose;
                }
            }
            NumberOfFractions = (plan is PlanSetup) ?
                (plan as PlanSetup)?.NumberOfFractions == null ? 0 : (int)(plan as PlanSetup).NumberOfFractions
                : 0;
            Structures = new ObservableCollection<StructureModel>();
            GenerateStructures(plan);
            SetParameters(plan);
        }
        private void SetParameters(PlanSetup plan)
        {
            PlanId = plan.Id;
            CourseId = plan.Course.Id;
            PatientId = plan.Course.Patient.Id;
            DoseUnit = plan.TotalDose.Unit;
        }

        /// <summary>
        /// Add structures to plan.
        /// </summary>
        private void GenerateStructures(PlanningItem plan)
        {
            foreach (var structure in plan.StructureSet.Structures.Where(x => x.DicomType != "SUPPORT" && x.DicomType != "MARKER"))
            {
                //TODO work on filters for structures
                Structures.Add(new StructureModel()
                {
                    StructureId = structure.Id,
                    StructureCode = structure.StructureCodeInfos.FirstOrDefault().Code,
                    StructureComment = structure.Comment
                });
            }
        }
    }
}
