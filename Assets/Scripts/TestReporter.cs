using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestReporter
{
    private string folderPath = Application.dataPath + "/Reports/";

    public void SaveReport(
        string levelName,
        MetricsSummary solverSummary,
        MetricsSummary playerSummary,
        DifficultyReport difficultyReport,
        List<TestResult> solverResults,
        List<TestResult> playerResults,
        string playerProfileName)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        LevelTestReport report = new LevelTestReport();
        report.LevelName = levelName;
        report.Timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        report.PlayerProfile = playerProfileName;

        report.SolverSuccessRate = solverSummary.SuccessRate;
        report.SolverAverageSteps = solverSummary.AverageSteps;

        report.PlayerSuccessRate = playerSummary.SuccessRate;
        report.PlayerAverageSteps = playerSummary.AverageSteps;
        report.PlayerAverageWrongMoves = playerSummary.AverageWrongMoves;

        report.DifficultyScore = difficultyReport.DifficultyScore;
        report.Classification = difficultyReport.Classification;

        report.FailedRunTraces = BuildFailedRunTraces(solverResults, playerResults, playerProfileName);

        string fileNameBase = levelName + "_" + playerProfileName + "_" + report.Timestamp;

        string jsonPath = folderPath + fileNameBase + ".json";
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText(jsonPath, json);

        string txtPath = folderPath + fileNameBase + ".txt";
        string txtContent = BuildTxtContent(report);
        File.WriteAllText(txtPath, txtContent);

        Debug.Log("TestReporter: Rapor kaydedildi -> " + jsonPath);
    }

    public void SaveMultiProfileSummary(MultiProfileSummaryReport summary)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileNameBase = "MultiProfileSummary_" + summary.Timestamp;
        string jsonPath = folderPath + fileNameBase + ".json";
        string json = JsonUtility.ToJson(summary, true);
        File.WriteAllText(jsonPath, json);

        Debug.Log("TestReporter: Multi-profile ozet raporu kaydedildi -> " + jsonPath);
    }

    private List<FailedRunTrace> BuildFailedRunTraces(List<TestResult> solverResults, List<TestResult> playerResults, string playerProfileName)
    {
        List<FailedRunTrace> failedTraces = new List<FailedRunTrace>();

        for (int i = 0; i < solverResults.Count; i++)
        {
            if (solverResults[i].Outcome != TestOutcome.Success)
            {
                FailedRunTrace failedTrace = new FailedRunTrace();
                failedTrace.RunLabel = "Solver_" + i.ToString("00");
                failedTrace.Outcome = solverResults[i].Outcome;
                failedTrace.Trace = solverResults[i].Trace;
                failedTraces.Add(failedTrace);
            }
        }

        for (int i = 0; i < playerResults.Count; i++)
        {
            if (playerResults[i].Outcome != TestOutcome.Success)
            {
                FailedRunTrace failedTrace = new FailedRunTrace();
                failedTrace.RunLabel = "Player_" + playerProfileName + "_" + i.ToString("00");
                failedTrace.Outcome = playerResults[i].Outcome;
                failedTrace.Trace = playerResults[i].Trace;
                failedTraces.Add(failedTrace);
            }
        }

        return failedTraces;
    }

    private string BuildTxtContent(LevelTestReport report)
    {
        string content = "";
        content += "LEVEL: " + report.LevelName + "\n";
        content += "Tarih: " + report.Timestamp + "\n";
        content += "--------------------------------\n";
        content += "Solver Basari Orani: " + report.SolverSuccessRate.ToString("F1") + "%\n";
        content += "Solver Ortalama Adim: " + report.SolverAverageSteps.ToString("F1") + "\n";
        content += "Player Basari Orani: " + report.PlayerSuccessRate.ToString("F1") + "%\n";
        content += "Player Ortalama Adim: " + report.PlayerAverageSteps.ToString("F1") + "\n";
        content += "Player Ortalama Yanlis Hamle: " + report.PlayerAverageWrongMoves.ToString("F1") + "\n";
        content += "--------------------------------\n";
        content += "Difficulty Score: " + report.DifficultyScore.ToString("F1") + " / 10\n";
        content += "Classification: " + report.Classification + "\n";

        return content;
    }
}
