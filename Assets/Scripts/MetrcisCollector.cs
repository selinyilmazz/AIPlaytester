using System.Collections.Generic;

public class MetricsCollector
{
    public MetricsSummary Summarize(List<TestResult> results)
    {
        MetricsSummary summary = new MetricsSummary();
        summary.TotalTests = results.Count;

        int totalSteps = 0;
        float totalTime = 0f;
        int totalWrongMoves = 0;
        int minSteps = int.MaxValue;
        int maxSteps = int.MinValue;

        foreach (TestResult result in results)
        {
            totalTime += result.TimeTaken;
            totalWrongMoves += result.WrongMoves;

            if (result.Success)
            {
                summary.SuccessCount++;
                totalSteps += result.Steps;

                if (result.Steps < minSteps)
                {
                    minSteps = result.Steps;
                }

                if (result.Steps > maxSteps)
                {
                    maxSteps = result.Steps;
                }
            }
            else
            {
                summary.FailCount++;
            }
        }

        summary.SuccessRate = summary.TotalTests > 0 ? (float)summary.SuccessCount / summary.TotalTests * 100f : 0f;
        summary.AverageSteps = summary.SuccessCount > 0 ? (float)totalSteps / summary.SuccessCount : 0f;
        summary.MinSteps = summary.SuccessCount > 0 ? minSteps : 0;
        summary.MaxSteps = summary.SuccessCount > 0 ? maxSteps : 0;
        summary.AverageTime = summary.TotalTests > 0 ? totalTime / summary.TotalTests : 0f;
        summary.AverageWrongMoves = summary.TotalTests > 0 ? (float)totalWrongMoves / summary.TotalTests : 0f;

        return summary;
    }
}