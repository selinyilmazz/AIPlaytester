using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSimBrain : MonoBehaviour
{
    public LevelScanner scanner;
    public int maxSteps = 10;
    public float stepDelay = 1f;
    public float timeoutSeconds = 10f;
    public float simulationSpeed = 1f;

    public PlayerProfileType profileType = PlayerProfileType.Normal;

    private enum BotState { Idle, Scanning, Planning, Executing, Evaluating, Finished }

    private BotState currentState;
    private int stepCount;
    private int wrongMoves;
    private float startTime;
    private GameState currentGameState;
    private IInteractable chosenAction;
    private string chosenActionId;
    private int interactableCountBeforeAction;
    private LevelStatus statusBeforeAction;
    private string stateSignatureBeforeAction;
    private float? distanceToGoalBeforeAction;

    private PlayerMemory memory = new PlayerMemory();
    private IPlayerProfile profile;

    private TestOutcome finishOutcome;
    private List<ActionTraceEntry> trace;

    // GENERIC FRAMEWORK - ADIM 5/6 (minimum wiring): sahnede IGameStateReader+
    // IGameActionExecutor implement eden bir adapter varsa (bkz. ResolveAdapterIfNeeded,
    // artik somut PuzzleGameAdapter tipine bagli degil), state okuma/aksiyon kesfi/
    // aksiyon uygulama bu iki arayuz uzerinden yapilir. Yoksa (adapterLookupDone true
    // ama stateReader/actionExecutor null kaldiysa) asagidaki her kullanim noktasi
    // ESKI scanner/BuildCandidates()/Interact() mantigina AYNEN duser - davranis sifir
    // degisir. Yeni bir interface/adapter OLUSTURULMADI; sadece zaten var olan
    // IGameStateReader/IGameActionExecutor kullanildi.
    private IGameStateReader stateReader;
    private IGameActionExecutor actionExecutor;
    private bool adapterLookupDone;

    public IEnumerator RunTest(Action<TestResult> onComplete)
    {
        ResolveAdapterIfNeeded();

        stepCount = 0;
        wrongMoves = 0;
        currentState = BotState.Idle;
        startTime = Time.time;
        Time.timeScale = simulationSpeed;
        profile = CreateProfile(profileType);
        finishOutcome = TestOutcome.Error;
        trace = new List<ActionTraceEntry>();

        yield return StartCoroutine(RunLoop());

        Time.timeScale = 1f;

        TestResult result = new TestResult();
        result.Success = currentGameState.Status == LevelStatus.Completed;
        result.Steps = stepCount;
        result.TimeTaken = Time.time - startTime;
        result.WrongMoves = wrongMoves;
        result.Outcome = finishOutcome;
        result.Trace = trace;

        onComplete?.Invoke(result);
    }

    private IEnumerator RunLoop()
    {
        while (currentState != BotState.Finished)
        {
            switch (currentState)
            {
                case BotState.Idle:
                    currentState = BotState.Scanning;
                    break;

                case BotState.Scanning:
                    currentGameState = stateReader != null ? stateReader.GetGameState() : scanner.GetCurrentState();
                    currentState = BotState.Planning;
                    break;

                case BotState.Planning:
                    if (currentGameState.Status != LevelStatus.InProgress)
                    {
                        finishOutcome = currentGameState.Status == LevelStatus.Completed ? TestOutcome.Success : TestOutcome.HazardFailure;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (stepCount >= maxSteps)
                    {
                        finishOutcome = TestOutcome.MaxStepsExceeded;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (Time.time - startTime > timeoutSeconds)
                    {
                        Debug.Log(
                            $"PlayerSimBrain TIMEOUT -> " +
                            $"Elapsed: {Time.time - startTime:F2}s, " +
                            $"Step: {stepCount}, " +
                            $"State: {currentState}, " +
                            $"Status: {currentGameState.Status}"
                        );

                        finishOutcome = TestOutcome.Timeout;
                        currentState = BotState.Finished;
                        break;
                    }

                    if (currentGameState.Interactables.Count == 0)
                    {
                        finishOutcome = TestOutcome.Error;
                        currentState = BotState.Finished;
                        break;
                    }

                    List<ActionCandidate> availableActions = actionExecutor != null ? actionExecutor.GetAvailableActions() : BuildCandidates(currentGameState);
                    ActionCandidate chosenCandidate = profile.ChooseAction(availableActions, memory);
                    chosenAction = chosenCandidate.Interactable;
                    chosenActionId = chosenCandidate.ActionId;
                    interactableCountBeforeAction = currentGameState.Interactables.Count;
                    statusBeforeAction = currentGameState.Status;
                    stateSignatureBeforeAction = currentGameState.StateSignature;
                    distanceToGoalBeforeAction = currentGameState.DistanceToGoal;

                    float thinkingDelay = profile.GetThinkingDelay();
                    if (thinkingDelay > 0f)
                    {
                        yield return new WaitForSeconds(thinkingDelay);
                    }

                    currentState = BotState.Executing;
                    break;

                case BotState.Executing:
                    yield return new WaitForSeconds(stepDelay);

                    if (actionExecutor != null)
                    {
                        actionExecutor.Execute(new ActionCandidate(chosenAction, chosenActionId));
                    }
                    else
                    {
                        chosenAction.Interact();
                    }
                    stepCount++;
                    currentState = BotState.Evaluating;
                    break;

                case BotState.Evaluating:
                    currentGameState = stateReader != null ? stateReader.GetGameState() : scanner.GetCurrentState();

                    // GENERIC FRAMEWORK - ADIM 9: eski Count/Status kontrolu AYNEN korunuyor,
                    // uzerine GameState.StateSignature bazli bir ucuncu OR kosulu ekleniyor.
                    // signatureChanged sadece HER IKI taraf da (before/after) dolu VE
                    // farkliysa true olur - adapter StateSignature doldurmuyorsa (bugunku
                    // PuzzleGameAdapter/LevelScanner yolu) bu kosul hep false kalir ve
                    // wasEffective TAMAMEN eski mantiga (countChanged || statusChanged) esdeger
                    // olur - davranis sifir degisir. CounterGame'e (veya baska bir adapter'a)
                    // ozel hicbir tip/isim kontrolu yok.
                    bool signatureChanged =
                        !string.IsNullOrEmpty(stateSignatureBeforeAction) &&
                        !string.IsNullOrEmpty(currentGameState.StateSignature) &&
                        stateSignatureBeforeAction != currentGameState.StateSignature;

                    bool countChanged =
                        currentGameState.Interactables.Count != interactableCountBeforeAction;

                    bool statusChanged =
                        currentGameState.Status != statusBeforeAction;

                    bool wasEffective = signatureChanged || countChanged || statusChanged;

                    bool wasSafe = currentGameState.Status != LevelStatus.Failed;

                    // GENERIC FRAMEWORK - ADIM 11: wasEffective/wasSafe'e HICBIR sekilde
                    // dokunulmadi (yukaridaki iki satir AYNEN duruyor) - progress tamamen
                    // BAGIMSIZ, ucuncu bir sinif. distanceToGoalBeforeAction veya
                    // currentGameState.DistanceToGoal null ise (adapter doldurmuyorsa,
                    // bugunku PuzzleGameAdapter/LevelScanner yolu dahil) Unknown kalir -
                    // asagidaki mantik hicbir CounterGame'e ozel isim/tip kontrolu icermiyor.
                    ProgressClassification progress;
                    if (!distanceToGoalBeforeAction.HasValue || !currentGameState.DistanceToGoal.HasValue)
                    {
                        progress = ProgressClassification.Unknown;
                    }
                    else
                    {
                        float progressDelta = distanceToGoalBeforeAction.Value - currentGameState.DistanceToGoal.Value;
                        if (progressDelta > 0f)
                        {
                            progress = ProgressClassification.Productive;
                        }
                        else if (progressDelta < 0f)
                        {
                            progress = ProgressClassification.Regressive;
                        }
                        else
                        {
                            progress = ProgressClassification.Neutral;
                        }
                    }

                    memory.RecordAttempt(chosenActionId, wasEffective, wasSafe, progress);

                    trace.Add(new ActionTraceEntry
                    {
                        StepIndex = stepCount - 1,
                        ActionId = chosenActionId,
                        WasEffective = wasEffective,
                        InteractableCountBefore = interactableCountBeforeAction,
                        InteractableCountAfter = currentGameState.Interactables.Count,
                        ResultingStatus = currentGameState.Status
                    });

                    if (!wasEffective)
                    {
                        wrongMoves++;
                    }

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

            // GENERIC FRAMEWORK - ADIM 10: burada ONCEDEN, switch disinda ve HER case
            // icin kosulsuz calisan ikinci bir "yield return new WaitForSeconds(stepDelay)"
            // vardi. Executing case'i zaten aksiyonu uygulamadan hemen once KENDI
            // stepDelay beklemesini yapiyor (yukarida) - bu ikisi ust uste binince tek
            // bir aksiyon dongusu (Scanning+Planning+Executing+Evaluating) 4 ayri
            // iterasyonun HER BIRINDE bu bekleme calistigi icin 4x, Executing'in kendi
            // ekstra beklemesiyle birlikte 5x stepDelay'e kadar yigiliyordu - bu da
            // dusuk stepDelay/timeoutSeconds degerlerinde (orn. CounterGame sahnesi)
            // sadece birkac adimda timeout'a sebep oluyordu. Aksiyonlar arasi tek ve
            // tutarli bekleme noktasi ARTIK SADECE Executing case'inin icindeki
            // "yield return new WaitForSeconds(stepDelay)" - bu yuzden burasi kaldirildi.
        }
    }

    // GENERIC FRAMEWORK - ADIM 6: adapter kesfi artik somut PuzzleGameAdapter tipine
    // BAGLI DEGIL. LevelScanner.FindAllInteractables() ve ActionGenerator.GenerateActions()
    // ile AYNI teknik kullanilir - tum sahne MonoBehaviour'lari taranir, hem
    // IGameStateReader hem IGameActionExecutor'i AYNI ANDA implement eden ilk nesne
    // adapter olarak kabul edilir (boylece state okuma ve aksiyon kesfi/uygulama
    // hep TUTARLI, tek bir adapter'dan gelir - PuzzleGameAdapter'da oldugu gibi).
    // Sadece bir kez calisir (adapterLookupDone bayragi ile). Sahnede uygun bir
    // adapter yoksa stateReader/actionExecutor null kalir ve tum cagri noktalari
    // eski scanner/BuildCandidates()/Interact() mantigina duser - davranis degismez.
    private void ResolveAdapterIfNeeded()
    {
        if (adapterLookupDone)
        {
            return;
        }

        adapterLookupDone = true;

        MonoBehaviour[] allObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour obj in allObjects)
        {
            IGameStateReader reader = obj as IGameStateReader;
            IGameActionExecutor executor = obj as IGameActionExecutor;

            if (reader != null && executor != null)
            {
                stateReader = reader;
                actionExecutor = executor;
                Debug.Log("PlayerSimBrain: sahnede IGameStateReader+IGameActionExecutor implement eden bir adapter (" + obj.GetType().Name + ") bulundu, state okuma/aksiyon kesfi/uygulama bu adapter uzerinden yapilacak.");
                break;
            }
        }
    }

    private List<ActionCandidate> BuildCandidates(GameState state)
    {
        List<ActionCandidate> candidates = new List<ActionCandidate>();

        foreach (IInteractable interactable in state.Interactables)
        {
            MonoBehaviour mb = interactable as MonoBehaviour;
            string id = mb != null ? mb.gameObject.name : null;
            candidates.Add(new ActionCandidate(interactable, id));
        }

        return candidates;
    }

    public void ResetMemory()
    {
        memory = new PlayerMemory();
    }

    private IPlayerProfile CreateProfile(PlayerProfileType type)
    {
        switch (type)
        {
            case PlayerProfileType.Careful:
                return new CarefulPlayerProfile();
            case PlayerProfileType.Impulsive:
                return new ImpulsivePlayerProfile();
            default:
                return new NormalPlayerProfile();
        }
    }
}
