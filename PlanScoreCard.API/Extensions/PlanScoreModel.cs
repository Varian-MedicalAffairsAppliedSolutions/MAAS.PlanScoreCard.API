﻿using PlanScoreCard.API.Models.ScoreCard;
using PlanScoreCard.API.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanScoreCard.API.Extensions
{
    public class PlanScoreModel
    {
        private VMS.TPS.Common.Model.API.Application _app;
        public string StructureId { get; set; }
        public string StructureComment { get; set; }
        public string TemplateStructureId { get; set; }
        public string MetricText { get; set; }
        public double ScoreMax { get; set; }
        public string MetricComment { get; set; }
        public string PrintComment { get; set; }
        public int MetricId { get; set; }
        public ScoreTemplateModel InternalTemplate { get; set; }
        public ObservableCollection<ScoreValueModel> ScoreValues { get; set; }
        public PlanScoreModel(VMS.TPS.Common.Model.API.Application app)
        {
            _app = app;
            ScoreValues = new ObservableCollection<ScoreValueModel>();
        }
        public void InputTemplate(ScoreTemplateModel template)
        {
            MetricText = PlanScoreCalculationServices.GetMetricTextFromTemplate(template);
        }
        public void BuildPlanScoreFromTemplate(PlanSetup plan, ScoreTemplateModel template, int metricId, bool canBuildStructure)
        {
            var _dvhResolution = 0.01;
            ScoreMax = template.ScorePoints.Count() == 0 ? -1000 : template.ScorePoints.Max(x => x.Score);
            string id = template.Structure?.StructureId;
            string code = template.Structure?.StructureCode;
            //check manual match ID.
            string matchId = String.Empty;
            if (String.IsNullOrWhiteSpace(template.Structure?.TemplateStructureId))
            {
                TemplateStructureId = id;
            }
            else
            {
                TemplateStructureId = template.Structure.TemplateStructureId;
            }
            if (!String.IsNullOrEmpty(TemplateStructureId))
            {
                template.Structure.TemplateStructureId = TemplateStructureId;
            }
            string templateId = template.Structure?.TemplateStructureId;
            bool auto = template.Structure == null ? false : template.Structure.AutoGenerated;
            string comment = template.Structure?.StructureComment;
            //check for matched ID
            if (template.Structure.MatchedStructure != null)
            {
                matchId = template.Structure.MatchedStructure.StructureId;
            }
            //find out if value is increasing. 
            bool increasing = false;
            if (template.ScorePoints.Count() > 0)
            {
                increasing = template.ScorePoints.ElementAt(
                  Array.IndexOf(template.ScorePoints.Select(x => x.PointX).ToArray(),
                  template.ScorePoints.Min(x => x.PointX))).Score <
                  template.ScorePoints.ElementAt(
                      Array.IndexOf(template.ScorePoints.Select(x => x.PointX).ToArray(),
                      template.ScorePoints.Max(x => x.PointX))).Score;
            }

            //MetricText = PlanScoreCalculationServices.GetMetricTextFromTemplate(template);
            //SetInitialPlotParameters(template);
            MetricId = metricId;
            MetricComment = template.MetricComment;
            // The id and the code are from the template Structure
            Structure structure = String.IsNullOrEmpty(id) && String.IsNullOrEmpty(templateId) ? null : GetStructureFromTemplate(matchId, id, templateId, code, auto, comment, plan, canBuildStructure);
            if (structure != null)
            {
                template.Structure.StructureId = structure.Id;
            }
            ScoreValueModel scoreValue = new ScoreValueModel();
            //scoreValue.bVisible = true;
            //added variation to scorevalue. 
            scoreValue.Variation = template.ScorePoints.Any(sp=>sp.Variation)?template.ScorePoints.FirstOrDefault(sp=>sp.Variation).Score:0.0;
            scoreValue.OutputUnit = template.OutputUnit;
            scoreValue.PlanId = plan.Id;

            scoreValue.CourseId = (plan as PlanSetup).Course.Id;
            scoreValue.PatientId = (plan as PlanSetup).Course.Patient.Id;

            StructureId = structure == null ? " - " : structure.Id;
            StructureComment = structure == null ? " - " : comment;
            TemplateStructureId = templateId;
            if (structure != null && plan.Dose != null && !structure.IsEmpty)
            {
                if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.DoseAtVolume)
                {
                    var dvh = plan.GetDVHCumulativeData(structure,
                        template.OutputUnit.Contains("%") ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                        template.InputUnit.Contains("%") ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                        _dvhResolution);
                    if (dvh != null)
                    {
                        if (Math.Abs(template.InputValue - 100.0) < 0.001) { scoreValue.Value = dvh.MinDose.Dose; }
                        else if (Math.Abs(template.InputValue - 0.0) < 0.001) { scoreValue.Value = dvh.MaxDose.Dose; }
                        else
                        {
                            scoreValue.Value = dvh.CurveData.FirstOrDefault(x => x.Volume <= template.InputValue + 0.001).DoseValue.Dose;
                        }
                        if (template.OutputUnit != dvh.MaxDose.UnitAsString)
                        {
                            if (template.OutputUnit == "Gy") { scoreValue.Value = scoreValue.Value / 100.0; }
                            else { scoreValue.Value = scoreValue.Value * 100.0; }
                        }
                    }
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.VolumeAtDose)
                {
                    DVHData dvh = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, structure, _dvhResolution);
                    if (template.InputUnit != dvh.MaxDose.UnitAsString)
                    {
                        if (template.InputUnit == "Gy")
                        {
                            scoreValue.Value = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue * 100.0).Volume;
                        }
                        else
                        {
                            scoreValue.Value = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue / 100.0).Volume;
                        }
                    }
                    else
                    {
                        scoreValue.Value = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue).Volume;
                    }
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.VolumeOfRegret)
                {
                    var body = plan.StructureSet.Structures.SingleOrDefault(x => x.DicomType == "EXTERNAL");
                    if (body == null)
                    {
                        //System.Windows.MessageBox.Show("No Single Body Structure Found");
                        scoreValue.Value = ScoreMax = scoreValue.Score = -1000;
                        return;
                    }
                    var dvh_body = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, body, _dvhResolution);
                    var dvh = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, structure, _dvhResolution);
                    if (template.InputUnit != dvh.MaxDose.UnitAsString)
                    {
                        if (template.InputUnit == "Gy")
                        {
                            var body_vol = dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue * 100).Volume;
                            var target_vol = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue * 100.0).Volume;
                            scoreValue.Value = body_vol - target_vol;
                        }
                        else
                        {
                            var body_vol = dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue / 100.0).Volume;
                            var target_vol = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue / 100.0).Volume;
                            scoreValue.Value = body_vol - target_vol;
                        }
                    }
                    else
                    {
                        var body_vol = dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue).Volume;
                        var target_vol = dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue).Volume;
                        scoreValue.Value = body_vol - target_vol;
                    }
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ConformationNumber)
                {
                    //conformation number is (volume at given dose)^2/(total volume @ dose * total target volume)
                    var body = plan.StructureSet.Structures.SingleOrDefault(x => x.DicomType == "EXTERNAL");
                    if (body == null)
                    {
                        //System.Windows.MessageBox.Show("No Single Body Structure Found");
                        scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return;
                    }
                    var dvh_body = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, body, _dvhResolution);
                    var dvh = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, structure, _dvhResolution);
                    var body_vol = 0.0;
                    var target_vol = 0.0;
                    PlanScoreCalculationServices.GetVolumesFromDVH(template, dvh_body, dvh, out body_vol, out target_vol);
                    if (body_vol == 0 || dvh.CurveData.Max(cd => cd.Volume) == 0 || target_vol == 0)
                    {
                        scoreValue.Value = -1000;
                    }
                    else
                    {
                        scoreValue.Value = Math.Pow(target_vol, 2) / (body_vol * dvh.CurveData.Max(x => x.Volume));
                    }

                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.HomogeneityIndex)
                {
                    if (plan is PlanSetup)
                    {
                        var dTarget = template.HI_Target;
                        if (template.HI_Target != 0.0 && template.HI_TargetUnit != "%")
                        {
                            if (template.HI_TargetUnit != (plan as PlanSetup).TotalDose.UnitAsString)
                            {
                                if ((plan as PlanSetup).TotalDose.UnitAsString.Contains('c'))
                                {
                                    //this means templat is in Gy and dose in in cGy
                                    dTarget = template.HI_Target * 100.0;
                                }
                                else
                                {
                                    //plan is in Gy and template is in cgy. 
                                    dTarget = template.HI_Target / 100.0;
                                }
                            }
                            else
                            {
                                dTarget = template.HI_Target;
                            }
                        }
                        var dvh = template.HI_TargetUnit == "%" ?
                            plan.GetDVHCumulativeData(structure, DoseValuePresentation.Relative, VolumePresentation.Relative, _dvhResolution)
                            : plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute,
                            VolumePresentation.Relative, _dvhResolution);
                        if (dvh == null) { scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return; }
                        var h_val = template.HI_HiValue;// * dTarget / 100.0;
                        var l_val = template.HI_LowValue;// * dTarget / 100.0;

                        var dHi = dvh.CurveData.FirstOrDefault(x => x.Volume <= h_val).DoseValue.Dose;
                        var dLo = dvh.CurveData.FirstOrDefault(x => x.Volume <= l_val).DoseValue.Dose;
                        //the target dose level has already been converted to the system's dose unit and therefore dHi and dLo do not need to be converted.
                        //if (template.HI_TargetUnit != (plan as PlanSetup).TotalDose.UnitAsString)
                        //{
                        //    if ((plan as PlanSetup).TotalDose.UnitAsString.Contains('c'))
                        //    {
                        //        dHi = dHi* 100.0;
                        //        dLo = dLo* 100.0;
                        //    }
                        //    else
                        //    {
                        //        dHi= dHi / 100.0;
                        //        dLo = dLo / 100.0;
                        //    }
                        //}
                        scoreValue.Value = template.OutputUnit == "%" ? (dHi - dLo) / (dTarget - (plan as PlanSetup).TotalDose.Dose) : (dHi - dLo) / dTarget;
                    }
                    else
                    {
                        //HI not yet supported for plansums.
                        scoreValue.Value = -1000;
                    }
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ConformityIndex)
                {
                    var body = plan.StructureSet.Structures.SingleOrDefault(x => x.DicomType == "EXTERNAL");
                    if (body == null)
                    {
                        //System.Windows.MessageBox.Show("No single body structure found.");
                        scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return;
                    }
                    //goahead and make the DVH absolute volume for conformity index (not saved in template). 
                    template.OutputUnit = "cc";
                    var dvh_body = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, body, _dvhResolution);
                    var dvh = PlanScoreCalculationServices.GetDVHForVolumeType(plan, template, structure, _dvhResolution);
                    var body_vol = 0.0;
                    var target_vol = 0.0;
                    PlanScoreCalculationServices.GetVolumesFromDVH(template, dvh_body, dvh, out body_vol, out target_vol);
                    scoreValue.Value = body_vol / structure.Volume;
                    template.OutputUnit = String.Empty;

                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.InhomogeneityIndex)
                {
                    var dvh = plan.GetDVHCumulativeData(structure, DoseValuePresentation.Absolute,
                        VolumePresentation.Relative, _dvhResolution);
                    if (dvh == null) { scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return; }
                    scoreValue.Value = (dvh.MaxDose.Dose - dvh.MinDose.Dose) / dvh.MeanDose.Dose;
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ModifiedGradientIndex)
                {
                    //plan.DoseValuePresentation = DoseValuePresentation.Absolute;
                    //var doseUnit = plan.Dose.DoseMax3D.UnitAsString;
                    var dhi = template.HI_HiValue;
                    var dlo = template.HI_LowValue;
                    var unit = template.InputUnit;
                    var dvh = plan.GetDVHCumulativeData(structure,
                        unit.Contains("%") ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                        VolumePresentation.AbsoluteCm3,
                        _dvhResolution);
                    if (dvh == null) { scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return; }
                    var doseUnit = dvh.MaxDose.UnitAsString;
                    if (unit != doseUnit)
                    {
                        if (unit.StartsWith("c"))
                        {
                            //unit is cGy and system unit is in Gy.
                            dhi = dhi / 100.0;
                            dlo = dlo / 100.0;
                        }
                        else
                        {
                            //unit is in Gy and system unit is in cGy
                            dhi = dhi * 100.0;
                            dlo = dlo * 100.0;
                        }
                    }
                    var vDLo = dvh.CurveData.FirstOrDefault(x => x.DoseValue.Dose >= dlo).Volume;
                    var vDHi = dvh.CurveData.FirstOrDefault(x => x.DoseValue.Dose >= dhi).Volume;
                    scoreValue.Value = vDLo / vDHi;
                }
                else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.DoseAtSubVolume)
                {
                    var specVolume = template.InputValue;
                    var structureVolume = structure.Volume;
                    var unit = template.OutputUnit;
                    var dvh = plan.GetDVHCumulativeData(structure,
                        unit.Contains("%") ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                        VolumePresentation.AbsoluteCm3, _dvhResolution);
                    if (dvh == null) { scoreValue.Value = ScoreMax = scoreValue.Score = -1000; return; }
                    var doseValue = dvh.CurveData.FirstOrDefault(x => x.Volume <= structureVolume - specVolume).DoseValue;
                    var doseUnit = doseValue.UnitAsString;
                    scoreValue.Value = doseValue.Dose;
                    if (unit != doseUnit)
                    {
                        if (unit.StartsWith("c"))
                        {
                            //unit is in cGy and dvh is in Gy
                            scoreValue.Value = doseValue.Dose * 100.0;
                        }
                        else
                        {
                            scoreValue.Value = doseValue.Dose / 100.0;
                        }
                    }

                }
                else
                {
                    if (String.IsNullOrEmpty(template.OutputUnit))
                    {
                        //MessageBox.Show($"No output unit for metric {template.MetricType} on {template.Structure.StructureId}");
                        scoreValue.Value = -1000;
                    }
                    else
                    {
                        var dvh = plan.GetDVHCumulativeData(structure,
                            template.OutputUnit.Contains("%") ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                            VolumePresentation.Relative,
                            _dvhResolution);
                        if (template.MetricType.Contains("Min"))
                        {
                            scoreValue.Value = dvh.MinDose.Dose;
                        }
                        else if (template.MetricType.Contains("Max"))
                        {
                            scoreValue.Value = dvh.MaxDose.Dose;
                        }
                        else if (template.MetricType.Contains("Mean"))
                        {
                            scoreValue.Value = dvh.MeanDose.Dose;
                        }
                        if (template.OutputUnit != dvh.MaxDose.UnitAsString)
                        {
                            if (template.OutputUnit == "Gy") { scoreValue.Value = scoreValue.Value / 100.0; }
                            else { scoreValue.Value = scoreValue.Value * 100.0; }
                        }
                    }
                }
                if (template.ScorePoints.Any())
                {
                    scoreValue.Score = PlanScoreCalculationServices.GetScore(template.ScorePoints, increasing, scoreValue.Value);
                }
                else { scoreValue.Score = -1000; }
            }
            else
            {
                scoreValue.Score = 0.0;
                scoreValue.Value = -1000;
            }
            scoreValue.StructureId = StructureId;
            ScoreValues.Add(scoreValue);

        }
        /// <summary>
        /// Find structure based on templated structure model
        /// </summary>
        /// <param name="id">Structure Id</param>
        /// <param name="code">Structure code</param>
        /// <param name="autoGenerate">Reference to whether the structure is meant to be generated automatically</param>
        /// <param name="comment">Structure Comment that details how to generate the structure.</param>
        /// <param name="plan">Plan whereby to look for the structure.</param>
        /// <returns></returns>
        public Structure GetStructureFromTemplate(string matchedId, string id, string templateId, string code, bool autoGenerate, string comment, PlanningItem plan, bool canBuildStructure)
        {
            // This method is where we will want to add the logic to the Structure Matching w/ Dictionary
            // - Case Insensitive (Overwrite string.Compare() to automatically do this)
            bool writeable = canBuildStructure;
            if (String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(templateId))
            {
                id = templateId;
            }
            // FIRST: Check for an exact Match --> But matchId must not exist. 
            if (id != null && code != null && String.IsNullOrEmpty(matchedId))
            {
                foreach (var s in plan.StructureSet.Structures)
                {
                    //match on code and id, then on code, then on id.
                    if (s.StructureCodeInfos != null && s.StructureCodeInfos.FirstOrDefault().Code == code && s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    {
                        return s;
                    }
                }
            }
            // Check for structure existence
            if (plan.StructureSet.Structures.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {

                var structure = plan.StructureSet.Structures.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (structure != null && !structure.IsEmpty)
                {
                    return structure;
                }
                else if (structure.IsEmpty && autoGenerate && writeable && canBuildStructure)//generate structure if empty.
                {
                    var new_structure = StructureGenerationService.BuildStructureWithESAPI(_app, structure.Id, comment, true, plan, canBuildStructure);
                    return new_structure;
                }
                else//return empty structure. 
                {
                    //what do do with an empty structure.
                    return structure;
                }

            }//If no structure found, try to find structure based on code.
            //next check for structure on templateID.
            if (plan.StructureSet.Structures.Any(x => x.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase)))
            {
                //bFromTemplate = true;
                var structure = plan.StructureSet.Structures.FirstOrDefault(x => x.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase));

                if (structure != null && !structure.IsEmpty)
                {

                    return structure;
                }
                else if (structure.IsEmpty && autoGenerate && writeable && canBuildStructure)//generate structure if empty.
                {
                    var new_structure = StructureGenerationService.BuildStructureWithESAPI(_app, structure.Id, comment, true, plan, canBuildStructure);
                    return new_structure;
                }
                else//return empty structure. 
                {
                    //what do do with an empty structure.
                    return structure;
                }

            }
            // See if you can find it based on just stucture Code
            if (code != null && code.ToLower() != "control" && code.ToLower() != "ptv" && code.ToLower() != "ctv" && code.ToLower() != "gtv")//do not try to match control structures, they will be mismatched
            {
                if (plan.StructureSet.Structures.Where(x => x.StructureCodeInfos.Any()).Any(y => y.StructureCodeInfos.FirstOrDefault().Code == code) && !autoGenerate)
                {
                    return plan.StructureSet.Structures.Where(x => x.StructureCodeInfos.Any()).FirstOrDefault(x => x.StructureCodeInfos.FirstOrDefault().Code == code);
                }
            }

            // If no match, create it. 
            if (autoGenerate && writeable && !String.IsNullOrEmpty(comment) && canBuildStructure)
            {
                var structure = StructureGenerationService.BuildStructureWithESAPI(_app, id, comment, false, plan, canBuildStructure);
                return structure;
            }

            //if (plan.StructureSet.Structures.Where(x => x.StructureCodeInfos.Any()).Any(y => y.StructureCodeInfos.FirstOrDefault().Code == code) && !autoGenerate)
            //{
            //    return plan.StructureSet.Structures.Where(x => x.StructureCodeInfos.Any()).FirstOrDefault(x => x.StructureCodeInfos.FirstOrDefault().Code == code);
            //}
            //else
            //{//if structure doesn't exist, create it. 
            //    if (autoGenerate && writeable)
            //    {
            //        var structure = StructureGenerationService.BuildStructureWithESAPI(_app, id, comment, false, plan);
            //        return structure;
            //    }
            //    return null;
            //}

            // No match at all.
            return null;

        }

    }
}
