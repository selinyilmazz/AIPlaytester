using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotBrain : MonoBehaviour
{
    public LevelScanner scanner;
    public int maxSteps = 10;
    public float stepDelay = 1f;
    public float timeoutSeconds = 10f;
    public float simulationSpeed = 1f;

    private enum BotState { Idle, Scanning, Planning, Executing, Evaluating, Finished }

    private BotState currentState;
    private int stepCount;
    private float startTime;
    private GameState currentGameState;
    private IInteractable chosenAction;
    private string chosenActionId;
    private int interactableCountBeforeAction;
    private LevelStatus statusBeforeAction;

    private List<string> solutionPath;
    private int solutionIndex;

    private TestOutcome finishOutcome;
    private List<ActionTraceEntry> trace;

    public IEnumerator RunTest(Action<TestResult> onComplete, List<string> solution)
    {
        stepCount = 0;
        solutionIndex = 0;
        currentState = BotState.Idle;
        startTime = Time.time;
        Time.timeScale = simulationSpeed;
        solutionPath = solution;
        finishOutcome = TestOutcome.Error;
        trace = new List<ActionTraceEntry>();

        yield return StartCoroutine(RunLoop());

        Time.timeScale = 1f;

        TestResult result = new TestResult();
        result.Success = currentGameState.Status == LevelStatus.Completed;
        result.Steps = stepCount;
        result.TimeTaken = Time.time - startTime;
        result.Outcome = finishOutcome;
        result.Trace = trace;

        onComplete?.Invoke(result);
    }

    private IEnumerator RunLoop()
    {
        yield return new WaitForSeconds(stepDelay);

        while (currentState != BotState.Finished)
        {
            switch (currentState)
            {
                case BotState.Idle:
                    currentState = BotState.Scanning;
                    break;

                case BotState.Scanning:
                    currentGameState = scanner.GetCurrentState();
                    currentState = BotState.Planning;
                    break;

                case BotState.Planning:
                    if (currentGameState.Status != LevelStatus.InProgress)
                    {
                        finishOutcome = currentGameState.Status == LevelStatus.Completed ? TestOutcome.Success : TestOutcome.HazardFailure;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (solutionPath == null)
                    {
                        Debug.Log("BotBrain: Uygulanacak cozum yok.");
                        finishOutcome = TestOutcome.NoSolutionToFollow;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (solutionIndex >= solutionPath.Count)
                    {
                        Debug.Log("BotBrain: Cozum tamamen uygulandi ama level kazanilmadi.");
                        finishOutcome = TestOutcome.Error;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (Time.time - startTime > timeoutSeconds)
                    {
                        Debug.Log("BotBrain: TimeoutSeconds asildi.");
                        finishOutcome = TestOutcome.Timeout;
                        currentState = BotState.Finished;
                        break;
                    }

                    string targetName = solutionPath[solutionIndex];
                    chosenAction = FindInteractableByName(currentGameState, targetName);

                    if (chosenAction == null)
                    {
                        Debug.Log("BotBrain: '" + targetName + "' adinda bir interactable bulunamadi.");
                        finishOutcome = TestOutcome.Error;
                        currentState = BotState.Finished;
                        break;
                    }

                    chosenActionId = targetName;
                    interactableCountBeforeAction = currentGameState.Interactables.Count;
                    statusBeforeAction = currentGameState.Status;

                    currentState = BotState.Executing;
                    break;

                case BotState.Executing:
                    chosenAction.Interact();
                    stepCount++;
                    solutionIndex++;
                    currentState = BotState.Evaluating;
                    break;

                case BotState.Evaluating:
                    currentGameState = scanner.GetCurrentState();

                    bool wasEffective = currentGameState.Interactables.Count != interactableCountBeforeAction
                        || currentGameState.Status != statusBeforeAction;

                    trace.Add(new ActionTraceEntry
                    {
                        StepIndex = stepCount - 1,
                        ActionId = chosenActionId,
                        WasEffective = wasEffective,
                        InteractableCountBefore = interactableCountBeforeAction,
                        InteractableCountAfter = currentGameState.Interactables.Count,
                        ResultingStatus = currentGameState.Status
                    });

                    if (currentGameState.Status == LevelStatus.Completed)
                    {
                        finishOutcome = TestOutcome.Success;
                        currentState = BotState.Finished;
                    }
                    else if (currentGameState.Status == LevelStatus.Failed)
                    {
                        finishOutcome = TestOutcome.HazardFailure;
                        currentState = BotState.Finished;
                    }
                    else
                    {
                        currentState = BotState.Scanning;
                    }
                    break;
            }

            yield return new WaitForSeconds(stepDelay);
        }
    }

    private IInteractable FindInteractableByName(GameState state, string name)
    {
        foreach (IInteractable item in state.Interactables)
        {
            MonoBehaviour mb = item as MonoBehaviour;
            if (mb != null && mb.gameObject.name == name)
            {
                return item;
            }
        }

        return null;
    }
}
