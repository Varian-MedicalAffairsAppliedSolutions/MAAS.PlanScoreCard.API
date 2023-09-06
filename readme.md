# PlanScoreCard API Quick Start Guide

## Installing the API

Clone the project by whatever source control means you're currently using.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/35044496-d1e0-4157-8a05-f0b27374dfc2)

Compile the API with the following steps:

- Update your .NET Framework version to match the current version of the Eclipse API you're using.
- The compiler should add the VMS.TPS. ESAPI libraries. If you're using a different version of the API than 15.6, you may need to re-attach these libraries to your current version.
- The compile should install Newtonsoft.Json from nugget.
  ![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/82c4832d-847a-4d14-88c1-25b195e0178a)



## Building the Client

Below shows the use of the plan scorecard API using a stand-alone executable generated from the Eclipse Script Wizard.

1. Generate a stand-alone executable application.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/3f56175b-2dfe-4923-8d97-4c5b772a4ead)

2. Add reference to the ScoreCard.API that was just compiled.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/c7cb0545-4b3b-44b6-8cf3-fae986fa0a01)

3. In the code, first access the intended patient and plan to score.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/caf4fb36-f2b8-43e9-9ae1-6a0b697e552e)

4. Next access import the JSON scorecard template from the scorecard file. The InternalTemplateModel class requires the PlanScoreCard.API.Models.ScoreCard namespace.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/b0da3cc1-83fa-4955-b311-99e8f8b83662)

5. Score the plan. The ScoreCenter class requires the PlanScoreCard.API namespace.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/babe5224-7ce3-41ea-875c-284e0261b97d)

6. Output the results.

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/a19de6e4-66e5-4b16-b29a-112bc55c7f14)

![image](https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS.PlanScoreCard.API/assets/78000769/bf7c9cd4-c8fb-410b-b43a-7f19348716bb)


## Notes on the Scorecard Datamodel

Since the scorecard tool was initially created to score multiple plans and patients, the actual score data ( **ScoreValueModel** ) is a collection within the **PlanScoreModel**. It may be useful to share the properties available to each, but as seen in #6 above, the Score itself must be extracted from the ScoreValues property.

The PlanScoreModel also has some methods to help in scoring the plan.

- InputTemplate – Sets the metric text of the template for each metric. This is an internal method that should be private in an upcoming release.
- BuildPlanScoreFromTemplate – Builds the plan template from plan and template.
- GetStructureFromTemplate - Uses an override id (matchedId) (1) or the template Id (2) to try to find a match for the structure. If the code allows for the automated generation of a structure, then the structure generation service will build the structure using the StructureGenerationService in the API.

<br>
<br>

**PlanScoreModel**

| **Property** | **Description** |
| --- | --- |
| **StructureId** | Id of structure to score |
| **StructureComment** | Comment that can be added to the structure. Can be used to alert user in case a structure is built under some required conditions |
| **TemplateStructureId** | Id of the structure in the template. Can be different than the structure Id. Can be used in a Structure Dictionary setup to help match structures. |
| **MetricText** | Description of Metric. |
| **ScoreMax** | Max Score available for this particular metric |
| **MetricComment** | Comment that can be added to the metric. Used mostly in the UI tool for printing the report. |
| **PrintComment** | Print Comment. Comment that can be set by the user in the UI tool for printing report. |
| **MetricId** | Used in the UI report for ordering the metrics. |
| **ScoreTemplateModel** | JSON template for a score card |
| **ScoreValues** | Collection of score objects |

<br>
<br>

**ScoreValueModel**

| **Property** | Description |
| --- | --- |
| **PlanId** | Id of the plan |
| **Value (double)** | Value of the metric. This is the independent axis on which the Score is derived. It is the value of the metric. |
| **Score (double)** | Score of the current metric. |
| **Variation (double)** | Variation value for the score curve |
| **Courseid** | Id of the course |
| **OutputUnit (string)** | Unit of the Value object. Could be dose unit or volume unit or unitless (empty string) |
| **PatientId** | Id of the patient |
| **StructureId** | The actual Structure id used in the scoring. Could be useful in the case that some logic is applied to extract the appropriate structure |
