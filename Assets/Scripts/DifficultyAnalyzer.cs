using UnityEngine;

public class DifficultyAnalyzer
{
    public DifficultyReport Analyze(MetricsSummary solverMetrics, MetricsSummary playerMetrics)
    {
        float failureRate = 1f - (playerMetrics.SuccessRate / 100f);
        float wrongMoveFactor = Mathf.Clamp01(playerMetrics.AverageWrongMoves / 3f);
        float solverLengthFactor = Mathf.Clamp01(solverMetrics.AverageSteps / 10f);

        float score = (failureRate * 6f) + (wrongMoveFactor * 2f) + (solverLengthFactor * 2f);

        DifficultyReport report = new DifficultyReport();
        report.DifficultyScore = score;
        report.Classification = Classify(score);

        return report;
    }

    private string Classify(float score)
    {
        if (score <= 3f)
        {
            return "EASY";
        }

        if (score <= 6f)
        {
            return "MEDIUM";
        }

        if (score <= 8f)
        {
            return "HARD";
        }

        return "VERY HARD";
    }
}