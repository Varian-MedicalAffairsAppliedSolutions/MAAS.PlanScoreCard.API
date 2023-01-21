using PlanScoreCard.API.Models.ScoreCard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanScoreCard.API.Services
{
    public static class PlanScoreCalculationServices
    {
        /// <summary>
        /// Find the proper volume from the DVH
        /// </summary>
        /// <param name="template">Scoring Template</param>
        /// <param name="dvh_body">DVH of the body -- used in certain metrics</param>
        /// <param name="dvh">DVH of the structure</param>
        /// <param name="body_vol">The volume of the body DVH at the cutpoint.</param>
        /// <param name="target_vol">The volume of the structure DVH at the cutpoint</param>
        internal static void GetVolumesFromDVH(ScoreTemplateModel template, DVHData dvh_body, DVHData dvh, out double body_vol, out double target_vol)
        {
            if (template.InputUnit != dvh.MaxDose.UnitAsString)
            {
                if (template.InputUnit == "Gy")
                {
                    body_vol = dvh_body.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue * 100) ?
                    dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue * 100).Volume :
                    0.0;
                    target_vol = dvh.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue * 100) ?
                        dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue * 100.0).Volume :
                        0.0;
                }
                else
                {
                    
                    body_vol = dvh_body.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue / 100) ? 
                        dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue / 100.0).Volume:
                    0.0;
                    target_vol = dvh.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue / 100) ? 
                        dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue / 100.0).Volume:
                        0.0;
                }

            }
            else
            {
                body_vol = dvh_body.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue) ? 
                    dvh_body.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue).Volume:
                    0.0;
                target_vol = dvh.CurveData.Any(cd => cd.DoseValue.Dose <= template.InputValue / 100) ? 
                    dvh.CurveData.LastOrDefault(x => x.DoseValue.Dose <= template.InputValue).Volume:
                    0.0;
            }
        }
        /// <summary>
        /// Determing metricText from template
        /// </summary>
        /// <param name="template">Score template</param>
        internal static string GetMetricTextFromTemplate(ScoreTemplateModel template)
        {
            if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.DoseAtVolume)
            {
                return $"Dose at {TruncateLength(template.InputValue)}{template.InputUnit} [{template.OutputUnit}]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.VolumeAtDose)
            {
                return $"Volume at {TruncateLength(template.InputValue)}{template.InputUnit} [{template.OutputUnit}]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.VolumeOfRegret)
            {
                return $"Volume of Regret [{TruncateLength(template.InputValue)}{template.InputUnit}] [{template.OutputUnit}]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ConformationNumber)
            {
                return $"Conformation No. at [{TruncateLength(template.InputValue)}{template.InputUnit}]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.HomogeneityIndex)
            {
                return $"HI [{template.HI_HiValue} - {template.HI_LowValue}]/{TruncateLength(template.HI_Target)}]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.InhomogeneityIndex)
            {
                return "IHI[(Max-Min)/Mean]";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ModifiedGradientIndex)
            {
                return $"Mod GI[V{template.HI_LowValue}/V{template.HI_HiValue}]{template.InputUnit}";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.DoseAtSubVolume)
            {
                return $"D At (V - {TruncateLength(template.InputValue)}CC)";
            }
            else if ((MetricTypeEnum)Enum.Parse(typeof(MetricTypeEnum), template.MetricType) == MetricTypeEnum.ConformityIndex)
            {
                return $"CI [{TruncateLength(template.InputValue)}{template.InputUnit}]";
            }
            else
            {
                return $"{template.MetricType} [{template.OutputUnit}]";
            }
        }
        internal static string TruncateLength(double inputValue)
        {
            if (inputValue.ToString().Length > 8)
            {
                return inputValue.ToString("F2");
            }
            return inputValue.ToString();
        }
        /// <summary>
        /// Determine Volume Type from Templaste
        /// </summary>
        /// <param name="plan">The plan to get the DVH</param>
        /// <param name="template">The template used for calculating DVH</param>
        /// <param name="structure">Structure for the DVH</param>
        /// <returns>The DVH of the structure</returns>
        internal static DVHData GetDVHForVolumeType(PlanningItem plan, ScoreTemplateModel template, Structure structure, double _dvhResolution)
        {
            return plan.GetDVHCumulativeData(structure,
                template.InputUnit.Contains("%") ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                template.OutputUnit.Contains("%") ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
               _dvhResolution);
        }
        /// <summary>
        /// Find score by interpolation
        /// </summary>
        /// <param name="scorePoints">Scoring function.</param>
        /// <param name="increasing">Scoring function increasing or decreasing.</param>
        /// <param name="value">Metric Value</param>
        /// <returns></returns>
        internal static double GetScore(List<ScorePointInternalModel> scorePoints, bool increasing, double value)
        {
            if (scorePoints.Count() == 0) { return 0; }
            else if (scorePoints.Count() == 1) { return scorePoints.First().Score; }
            if (!increasing)
            {
                if (value <= scorePoints.Min(x => x.PointX))
                {
                    return scorePoints.Max(x => x.Score);
                }
                else if (value >= scorePoints.Max(x => x.PointX))
                {
                    return scorePoints.Min(x => x.Score);
                }
                else
                {
                    //linearly interpolate. 
                    var pbefore = scorePoints.OrderBy(x => x.PointX).LastOrDefault(x => x.PointX <= value);
                    var pafter = scorePoints.OrderBy(x => x.PointX).First(x => x.PointX >= value);
                    return (double)pbefore.Score + (value - (double)pbefore.PointX) * (((double)pafter.Score - (double)pbefore.Score) / ((double)pafter.PointX - (double)pbefore.PointX));
                }
            }
            else
            {
                if (value >= scorePoints.Max(x => x.PointX))
                {
                    return scorePoints.Max(x => x.Score);
                }
                else if (value <= scorePoints.Min(x => x.PointX))
                {
                    return scorePoints.Min(x => x.Score);
                }
                else
                {
                    //linearly interpolate. 
                    var pbefore = scorePoints.OrderBy(x => x.PointX).LastOrDefault(x => x.PointX <= value);
                    var pafter = scorePoints.OrderBy(x => x.PointX).First(x => x.PointX >= value);
                    return pbefore.Score + (value - pbefore.PointX) * ((pafter.Score - pbefore.Score) / (pafter.PointX - pbefore.PointX));
                }
            }

        }
    }
}
