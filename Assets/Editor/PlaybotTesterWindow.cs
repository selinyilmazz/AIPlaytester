using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlaybotTesterWindow : EditorWindow
{
    // ---------------------------------------------------------------
    // LAYOUT CONSTANTS
    // ---------------------------------------------------------------

    private const float SidebarWidth = 220f;
    private const float NarrowWindowThreshold = 760f;

    // ---------------------------------------------------------------
    // PALETTE (senior dark dashboard tema)
    // ---------------------------------------------------------------

    // Renk kodlari kullanicinin verdigi referans hex degerlerinden hesaplandi
    // (Background #050914/#080D18, Card #0B1220/#0E1626, Elevated Card #111B2E).
    private static readonly Color ColorBackground = new Color(0.02f, 0.035f, 0.078f);
    private static readonly Color ColorBackgroundAlt = new Color(0.031f, 0.051f, 0.094f);
    private static readonly Color ColorCard = new Color(0.043f, 0.071f, 0.125f);
    private static readonly Color ColorCardAlt = new Color(0.067f, 0.086f, 0.149f);
    private static readonly Color ColorCardElevated = new Color(0.067f, 0.106f, 0.180f);
    private static readonly Color ColorBorder = new Color(0.263f, 0.322f, 0.482f, 0.35f);
    private static readonly Color ColorPrimary = new Color(0.635f, 0.361f, 0.965f);
    private static readonly Color ColorSecondary = new Color(0.216f, 0.545f, 0.965f);
    private static readonly Color ColorSuccess = new Color(0.153f, 0.808f, 0.408f);
    private static readonly Color ColorWarning = new Color(0.976f, 0.616f, 0.114f);
    private static readonly Color ColorDanger = new Color(0.945f, 0.286f, 0.298f);
    private static readonly Color ColorCyan = new Color(0.235f, 0.831f, 0.902f);
    private static readonly Color ColorTextPrimary = new Color(0.918f, 0.929f, 0.965f);
    private static readonly Color ColorTextSecondary = new Color(0.573f, 0.612f, 0.694f);
    private static readonly Color ColorTextMuted = new Color(0.376f, 0.412f, 0.502f);

    private Vector2 scrollPosition;
    private Vector2 matrixScrollPosition;
    private Vector2 resultDetailsScrollPosition;

    [MenuItem("Tools/Playbot Tester")]
    public static void ShowWindow()
    {
        GetWindow<PlaybotTesterWindow>("Playbot Tester");
    }

    private void OnEnable()
    {
        minSize = new Vector2(680f, 420f);
    }

    // ---------------------------------------------------------------
    // ROOT
    // ---------------------------------------------------------------

    private void OnGUI()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ColorBackground);
        DrawAmbientBackground();

        // Not: yatay scrollbar sorununun gercek kok nedeni, kart padding'i (BoxWithPadding,
        // 16+16=32px) dusulmeden ic grid genisliklerine (DrawGrid) tam genislik verilmesiydi.
        // Bu artik DrawProfileComparisonCard/DrawResultDetailsCard icinde duzeltildi. Burada
        // ayrica, dikey scrollbar'in kapladigi alan + genel guvenlik payi cok cömert tutulup,
        // pencerenin gercek genisligini KESINLIKLE asmayacak sekilde mainWidth hesaplanir.
        const float ScrollbarReserve = 26f;
        const float OuterPadding = 18f;
        const float SidebarGap = 16f;

        bool showSidebar = position.width >= NarrowWindowThreshold;
        float mainWidth = showSidebar
            ? Mathf.Max(320f, position.width - SidebarWidth - OuterPadding - ScrollbarReserve - SidebarGap - 12f)
            : Mathf.Max(320f, position.width - OuterPadding - ScrollbarReserve);

        // alwaysShowHorizontal=false: icerik gercekten tastigi durumlarda bile (beklenmeyen bir
        // durum olursa) pencerenin en altinda sabit/gorunur bir yatay scrollbar ZORLANMAZ; sadece
        // dikey scroll her zaman acik kalir (uzun dashboard icerigi icin gerekli).
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

        GUILayout.Space(8);

        if (showSidebar)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            DrawSidebar();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical(GUILayout.Width(mainWidth));
            DrawMainContent(mainWidth);
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical(GUILayout.Width(mainWidth));
            DrawMainContent(mainWidth);
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(8);

        EditorGUILayout.EndScrollView();
    }

    // PREMIUM REDESIGN v2: pencerenin arkasinda, scroll ile HAREKET ETMEYEN (sabit
    // pencere koordinatlarinda), cok dusuk alfali (<=0.06) katmanli daireler ile
    // "ambient radial glow" hissi verir. Bu sadece OnGUI'nin en basinda, arka plan
    // dolgusundan hemen sonra, tum icerik cizilmeden ONCE cagrilir - boylece kartlar
    // her zaman ustte kalir ve okunabilirlik hicbir sekilde etkilenmez. GUILayout'a
    // hicbir genislik/yukseklik talebi girmez (Handles ile dogrudan mutlak
    // koordinatlarda cizilir), bu yuzden scrollbar/layout guvenligini etkilemez.
    private void DrawAmbientBackground()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Handles.BeginGUI();

        Vector3 topLeft = new Vector3(position.width * 0.12f, -30f, 0f);
        for (int i = 5; i >= 1; i--)
        {
            Handles.color = new Color(ColorPrimary.r, ColorPrimary.g, ColorPrimary.b, 0.012f * i);
            Handles.DrawSolidDisc(topLeft, Vector3.forward, 90f + i * 26f);
        }

        Vector3 topRight = new Vector3(position.width * 0.92f, 50f, 0f);
        for (int i = 4; i >= 1; i--)
        {
            Handles.color = new Color(ColorSecondary.r, ColorSecondary.g, ColorSecondary.b, 0.010f * i);
            Handles.DrawSolidDisc(topRight, Vector3.forward, 70f + i * 22f);
        }

        Handles.EndGUI();
    }

    // ---------------------------------------------------------------
    // SIDEBAR
    // ---------------------------------------------------------------

    private void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));

        // ColorBackgroundAlt: sidebar'i ana icerik alanindan hafifce ayirmak icin
        // (referans paletteki Background/Background Alt ayrimi) - sadece dolgu
        // rengi, hicbir layout/veri etkisi yok.
        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = ColorBackgroundAlt;
        Rect sidebarRect = EditorGUILayout.BeginVertical(BoxWithPadding());
        GUI.backgroundColor = previousBg;

        GUILayout.Label("AI PLAYBOT TESTER", SidebarTitleStyle());
        GUILayout.Label("Intelligent Game Testing Platform", SidebarSubtitleStyle());

        GUILayout.Space(12);

        DrawNavItem("Dashboard", true);
        DrawNavItem("Levels", false);
        DrawNavItem("Profiles", false);
        DrawNavItem("Test Runs", false);
        DrawNavItem("Reports", false);
        DrawNavItem("Traces", false);
        DrawNavItem("Settings", false);

        GUILayout.Space(18);

        DrawTestConfigurationCard();
        GUILayout.Space(8);
        DrawEngineStatusCard();
        GUILayout.Space(12);

        GUILayout.Label("AI Playbot Engine v1.0.0", FooterStyle());

        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.Repaint)
        {
            DrawBorder(sidebarRect, ColorBorder, 1f);
        }

        EditorGUILayout.EndVertical();
    }

    // PREMIUM REDESIGN v2: her nav ogesinin basina kucuk, sabit (label'a gore
    // belirlenen) bir geometrik ikon eklendi ve inaktif ogelere mouse-over durumunda
    // cok hafif bir vurgu (hover state) eklendi. "active" durumunun HANGI ogede
    // oldugu (navigasyon davranisi) hicbir sekilde degismedi - hala her zaman
    // "Dashboard" sabit olarak aktif gosteriliyor, tikla-degistir mantigi yok
    // (bu turdan once de yoktu).
    private void DrawNavItem(string label, bool active)
    {
        string content = GetNavIcon(label) + "  " + label;

        if (active)
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(content), ActiveNavStyle(), GUILayout.Height(28), GUILayout.ExpandWidth(true));
            DrawGlow(rect, ColorPrimary, 7, 4f);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = ColorPrimary;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.Box(rect, content, ActiveNavStyle());
            }
            GUI.backgroundColor = previous;
        }
        else
        {
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(content), InactiveNavStyle(), GUILayout.Height(24), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.03f));
                }
                GUI.Label(rect, content, InactiveNavStyle());
            }
        }
    }

    private string GetNavIcon(string label)
    {
        switch (label)
        {
            case "Dashboard":
                return "■";
            case "Levels":
                return "▤";
            case "Profiles":
                return "◆";
            case "Test Runs":
                return "▶";
            case "Reports":
                return "▣";
            case "Traces":
                return "◇";
            case "Settings":
                return "○";
            default:
                return "•";
        }
    }

    private void DrawTestConfigurationCard()
    {
        Rect rect = BeginColoredCard(ColorCardAlt);

        GUILayout.Label("TEST CONFIGURATION", MutedLabelStyle());
        GUILayout.Space(4);

        DrawConfigRow("Runs per Profile", MultiLevelTestRunner.LastPlayerTestCount > 0 ? MultiLevelTestRunner.LastPlayerTestCount.ToString() : "-");
        DrawConfigRow("Profiles", System.Enum.GetValues(typeof(PlayerProfileType)).Length.ToString());
        DrawConfigRow("Simulation Speed", MultiLevelTestRunner.LastSimulationSpeed > 0f ? MultiLevelTestRunner.LastSimulationSpeed.ToString("F1") + "x" : "-");
        DrawConfigRow("Step Delay", MultiLevelTestRunner.LastStepDelay > 0f ? MultiLevelTestRunner.LastStepDelay.ToString("F1") + "s" : "-");
        DrawConfigRow("Timeout", MultiLevelTestRunner.LastTimeoutSeconds > 0f ? MultiLevelTestRunner.LastTimeoutSeconds.ToString("F0") + "s" : "-");

        EndColoredCard(rect);
    }

    private void DrawEngineStatusCard()
    {
        Rect rect = BeginColoredCard(ColorCardAlt);

        GUILayout.Label("ENGINE STATUS", MutedLabelStyle());
        GUILayout.Space(4);

        bool running = MultiLevelTestRunner.IsRunning;
        Color stateColor = running ? ColorWarning : ColorSuccess;
        string stateLabel = running ? "RUNNING" : "READY";

        EditorGUILayout.BeginHorizontal();
        DrawStatusDot(stateColor);
        GUILayout.Label(stateLabel, StatusTextStyle(stateColor));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (MultiLevelTestRunner.LastSolverSummaries.Count > 0)
        {
            DrawConfigRow("Solver", ComputeAverageSolverSuccessRate().ToString("F0") + "%");
        }

        DrawConfigRow("Player Simulator", running ? "Running" : "Idle");

        if (MultiLevelTestRunner.LastSummary != null && !string.IsNullOrEmpty(MultiLevelTestRunner.LastSummary.Timestamp))
        {
            DrawConfigRow("Last Update", MultiLevelTestRunner.LastSummary.Timestamp);
        }

        EndColoredCard(rect);
    }

    private void DrawConfigRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, MutedLabelStyle());
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, ValueLabelStyle());
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusDot(Color color)
    {
        Rect dotRect = GUILayoutUtility.GetRect(9, 9, GUILayout.Width(9), GUILayout.Height(9));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(dotRect, color);
        }
        GUILayout.Space(4);
    }

    // ---------------------------------------------------------------
    // MAIN CONTENT ROOT
    // ---------------------------------------------------------------

    private void DrawMainContent(float mainWidth)
    {
        DrawTopHeader();
        GUILayout.Space(14);

        if (!EditorApplication.isPlaying)
        {
            DrawIdleDashboard();
        }
        else if (MultiLevelTestRunner.IsRunning)
        {
            DrawRunningDashboard();
        }
        else if (MultiLevelTestRunner.AllTestsCompleted)
        {
            DrawCompletedDashboard(mainWidth);
        }
        else
        {
            DrawIdleDashboard();
        }
    }

    // ---------------------------------------------------------------
    // TOP HEADER
    // ---------------------------------------------------------------

    private void DrawTopHeader()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(360f));
        GUILayout.Label("DASHBOARD", PageTitleStyle());
        GUILayout.Label("Overview of your AI playtesting results", PageSubtitleStyle());
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        if (MultiLevelTestRunner.LastSummary != null && !string.IsNullOrEmpty(MultiLevelTestRunner.LastSummary.Timestamp))
        {
            GUILayout.Label("Last Test Run", MutedLabelStyle());
            GUILayout.Label(MultiLevelTestRunner.LastSummary.Timestamp, ValueLabelStyle());
        }
        DrawStatusBadge();
        EditorGUILayout.EndVertical();

        GUILayout.Space(14);

        if (!EditorApplication.isPlaying)
        {
            Rect buttonRect = GUILayoutUtility.GetRect(170, 36, GUILayout.Height(36), GUILayout.Width(170));
            DrawGlow(buttonRect, ColorPrimary, 8, 5f);

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = ColorPrimary;
            if (GUI.Button(buttonRect, "▶  START PLAYTEST", RoundedButtonStyle()))
            {
                MultiLevelTestRunner.AllTestsCompleted = false;
                EditorApplication.isPlaying = true;
            }
            GUI.backgroundColor = previous;
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button(MultiLevelTestRunner.IsRunning ? "RUNNING..." : "START PLAYTEST", RoundedButtonStyle(), GUILayout.Height(36), GUILayout.Width(170));
            GUI.enabled = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBadge()
    {
        string label = "IDLE";
        Color color = ColorTextMuted;

        if (MultiLevelTestRunner.IsRunning)
        {
            label = "RUNNING";
            color = ColorWarning;
        }
        else if (MultiLevelTestRunner.AllTestsCompleted)
        {
            label = "COMPLETED";
            color = ColorSuccess;
        }

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Box(label, RoundedPillStyle(), GUILayout.Width(110), GUILayout.Height(22));
        GUI.backgroundColor = previous;
    }

    // ---------------------------------------------------------------
    // IDLE STATE
    // ---------------------------------------------------------------

    private void DrawIdleDashboard()
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        GUILayout.Space(20);
        GUIStyle idleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        idleStyle.normal.textColor = ColorTextSecondary;
        GUILayout.Label("No test results available.", idleStyle, GUILayout.ExpandWidth(true));

        GUIStyle subStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        subStyle.normal.textColor = ColorTextMuted;
        GUILayout.Label("Run a playtest to see analytics here.", subStyle, GUILayout.ExpandWidth(true));
        GUILayout.Space(20);

        EndColoredCard(cardRect);
    }

    // ---------------------------------------------------------------
    // RUNNING STATE
    // ---------------------------------------------------------------

    private void DrawRunningDashboard()
    {
        DrawRunningStatusCard();
        GUILayout.Space(12);
        DrawLiveExecutionCard();
        GUILayout.Space(12);
        DrawCompletedSoFarCard();
    }

    private void DrawRunningStatusCard()
    {
        Rect currentTestRect = BeginColoredCard(ColorCard);

        EditorGUILayout.BeginHorizontal();
        DrawStatusDot(ColorWarning);
        GUILayout.Label("RUNNING", StatusTextStyle(ColorWarning));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(14);

        GUIStyle levelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 26 };
        levelStyle.normal.textColor = ColorTextPrimary;
        GUILayout.Label(MultiLevelTestRunner.CurrentLevelName + "  (" + (MultiLevelTestRunner.CurrentLevelIndex + 1) + " / " + MultiLevelTestRunner.TotalLevels + ")", levelStyle);

        GUIStyle profileStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
        profileStyle.normal.textColor = GetProfileColor(MultiLevelTestRunner.CurrentProfileName);
        GUILayout.Label(MultiLevelTestRunner.CurrentProfileName, profileStyle);

        GUILayout.Label("Run " + (MultiLevelTestRunner.CurrentRunIndex + 1) + " / " + MultiLevelTestRunner.TotalRunsInBatch, ValueLabelStyle());

        GUILayout.Space(16);

        DrawConfigRow("Completed Runs", MultiLevelTestRunner.CompletedRunUnits + " / " + MultiLevelTestRunner.TotalRunUnits);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("OVERALL PROGRESS", CardHeaderStyle());
        GUILayout.FlexibleSpace();
        GUIStyle progressPercentStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        progressPercentStyle.normal.textColor = ColorPrimary;
        GUILayout.Label((MultiLevelTestRunner.OverallProgress * 100f).ToString("F0") + "%", progressPercentStyle);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        Rect barRect = GUILayoutUtility.GetRect(18, 24, GUILayout.ExpandWidth(true));
        DrawCustomProgressBar(barRect, MultiLevelTestRunner.OverallProgress, ColorPrimary);

        EndColoredCard(currentTestRect);
    }

    // "LIVE EXECUTION" paneli: RUNNING durumunda ekranin bos kalmamasi icin, mevcut
    // batch'in (bu level x bu profile icin) ilerlemesini ayrica, daha yogun/gorsel bir
    // sekilde gosterir. Kullanilan tum veriler zaten MultiLevelTestRunner tarafindan
    // gercek zamanli olarak guncellenen static alanlardir - hicbir yeni/uydurma veri yok.
    private void DrawLiveExecutionCard()
    {
        Rect rect = BeginColoredCard(ColorCard);

        GUILayout.Label("LIVE EXECUTION", CardHeaderStyle());
        GUILayout.Label("Current batch progress for this level and profile", MutedLabelStyle());
        GUILayout.Space(12);

        Color profileColor = GetProfileColor(MultiLevelTestRunner.CurrentProfileName);

        EditorGUILayout.BeginHorizontal();

        GUIStyle liveLevelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
        liveLevelStyle.normal.textColor = ColorTextPrimary;
        GUILayout.Label(MultiLevelTestRunner.CurrentLevelName, liveLevelStyle);

        GUILayout.Space(8);

        Color previousBg = GUI.backgroundColor;
        GUI.backgroundColor = profileColor;
        GUILayout.Box(MultiLevelTestRunner.CurrentProfileName.ToUpper(), RoundedPillStyle(), GUILayout.Width(90), GUILayout.Height(20));
        GUI.backgroundColor = previousBg;

        GUILayout.FlexibleSpace();

        GUIStyle runLabelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        runLabelStyle.normal.textColor = ColorTextSecondary;
        GUILayout.Label("Run " + (MultiLevelTestRunner.CurrentRunIndex + 1) + " / " + MultiLevelTestRunner.TotalRunsInBatch, runLabelStyle);

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        float batchProgress = MultiLevelTestRunner.TotalRunsInBatch > 0
            ? (float)(MultiLevelTestRunner.CurrentRunIndex + 1) / MultiLevelTestRunner.TotalRunsInBatch
            : 0f;
        Rect batchBarRect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        DrawCustomProgressBar(batchBarRect, batchProgress, profileColor);

        GUILayout.Space(16);

        GUILayout.Label("EXECUTION INFORMATION", MutedLabelStyle());
        GUILayout.Space(4);

        DrawConfigRow("Current Level", string.IsNullOrEmpty(MultiLevelTestRunner.CurrentLevelName) ? "-" : MultiLevelTestRunner.CurrentLevelName);
        DrawConfigRow("Current Profile", string.IsNullOrEmpty(MultiLevelTestRunner.CurrentProfileName) ? "-" : MultiLevelTestRunner.CurrentProfileName);
        DrawConfigRow("Current Run", (MultiLevelTestRunner.CurrentRunIndex + 1) + " / " + MultiLevelTestRunner.TotalRunsInBatch);
        DrawConfigRow("Runs Completed", MultiLevelTestRunner.CompletedRunUnits.ToString());
        DrawConfigRow("Total Runs", MultiLevelTestRunner.TotalRunUnits.ToString());
        DrawConfigRow("Current Phase", string.IsNullOrEmpty(MultiLevelTestRunner.CurrentStatus) ? "-" : MultiLevelTestRunner.CurrentStatus);

        EndColoredCard(rect);
    }

    // "COMPLETED SO FAR": test devam ederken bile, o ana kadar tamamlanmis level'larin
    // gercek sonuclarini gosterir. LastSummary, RunSingleLevel her level'i bitirdiginde
    // artimli olarak doluyor (bkz. MultiLevelTestRunner.cs) - burada yeni bir hesap ya da
    // veri kaynagi yok, sadece o an mevcut olan gercek veriler erken gosteriliyor.
    private void DrawCompletedSoFarCard()
    {
        Rect rect = BeginColoredCard(ColorCard);

        GUILayout.Label("COMPLETED SO FAR", CardHeaderStyle());
        GUILayout.Label("Results already recorded in this run", MutedLabelStyle());
        GUILayout.Space(8);

        bool hasData = MultiLevelTestRunner.LastSummary != null && MultiLevelTestRunner.LastSummary.Levels.Count > 0;

        if (!hasData)
        {
            DrawEmptyState("No level has finished yet — results will appear here as they complete.");
        }
        else
        {
            foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(level.LevelName, ValueLabelStyle(), GUILayout.Width(90));

                foreach (ProfileSummaryEntry entry in level.Profiles)
                {
                    GUIStyle miniStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                    miniStyle.normal.textColor = GetProfileColor(entry.Profile);
                    GUILayout.Label(entry.Profile.Substring(0, 1) + " " + entry.SuccessRate.ToString("F0") + "%", miniStyle, GUILayout.Width(70));
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(3);
            }
        }

        EndColoredCard(rect);
    }

    // ---------------------------------------------------------------
    // COMPLETED DASHBOARD
    // ---------------------------------------------------------------

    private void DrawCompletedDashboard(float mainWidth)
    {
        DrawKpiCards(mainWidth);
        GUILayout.Space(12);

        DrawOverallProgressCard();
        GUILayout.Space(12);

        bool wideEnoughForTwoColumns = mainWidth >= 700f;
        float halfWidth = wideEnoughForTwoColumns ? (mainWidth - 14f) / 2f : mainWidth;

        if (wideEnoughForTwoColumns)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));
            DrawDifficultyOverviewCard(halfWidth);
            EditorGUILayout.EndVertical();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));
            DrawProfileComparisonCard(halfWidth);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            DrawDifficultyOverviewCard(halfWidth);
            GUILayout.Space(12);
            DrawProfileComparisonCard(halfWidth);
        }

        GUILayout.Space(12);

        if (wideEnoughForTwoColumns)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));
            DrawLevelPerformanceMatrixCard(halfWidth);
            EditorGUILayout.EndVertical();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical(GUILayout.Width(halfWidth));
            DrawVisualComparisonCard();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            DrawLevelPerformanceMatrixCard(halfWidth);
            GUILayout.Space(12);
            DrawVisualComparisonCard();
        }

        GUILayout.Space(12);
        DrawPerformanceInsights(mainWidth);

        GUILayout.Space(12);
        DrawResultDetailsCard(mainWidth);
    }

    // ---------------------------------------------------------------
    // KPI CARDS
    // ---------------------------------------------------------------

    private void DrawKpiCards(float availableWidth)
    {
        List<System.Action> cards = new List<System.Action>();

        int levelsTested = MultiLevelTestRunner.LastSummary != null ? MultiLevelTestRunner.LastSummary.Levels.Count : 0;
        cards.Add(() => DrawKpiCard("TOTAL LEVELS", levelsTested.ToString(), "Scanned & Tested", ColorPrimary));

        // Dekoratif mini-ring: TotalRunUnits/CompletedRunUnits'ten (zaten var olan, ayni
        // OVERALL PROGRESS'te kullanilan oran) hesaplanir - yeni bir metrik degil.
        float runRatio = MultiLevelTestRunner.TotalRunUnits > 0 ? (float)MultiLevelTestRunner.CompletedRunUnits / MultiLevelTestRunner.TotalRunUnits : 0f;
        cards.Add(() => DrawKpiCard("TOTAL RUNS", MultiLevelTestRunner.CompletedRunUnits + " / " + MultiLevelTestRunner.TotalRunUnits, "Completed", ColorSecondary, r => DrawMiniRing(r, runRatio, ColorSecondary)));

        int totalSuccess = 0;
        int totalRuns = 0;
        foreach (KeyValuePair<string, MetricsSummary> kvp in MultiLevelTestRunner.LastPlayerSummaries)
        {
            totalSuccess += kvp.Value.SuccessCount;
            totalRuns += kvp.Value.TotalTests;
        }
        float overallSuccessRate = totalRuns > 0 ? (float)totalSuccess / totalRuns * 100f : 0f;
        // Dekoratif mini-bars: ComputeProfileAggregates() zaten Profile Comparison'da
        // kullanilan AYNI hesap - burada tekrar cagrilip sadece kucuk bir gorsel icin
        // okunuyor, yeni bir veri kaynagi/hesap degil.
        Dictionary<string, ProfileAggregate> kpiProfileAggregates = ComputeProfileAggregates();
        cards.Add(() => DrawKpiCard("PLAYER SUCCESS RATE", overallSuccessRate.ToString("F0") + "%", totalSuccess + " / " + totalRuns + " Successful", ColorSuccess, r => DrawMiniProfileBars(r, kpiProfileAggregates)));

        float avgDifficulty = ComputeOverallAverageDifficulty();
        // Dekoratif mini-line: her level'in ComputeAverageDifficulty() sonucu (zaten
        // DIFFICULTY OVERVIEW grafiginde kullanilan AYNI deger) sirayla okunur.
        List<float> kpiPerLevelDifficulty = new List<float>();
        if (MultiLevelTestRunner.LastSummary != null)
        {
            foreach (LevelProfileSummary levelForKpi in MultiLevelTestRunner.LastSummary.Levels)
            {
                kpiPerLevelDifficulty.Add(ComputeAverageDifficulty(levelForKpi));
            }
        }
        // ColorCyan: mini-line'i kart aksan rengi olan ColorPrimary'den gorsel olarak
        // ayirmak icin kullanildi - deger/veri kaynagi hala kpiPerLevelDifficulty.
        cards.Add(() => DrawKpiCard("AVG DIFFICULTY", avgDifficulty.ToString("F1") + " / 10", "Across All Levels", ColorPrimary, r => DrawMiniLine(r, kpiPerLevelDifficulty, ColorCyan)));

        int profileCount = System.Enum.GetValues(typeof(PlayerProfileType)).Length;
        cards.Add(() => DrawKpiCard("PROFILES TESTED", profileCount.ToString(), "Careful, Normal, Impulsive", ColorWarning));

        float totalDuration = ComputeTotalDurationSeconds();
        cards.Add(() => DrawKpiCard("TOTAL DURATION", FormatDuration(totalDuration), "Test Execution Time", ColorSecondary));

        int columns = availableWidth >= 1080f ? 6 : (availableWidth >= 720f ? 3 : (availableWidth >= 440f ? 2 : 1));
        DrawGrid(cards, columns, availableWidth, 10f);
    }

    // PREMIUM REDESIGN: miniViz opsiyonel bir dekoratif gorsel cizer (ring/mini-bars/
    // mini-line). Sadece cizim - hicbir Compute*/veri kaynagina yeni bir hesap eklemez,
    // cagiran taraf zaten var olan degerleri kullanir. miniViz verilmezse (null) eski
    // davranisla birebir ayni gorunur.
    // PREMIUM REDESIGN v2: title/value/subtitle/accentColor/miniViz parametreleri ve
    // davranisi BIREBIR AYNI - imza degismedi, hicbir cagri noktasi guncellenmedi.
    // "emphasize" ve ikon, cagiran taraf yerine BURADA, zaten gelen "title" metnine
    // bakarak belirlenir (GetKpiIcon) - boylece "PLAYER SUCCESS RATE" karti otomatik
    // olarak gorsel odak noktasi olur (daha buyuk deger fontu, elevated arka plan,
    // hafif glow) ama bu SADECE sunum katmanidir; hangi karta hangi deger/veri
    // gittigi DrawKpiCards()'ta hala oldugu gibi belirlenir.
    private void DrawKpiCard(string title, string value, string subtitle, Color accentColor, System.Action<Rect> miniViz = null)
    {
        bool emphasize = title == "PLAYER SUCCESS RATE";

        Rect cardRect = BeginColoredCard(emphasize ? ColorCardElevated : ColorCard);

        if (emphasize && Event.current.type == EventType.Repaint)
        {
            DrawGlow(cardRect, accentColor, 10, 5f);
        }

        Rect accentBar = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(emphasize ? 5 : 4), GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            DrawGlow(accentBar, accentColor, 3, emphasize ? 4f : 3f);
            EditorGUI.DrawRect(accentBar, accentColor);
        }

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUIStyle iconStyle = new GUIStyle(EditorStyles.label) { fontSize = 12 };
        iconStyle.normal.textColor = accentColor;
        GUILayout.Label(GetKpiIcon(title), iconStyle, GUILayout.Width(14));
        GUILayout.Label(title, KpiTitleStyle());
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label(value, emphasize ? KpiValueEmphasizedStyle() : KpiValueStyle());
        GUILayout.Space(2);
        GUILayout.Label(subtitle, KpiSubtitleStyle());

        if (miniViz != null && Event.current.type == EventType.Repaint)
        {
            Rect vizRect = new Rect(cardRect.xMax - 46f, cardRect.y + 12f, 32f, 32f);
            miniViz(vizRect);
        }

        EndColoredCard(cardRect);
    }

    // Kucuk, sadece geometrik Unicode sembollerden olusan (emoji degil - farkli
    // isletim sistemi/font kombinasyonlarinda guvenilir render icin) baglamsal
    // ikonlar. Hangi karta hangi ikonun gittigi SABIT ve sadece gorseldir.
    private string GetKpiIcon(string title)
    {
        switch (title)
        {
            case "TOTAL LEVELS":
                return "■";
            case "TOTAL RUNS":
                return "●";
            case "PLAYER SUCCESS RATE":
                return "★";
            case "AVG DIFFICULTY":
                return "▲";
            case "PROFILES TESTED":
                return "◆";
            case "TOTAL DURATION":
                return "○";
            default:
                return "•";
        }
    }

    private void DrawMiniRing(Rect rect, float fraction01, Color color)
    {
        Vector3 center = new Vector3(rect.x + rect.width / 2f, rect.y + rect.height / 2f, 0f);
        float radius = Mathf.Min(rect.width, rect.height) / 2f;

        Handles.BeginGUI();
        Handles.color = ColorCardAlt;
        Handles.DrawSolidArc(center, Vector3.forward, Vector3.up, 360f, radius);

        if (fraction01 > 0f)
        {
            Handles.color = color;
            Handles.DrawSolidArc(center, Vector3.forward, Vector3.up, 360f * Mathf.Clamp01(fraction01), radius);
        }

        Handles.color = ColorCard;
        Handles.DrawSolidDisc(center, Vector3.forward, radius * 0.55f);
        Handles.EndGUI();
    }

    private void DrawMiniProfileBars(Rect rect, Dictionary<string, ProfileAggregate> aggregates)
    {
        string[] profiles = { "Careful", "Normal", "Impulsive" };
        float barWidth = rect.width / 3f - 2f;
        float baseY = rect.yMax;

        for (int i = 0; i < profiles.Length; i++)
        {
            ProfileAggregate agg;
            float rate = aggregates.TryGetValue(profiles[i], out agg) && agg != null ? agg.SuccessRate : 0f;
            float barHeight = rect.height * Mathf.Clamp01(rate / 100f);
            float x = rect.x + i * (barWidth + 2f);
            Rect barRect = new Rect(x, baseY - barHeight, barWidth, Mathf.Max(barHeight, 2f));
            EditorGUI.DrawRect(barRect, GetProfileColor(profiles[i]));
        }
    }

    private void DrawMiniLine(Rect rect, List<float> values, Color color)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        Vector3[] points = new Vector3[values.Count];
        float stepX = values.Count > 1 ? rect.width / (values.Count - 1) : 0f;

        for (int i = 0; i < values.Count; i++)
        {
            float normalized = Mathf.Clamp01(values[i] / 10f);
            float x = values.Count > 1 ? rect.x + i * stepX : rect.x + rect.width / 2f;
            float y = rect.yMax - normalized * rect.height;
            points[i] = new Vector3(x, y, 0f);
        }

        Handles.BeginGUI();
        Handles.color = color;

        if (points.Length == 1)
        {
            Handles.DrawSolidDisc(points[0], Vector3.forward, 2f);
        }
        else
        {
            Handles.DrawAAPolyLine(2.5f, points);
            foreach (Vector3 p in points)
            {
                Handles.DrawSolidDisc(p, Vector3.forward, 2f);
            }
        }

        Handles.EndGUI();
    }

    // ---------------------------------------------------------------
    // OVERALL PROGRESS
    // ---------------------------------------------------------------

    private void DrawOverallProgressCard()
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("OVERALL PROGRESS", CardHeaderStyle());
        GUILayout.FlexibleSpace();
        GUILayout.Label((MultiLevelTestRunner.OverallProgress * 100f).ToString("F0") + "%", ValueLabelStyle());
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        Rect barRect = GUILayoutUtility.GetRect(18, 14, GUILayout.ExpandWidth(true));
        DrawCustomProgressBar(barRect, MultiLevelTestRunner.OverallProgress, ColorPrimary);

        EndColoredCard(cardRect);
    }

    private void DrawCustomProgressBar(Rect rect, float progress01, Color fillColor)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        // PREMIUM REDESIGN: track ve dolum artik rounded-pill gorunumunde (radius,
        // barin yuksekligine gore olceklenir), dolumun ucunda hafif bir glow var.
        // progress01 degeri ve dolum orani ONCEKI ile birebir ayni - sadece cizim
        // teknigi degisti.
        int radius = Mathf.Clamp(Mathf.RoundToInt(rect.height / 2f), 3, 10);
        Texture2D roundedTex = GetRoundedRectTexture(radius);
        Color previousColor = GUI.color;

        GUI.color = ColorCardAlt;
        GUI.DrawTexture(rect, roundedTex);

        float fillWidth = rect.width * Mathf.Clamp01(progress01);
        if (fillWidth > 0.5f)
        {
            Rect fillRect = new Rect(rect.x, rect.y, Mathf.Max(fillWidth, rect.height), rect.height);

            GUI.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.25f);
            GUI.DrawTexture(new Rect(fillRect.x - 3f, fillRect.y - 3f, fillRect.width + 6f, fillRect.height + 6f), roundedTex);

            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, roundedTex);
        }

        GUI.color = previousColor;
        DrawBorder(rect, ColorBorder, 1f);
    }

    // PREMIUM REDESIGN: Result Details tablosunda "Success" ve "Difficulty"
    // sutunlari icin kucuk inline mini-bar + metin hucresi. Gosterilen "text"
    // ve "fraction01" cagiran yerde entry.SuccessRate/entry.DifficultyScore'dan
    // BIREBIR turetiliyor - burada yeni bir hesap yok, sadece kucuk bir gorsel
    // ekleniyor. Sutun genisligi (width = columnWidths[i]) degismedi, bu yuzden
    // yatay scrollbar guvenligini etkilemez. Cok dar sutunlarda (width kucukse)
    // bar cizilmez, sadece metin gosterilir - hicbir icerik tasmaz/kesilmez.
    private void DrawInlineMiniBar(string text, float fraction01, Color barColor, float width)
    {
        Rect rect = GUILayoutUtility.GetRect(width, 16f, GUILayout.Width(width), GUILayout.Height(16f));

        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        float barWidth = Mathf.Min(width - 30f, 28f);

        if (barWidth < 10f)
        {
            GUI.Label(rect, text, ValueLabelStyle());
            return;
        }

        Rect barRect = new Rect(rect.x, rect.y + rect.height / 2f - 2f, barWidth, 4f);
        Texture2D roundedTex = GetRoundedRectTexture(2);
        Color previousColor = GUI.color;

        GUI.color = ColorCardAlt;
        GUI.DrawTexture(barRect, roundedTex);

        float fillWidth = barRect.width * Mathf.Clamp01(fraction01);
        if (fillWidth > 0.5f)
        {
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, fillWidth, barRect.height), roundedTex);
        }

        GUI.color = previousColor;

        Rect textRect = new Rect(barRect.xMax + 6f, rect.y, rect.width - barWidth - 6f, rect.height);
        GUI.Label(textRect, text, ValueLabelStyle());
    }

    // ---------------------------------------------------------------
    // DIFFICULTY OVERVIEW
    // ---------------------------------------------------------------

    private void DrawDifficultyOverviewCard(float width)
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        GUILayout.Label("DIFFICULTY OVERVIEW", CardHeaderStyle());
        GUILayout.Label("Average difficulty score per level (higher is harder)", MutedLabelStyle());
        GUILayout.Space(10);

        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            DrawEmptyState("No difficulty data yet.");
        }
        else
        {
            bool sideBySide = width >= 480f;

            if (sideBySide)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                DrawDifficultyBarChart();
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical(GUILayout.Width(150));
                DrawDifficultyGuideContent();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawDifficultyBarChart();
                GUILayout.Space(10);
                DrawDifficultyGuideContent();
            }
        }

        EndColoredCard(cardRect);
    }

    // PREMIUM REDESIGN: dikey bar chart yerine gradient area + glowing-dot line chart.
    // Veri kaynagi ve hesaplama (ComputeAverageDifficulty, ClassifyForDisplay,
    // GetClassificationColor) BIREBIR AYNI - sadece cizim teknigi degisti.
    private void DrawDifficultyBarChart()
    {
        List<LevelProfileSummary> levels = MultiLevelTestRunner.LastSummary.Levels;

        float chartHeight = 170f;
        Rect chartRect = GUILayoutUtility.GetRect(10, chartHeight, GUILayout.ExpandWidth(true));

        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        int count = levels.Count;
        if (count == 0)
        {
            return;
        }

        float labelsHeight = 48f;
        float plotHeight = chartRect.height - labelsHeight;
        float plotBottom = chartRect.y + plotHeight;
        float slotWidth = chartRect.width / count;

        // ince, saydam yatay grid cizgileri (0/2.5/5/7.5/10 seviyeleri). PREMIUM
        // REDESIGN v2: alfa 0.4 -> 0.22 dusuruldu, daha "refined"/az goze batan bir
        // grid icin - seviyeler/pozisyonlar AYNI.
        for (int g = 0; g <= 4; g++)
        {
            float gy = chartRect.y + plotHeight * (1f - g / 4f);
            EditorGUI.DrawRect(new Rect(chartRect.x, gy, chartRect.width, 1f), new Color(ColorBorder.r, ColorBorder.g, ColorBorder.b, 0.22f));
        }

        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float avgDifficulty = ComputeAverageDifficulty(levels[i]);
            float normalized = Mathf.Clamp01(avgDifficulty / 10f);
            float px = chartRect.x + i * slotWidth + slotWidth / 2f;
            float py = plotBottom - normalized * plotHeight;
            points[i] = new Vector3(px, py, 0f);
        }

        if (count == 1)
        {
            DrawAreaColumn(points[0].x - slotWidth / 4f, points[0].x + slotWidth / 4f, points[0].y, plotBottom);
        }
        else
        {
            for (int i = 0; i < count - 1; i++)
            {
                DrawAreaSegment(points[i], points[i + 1], plotBottom);
            }
        }

        Handles.BeginGUI();

        if (points.Length > 1)
        {
            Handles.color = ColorPrimary;
            Handles.DrawAAPolyLine(3f, points);
        }

        foreach (Vector3 p in points)
        {
            for (int g = 3; g >= 1; g--)
            {
                Handles.color = new Color(ColorPrimary.r, ColorPrimary.g, ColorPrimary.b, 0.08f * g);
                Handles.DrawSolidDisc(p, Vector3.forward, 3f + g * 2f);
            }

            Handles.color = ColorPrimary;
            Handles.DrawSolidDisc(p, Vector3.forward, 4f);
            Handles.color = ColorCard;
            Handles.DrawSolidDisc(p, Vector3.forward, 2f);
        }

        Handles.EndGUI();

        for (int i = 0; i < count; i++)
        {
            float avgDifficulty = ComputeAverageDifficulty(levels[i]);
            float slotX = chartRect.x + i * slotWidth;
            Color barColor = GetClassificationColor(ClassifyForDisplay(avgDifficulty));

            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            valueStyle.normal.textColor = ColorTextPrimary;
            GUI.Label(new Rect(slotX, points[i].y - 26f, slotWidth, 18f), avgDifficulty.ToString("F1"), valueStyle);

            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = ColorTextPrimary;
            GUI.Label(new Rect(slotX, chartRect.yMax - labelsHeight + 6f, slotWidth, 16f), levels[i].LevelName, nameStyle);

            float badgeWidth = Mathf.Min(74f, slotWidth - 6f);
            Rect badgeRect = new Rect(slotX + (slotWidth - badgeWidth) / 2f, chartRect.yMax - labelsHeight + 24f, badgeWidth, 17f);
            EditorGUI.DrawRect(badgeRect, barColor);

            GUIStyle classStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            classStyle.normal.textColor = Color.white;
            GUI.Label(badgeRect, ClassifyForDisplay(avgDifficulty), classStyle);
        }
    }

    // PREMIUM REDESIGN v2: alan doldurma artik cizginin hemen altinda daha belirgin,
    // taban cizgisine dogru giderek saydamlasan gercek bir "gradient area fill" -
    // onceki duz (tek alfali, 0.14) dolgunun yerine gecti. Veri noktalarinin
    // konumlari (x0/x1/y0/baseline - yani ComputeAverageDifficulty sonuclari) HICBIR
    // SEKILDE degismedi, sadece dolgunun kendi ic alfa gecisi degisti.
    private void DrawAreaSegment(Vector3 p0, Vector3 p1, float baseline)
    {
        int steps = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(p1.x - p0.x) / 6f));

        for (int s = 0; s < steps; s++)
        {
            float t0 = (float)s / steps;
            float t1 = (float)(s + 1) / steps;
            float x0 = Mathf.Lerp(p0.x, p1.x, t0);
            float x1 = Mathf.Lerp(p0.x, p1.x, t1);
            float y0 = Mathf.Lerp(p0.y, p1.y, t0);

            DrawGradientColumn(x0, x1, y0, baseline);
        }
    }

    private void DrawAreaColumn(float x0, float x1, float y, float baseline)
    {
        DrawGradientColumn(x0, x1, y, baseline);
    }

    private void DrawGradientColumn(float x0, float x1, float yTop, float baseline)
    {
        float height = Mathf.Max(0f, baseline - yTop);
        if (height <= 0f)
        {
            return;
        }

        int verticalSteps = Mathf.Clamp(Mathf.RoundToInt(height / 10f), 3, 6);
        float stepHeight = height / verticalSteps;

        for (int v = 0; v < verticalSteps; v++)
        {
            float t = (float)v / verticalSteps;
            float alpha = Mathf.Lerp(0.20f, 0.015f, t);
            Rect stepRect = new Rect(x0, yTop + v * stepHeight, Mathf.Max(1f, x1 - x0 + 1f), stepHeight + 1f);
            EditorGUI.DrawRect(stepRect, new Color(ColorPrimary.r, ColorPrimary.g, ColorPrimary.b, alpha));
        }
    }

    private void DrawDifficultyGuideContent()
    {
        Rect rect = BeginColoredCard(ColorCardAlt);

        GUILayout.Label("DIFFICULTY GUIDE", MutedLabelStyle());
        GUILayout.Space(6);

        DrawGuideRow("0 - 3", "EASY", ColorSuccess);
        DrawGuideRow("3 - 6", "MEDIUM", ColorPrimary);
        DrawGuideRow("6 - 8", "HARD", ColorWarning);
        DrawGuideRow("8 - 10", "VERY HARD", ColorDanger);

        EndColoredCard(rect);
    }

    private void DrawGuideRow(string range, string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        Rect dot = GUILayoutUtility.GetRect(9, 9, GUILayout.Width(9), GUILayout.Height(9));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(dot, color);
        }
        GUILayout.Space(4);
        GUILayout.Label(range, MutedLabelStyle(), GUILayout.Width(50));
        GUILayout.Label(label, ValueLabelStyle());
        EditorGUILayout.EndHorizontal();
    }

    // ---------------------------------------------------------------
    // PROFILE COMPARISON
    // ---------------------------------------------------------------

    private void DrawProfileComparisonCard(float width)
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        GUILayout.Label("PROFILE COMPARISON", CardHeaderStyle());
        GUILayout.Label("Performance comparison across all tested levels", MutedLabelStyle());
        GUILayout.Space(10);

        if (MultiLevelTestRunner.LastPlayerSummaries.Count == 0)
        {
            DrawEmptyState("No profile data yet.");
        }
        else
        {
            Dictionary<string, ProfileAggregate> aggregates = ComputeProfileAggregates();
            Dictionary<string, ProfileDisplayStats> displayStats = ComputeProfileDisplayStats();

            // Crown/star rozeti icin "en iyi profil" ayni formulle (en yuksek agirlikli
            // SuccessRate) burada da hesaplanir - DrawPerformanceInsights'taki BEST PROFILE
            // hesabiyla BIREBIR ayni mantik, sadece burada da (dekoratif rozet icin) okunuyor.
            // Best Profile'in KENDI hesaplanma yontemi hicbir sekilde degistirilmedi.
            string bestProfileName = null;
            float bestProfileRate = -1f;
            foreach (KeyValuePair<string, ProfileAggregate> kvp in aggregates)
            {
                if (kvp.Value.SuccessRate > bestProfileRate)
                {
                    bestProfileRate = kvp.Value.SuccessRate;
                    bestProfileName = kvp.Key;
                }
            }

            List<System.Action> cards = new List<System.Action>();
            foreach (PlayerProfileType profileType in System.Enum.GetValues(typeof(PlayerProfileType)))
            {
                string profileName = profileType.ToString();
                bool isBest = profileName == bestProfileName;
                cards.Add(() => DrawProfileDonutCard(profileName, aggregates, displayStats, isBest));
            }

            // KOK NEDEN DUZELTMESI: bu grid, BeginColoredCard(BoxWithPadding) ile acilmis
            // padded bir kartin ICINDE. BoxWithPadding sol+sag 16+16=32px pay ayirir, ama
            // grid'e disaridan gelen "width" degeri bu payi bilmiyor. Duzeltilmeden once
            // grid, kartin gercekte sahip oldugundan 32px daha genis bir satir istiyordu -
            // bu da pencerede kalici bir yatay tasmaya (ve dolayisiyla yatay scrollbar'a)
            // yol aciyordu. innerWidth, kartin gercek ic genisligini kullanir.
            float innerWidth = Mathf.Max(120f, width - 32f);
            int columns = innerWidth >= 560f ? 3 : (innerWidth >= 340f ? 2 : 1);
            DrawGrid(cards, columns, innerWidth, 12f);
        }

        EndColoredCard(cardRect);
    }

    private void DrawProfileDonutCard(string profileName, Dictionary<string, ProfileAggregate> aggregates, Dictionary<string, ProfileDisplayStats> displayStats, bool isBest)
    {
        Color profileColor = GetProfileColor(profileName);

        // ColorCardElevated: en iyi profil hafifce daha "yukselmis" gorunsun diye
        // (zaten var olan ★ BEST etiketiyle tutarli, saf gorsel - isBest hesaplamasi
        // degismedi).
        Rect cardRect = BeginColoredCard(isBest ? ColorCardElevated : ColorCardAlt);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(profileName.ToUpper(), ProfileNameStyle(profileColor));
        if (isBest)
        {
            GUILayout.FlexibleSpace();

            // PREMIUM REDESIGN v2: "★ BEST" artik duz kalin metin degil, hafif glow'lu,
            // dolgu renkli, yuvarlatilmis kucuk bir "odul" rozeti - premium bir "award
            // indicator" hissi icin. isBest hesabi/gosterilen metin AYNI, sadece sunumu
            // degisti.
            Rect bestRect = GUILayoutUtility.GetRect(64, 20, GUILayout.Width(64), GUILayout.Height(20));
            DrawGlow(bestRect, ColorWarning, 7, 3f);

            Color previousBadgeBg = GUI.backgroundColor;
            GUI.backgroundColor = ColorWarning;
            if (Event.current.type == EventType.Repaint)
            {
                GUI.Box(bestRect, "★ BEST", RoundedPillStyle());
            }
            GUI.backgroundColor = previousBadgeBg;
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        ProfileAggregate aggregate;
        aggregates.TryGetValue(profileName, out aggregate);
        float successRate = aggregate != null ? aggregate.SuccessRate : 0f;
        int successCount = aggregate != null ? aggregate.SuccessCount : 0;
        int totalCount = aggregate != null ? aggregate.TotalCount : 0;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Rect donutRect = GUILayoutUtility.GetRect(112, 112, GUILayout.Width(112), GUILayout.Height(112));
        DrawDonut(donutRect, successRate / 100f, profileColor, successRate.ToString("F0") + "%");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUIStyle centerLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
        centerLabelStyle.normal.textColor = ColorTextSecondary;
        GUILayout.Label("Success Rate", centerLabelStyle);

        GUIStyle countStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
        countStyle.normal.textColor = ColorTextMuted;
        GUILayout.Label(successCount + " / " + totalCount + " successful", countStyle);

        GUILayout.Space(12);

        ProfileDisplayStats stats;
        if (displayStats.TryGetValue(profileName, out stats) && stats != null)
        {
            DrawConfigRow("Avg Steps", stats.AverageSteps.ToString("F1"));
            DrawConfigRow("Avg Wrong Moves", stats.AverageWrongMoves.ToString("F1"));
            DrawConfigRow("Avg Difficulty", stats.AverageDifficulty.ToString("F1") + " / 10");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Classification", MutedLabelStyle());
            GUILayout.FlexibleSpace();
            DrawClassificationBadge(stats.Classification);
            EditorGUILayout.EndHorizontal();
        }

        EndColoredCard(cardRect);
    }

    private void DrawDonut(Rect rect, float fraction01, Color color, string centerText)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Vector3 center = new Vector3(rect.x + rect.width / 2f, rect.y + rect.height / 2f, 0f);
            float radius = Mathf.Min(rect.width, rect.height) / 2f;

            Handles.BeginGUI();

            // PREMIUM REDESIGN: donutun disina, gercek renginde, disari dogru saydamlasan
            // 3 katmanli ince bir halka ekler ("subtle glow"). Fraction01/renk/merkez metin
            // ONCEKI ile birebir ayni - sadece bu dekoratif katman eklendi.
            for (int i = 3; i >= 1; i--)
            {
                Handles.color = new Color(color.r, color.g, color.b, 0.05f * i);
                Handles.DrawSolidDisc(center, Vector3.forward, radius + i * 2.5f);
            }

            Handles.color = ColorCardAlt;
            Handles.DrawSolidArc(center, Vector3.forward, Vector3.up, 360f, radius);

            if (fraction01 > 0f)
            {
                Handles.color = color;
                float angle = 360f * Mathf.Clamp01(fraction01);
                Handles.DrawSolidArc(center, Vector3.forward, Vector3.up, angle, radius);
            }

            Handles.color = ColorCard;
            Handles.DrawSolidDisc(center, Vector3.forward, radius * 0.62f);

            Handles.EndGUI();
        }

        // PREMIUM REDESIGN v2: yuzde degeri iki farkli boyutta cizilir (buyuk sayi +
        // kucuk "%" isareti) - tipografik hiyerarsi icin. Gosterilen centerText'in
        // kendisi (yani hesaplanan deger) HICBIR SEKILDE degismedi, sadece "%" ile
        // bitiyorsa gorsel olarak iki parcaya bolunup ayri font boyutlariyla ciziliyor.
        if (centerText.EndsWith("%"))
        {
            string numberPart = centerText.Substring(0, centerText.Length - 1);

            GUIStyle bigStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 };
            bigStyle.normal.textColor = ColorTextPrimary;
            GUIStyle smallStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            smallStyle.normal.textColor = ColorTextSecondary;

            float numberWidth = bigStyle.CalcSize(new GUIContent(numberPart)).x;
            float percentWidth = smallStyle.CalcSize(new GUIContent("%")).x;
            float totalWidth = numberWidth + percentWidth + 2f;

            Rect numberRect = new Rect(rect.x + rect.width / 2f - totalWidth / 2f, rect.y, numberWidth, rect.height);
            Rect percentRect = new Rect(numberRect.xMax + 2f, rect.y + 5f, percentWidth, rect.height - 5f);

            GUI.Label(numberRect, numberPart, bigStyle);
            GUI.Label(percentRect, "%", smallStyle);
        }
        else
        {
            GUIStyle centerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
            centerStyle.normal.textColor = ColorTextPrimary;
            GUI.Label(rect, centerText, centerStyle);
        }
    }

    // ---------------------------------------------------------------
    // LEVEL PERFORMANCE MATRIX
    // ---------------------------------------------------------------

    private void DrawLevelPerformanceMatrixCard(float cardWidth)
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        GUILayout.Label("LEVEL PERFORMANCE MATRIX", CardHeaderStyle());
        GUILayout.Label("Success rate comparison by level and profile", MutedLabelStyle());
        GUILayout.Space(10);

        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            DrawEmptyState("No level data yet.");
        }
        else
        {
            // Sutunlar, kartin gercek genisligine oranli olarak hesaplanir; boylece
            // genis pencerede bosluk kalmaz, dar pencerede ise sadece bu tablo kendi
            // icinde yatay scroll olur (pencere genelinde degil).
            float contentWidth = Mathf.Max(10f, cardWidth - 36f);
            float minTableWidth = 560f;
            float tableWidth = Mathf.Max(minTableWidth, contentWidth);

            float[] fractions = { 0.17f, 0.12f, 0.12f, 0.12f, 0.13f, 0.16f, 0.18f };
            float[] columnWidths = new float[fractions.Length];
            for (int i = 0; i < fractions.Length; i++)
            {
                columnWidths[i] = tableWidth * fractions[i];
            }

            string[] headers = { "Level", "Solver", "Careful", "Normal", "Impulsive", "Avg Diff.", "Class." };

            float rowHeight = 26f;
            float viewHeight = Mathf.Min(210f, 32f + MultiLevelTestRunner.LastSummary.Levels.Count * rowHeight);

            matrixScrollPosition = EditorGUILayout.BeginScrollView(matrixScrollPosition, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.Height(viewHeight), GUILayout.Width(contentWidth));

            EditorGUILayout.BeginVertical(GUILayout.Width(tableWidth));

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < headers.Length; i++)
            {
                GUILayout.Label(headers[i], EditorStyles.miniBoldLabel, GUILayout.Width(columnWidths[i]));
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(level.LevelName, ValueLabelStyle(), GUILayout.Width(columnWidths[0]));

                MetricsSummary solverSummary;
                string solverText = MultiLevelTestRunner.LastSolverSummaries.TryGetValue(level.LevelName, out solverSummary) ? solverSummary.SuccessRate.ToString("F0") + "%" : "-";
                GUILayout.Label(solverText, ValueLabelStyle(), GUILayout.Width(columnWidths[1]));

                DrawMatrixCell(FindProfileEntry(level, "Careful"), columnWidths[2]);
                DrawMatrixCell(FindProfileEntry(level, "Normal"), columnWidths[3]);
                DrawMatrixCell(FindProfileEntry(level, "Impulsive"), columnWidths[4]);

                float avgDifficulty = ComputeAverageDifficulty(level);
                GUILayout.Label(avgDifficulty.ToString("F1") + "/10", ValueLabelStyle(), GUILayout.Width(columnWidths[5]));

                DrawClassificationBadge(ClassifyForDisplay(avgDifficulty));

                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        EndColoredCard(cardRect);
    }

    private void DrawMatrixCell(ProfileSummaryEntry entry, float width)
    {
        string text = entry != null ? entry.SuccessRate.ToString("F0") + "%" : "-";
        Color color = entry != null ? GetProfileColor(entry.Profile) : ColorTextMuted;

        GUIStyle style = new GUIStyle(ValueLabelStyle());
        style.normal.textColor = color;
        GUILayout.Label(text, style, GUILayout.Width(width));
    }

    private ProfileSummaryEntry FindProfileEntry(LevelProfileSummary level, string profileName)
    {
        foreach (ProfileSummaryEntry entry in level.Profiles)
        {
            if (entry.Profile == profileName)
            {
                return entry;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------
    // VISUAL COMPARISON
    // ---------------------------------------------------------------

    private void DrawVisualComparisonCard()
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        GUILayout.Label("VISUAL COMPARISON", CardHeaderStyle());
        GUILayout.Label("Success rate by level and profile", MutedLabelStyle());
        GUILayout.Space(8);

        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            DrawEmptyState("No comparison data yet.");
        }
        else
        {
            List<LevelProfileSummary> visualLevels = MultiLevelTestRunner.LastSummary.Levels;
            for (int levelIdx = 0; levelIdx < visualLevels.Count; levelIdx++)
            {
                LevelProfileSummary level = visualLevels[levelIdx];

                GUILayout.Label(level.LevelName, ValueLabelStyle());
                GUILayout.Space(3);

                foreach (ProfileSummaryEntry entry in level.Profiles)
                {
                    DrawHorizontalBarRow(entry.Profile, entry.SuccessRate / 100f, GetProfileColor(entry.Profile), entry.SuccessRate.ToString("F0") + "%");
                }

                if (levelIdx < visualLevels.Count - 1)
                {
                    GUILayout.Space(4);
                    Rect divider = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(divider, ColorBorder);
                    }
                    GUILayout.Space(10);
                }
            }
        }

        EndColoredCard(cardRect);
    }

    private void DrawHorizontalBarRow(string label, float fraction01, Color color, string valueText)
    {
        EditorGUILayout.BeginHorizontal();

        GUIStyle labelStyle = new GUIStyle(MutedLabelStyle());
        labelStyle.normal.textColor = color;
        GUILayout.Label(label, labelStyle, GUILayout.Width(72));

        Rect barRect = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(barRect, ColorCardAlt);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(fraction01), barRect.height);
            if (fillRect.width > 0f)
            {
                EditorGUI.DrawRect(fillRect, color);
            }

            GUIStyle valueStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            valueStyle.normal.textColor = ColorTextPrimary;
            Rect valueRect = new Rect(barRect.x, barRect.y, barRect.width - 6f, barRect.height);
            GUI.Label(valueRect, valueText, valueStyle);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    // ---------------------------------------------------------------
    // PERFORMANCE INSIGHTS
    // ---------------------------------------------------------------

    private void DrawPerformanceInsights(float availableWidth)
    {
        // PREMIUM REDESIGN v2: "PERFORMANCE INSIGHTS" bir section-level baslik oldugu
        // icin (CardHeaderStyle'dan daha yuksek hiyerarside), yaninda kucuk renkli bir
        // aksan cizgisi (accent tick) eklendi - tipografik hiyerarsiyi guclendirmek
        // icin. Baslik metni/anlami degismedi.
        EditorGUILayout.BeginHorizontal();
        Rect accentTick = GUILayoutUtility.GetRect(3, 16, GUILayout.Width(3), GUILayout.Height(16));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(accentTick, ColorPrimary);
        }
        GUILayout.Space(6);
        GUILayout.Label("PERFORMANCE INSIGHTS", SectionHeaderStyle());
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(8);

        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            Rect emptyRect = BeginColoredCard(ColorCard);
            DrawEmptyState("No insights available yet.");
            EndColoredCard(emptyRect);
            return;
        }

        Dictionary<string, ProfileAggregate> profileAggregates = ComputeProfileAggregates();

        string bestProfile = null;
        string worstProfile = null;
        float bestRate = -1f;
        float worstRate = 101f;

        foreach (KeyValuePair<string, ProfileAggregate> kvp in profileAggregates)
        {
            if (kvp.Value.SuccessRate > bestRate)
            {
                bestRate = kvp.Value.SuccessRate;
                bestProfile = kvp.Key;
            }

            if (kvp.Value.SuccessRate < worstRate)
            {
                worstRate = kvp.Value.SuccessRate;
                worstProfile = kvp.Key;
            }
        }

        string easiestLevel = null;
        string hardestLevel = null;
        float easiestScore = float.MaxValue;
        float hardestScore = float.MinValue;

        foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
        {
            float avgDifficulty = ComputeAverageDifficulty(level);

            if (avgDifficulty < easiestScore)
            {
                easiestScore = avgDifficulty;
                easiestLevel = level.LevelName;
            }

            if (avgDifficulty > hardestScore)
            {
                hardestScore = avgDifficulty;
                hardestLevel = level.LevelName;
            }
        }

        List<System.Action> cards = new List<System.Action>();

        if (bestProfile != null)
        {
            string p = bestProfile;
            float r = bestRate;
            cards.Add(() => DrawInsightCard("BEST PROFILE", p, r.ToString("F0") + "% Success Rate", "Most consistent performance", GetProfileColor(p)));
        }

        if (worstProfile != null)
        {
            string p = worstProfile;
            float r = worstRate;
            cards.Add(() => DrawInsightCard("LOWEST PROFILE", p, r.ToString("F0") + "% Success Rate", "Needs improvement", ColorDanger));
        }

        if (hardestLevel != null)
        {
            string l = hardestLevel;
            float s = hardestScore;
            cards.Add(() => DrawInsightCard("HARDEST LEVEL", l, s.ToString("F1") + " Avg Difficulty", "Most challenging", ColorWarning));
        }

        if (easiestLevel != null)
        {
            string l = easiestLevel;
            float s = easiestScore;
            cards.Add(() => DrawInsightCard("EASIEST LEVEL", l, s.ToString("F1") + " Avg Difficulty", "Most accessible", ColorSuccess));
        }

        int columns = availableWidth >= 800f ? 4 : (availableWidth >= 420f ? 2 : 1);
        DrawGrid(cards, columns, availableWidth, 10f);
    }

    private void DrawInsightCard(string title, string mainValue, string subValue, string description, Color accentColor)
    {
        Rect cardRect = BeginColoredCard(ColorCardAlt);

        Rect accentBar = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(4), GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(accentBar, accentColor);
        }
        GUILayout.Space(8);

        GUILayout.Label(title, MutedLabelStyle());
        GUILayout.Space(2);
        GUILayout.Label(mainValue, InsightValueStyle());
        GUILayout.Label(subValue, ValueLabelStyle());
        GUILayout.Space(2);
        GUILayout.Label(description, KpiSubtitleStyle());

        EndColoredCard(cardRect);
    }

    // ---------------------------------------------------------------
    // RESULT DETAILS
    // ---------------------------------------------------------------

    private void DrawResultDetailsCard(float cardWidth)
    {
        Rect cardRect = BeginColoredCard(ColorCard);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("RESULT DETAILS", CardHeaderStyle());
        GUILayout.FlexibleSpace();
        int rowCount = MultiLevelTestRunner.LastSummary != null ? CountResultRows() : 0;
        GUILayout.Label(rowCount + " rows", MutedLabelStyle());
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            DrawEmptyState("No detailed results yet.");
        }
        else
        {
            // Sutunlar, kartin gercek ic genisligine (padding dusulmus) oranli hesaplanir;
            // boylece bu tablo da diger section'lar gibi pencereyi asmaz. Satirlar cok
            // sayida oldugunda tablo ekranin buyuk bolumunu kaplamasin diye dikey yukseklik
            // sabit bir tavanla sinirlanir (icerik kendi icinde dikey scroll olur).
            float contentWidth = Mathf.Max(10f, cardWidth - 32f);
            float[] fractions = { 0.14f, 0.14f, 0.11f, 0.13f, 0.15f, 0.13f, 0.20f };
            float[] columnWidths = new float[fractions.Length];
            for (int i = 0; i < fractions.Length; i++)
            {
                columnWidths[i] = contentWidth * fractions[i];
            }

            string[] headers = { "Level", "Profile", "Success", "Avg Steps", "Wrong Moves", "Difficulty", "Classification" };

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < headers.Length; i++)
            {
                GUILayout.Label(headers[i], EditorStyles.miniBoldLabel, GUILayout.Width(columnWidths[i]));
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            float rowHeight = 24f;
            float viewHeight = Mathf.Min(200f, rowCount * rowHeight + 6f);

            resultDetailsScrollPosition = EditorGUILayout.BeginScrollView(resultDetailsScrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(viewHeight), GUILayout.Width(contentWidth));

            foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
            {
                foreach (ProfileSummaryEntry entry in level.Profiles)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(level.LevelName, ValueLabelStyle(), GUILayout.Width(columnWidths[0]));

                    GUIStyle profileStyle = new GUIStyle(ValueLabelStyle());
                    profileStyle.normal.textColor = GetProfileColor(entry.Profile);
                    GUILayout.Label(entry.Profile, profileStyle, GUILayout.Width(columnWidths[1]));

                    // PREMIUM REDESIGN: Success/Difficulty hucrelerine kucuk inline mini-bar
                    // eklendi. Gosterilen deger hala dogrudan entry.SuccessRate/DifficultyScore -
                    // hicbir yeni hesap yok, sadece ayni sayinin yaninda kucuk bir gorsel var.
                    DrawInlineMiniBar(entry.SuccessRate.ToString("F0") + "%", entry.SuccessRate / 100f, GetProfileColor(entry.Profile), columnWidths[2]);
                    GUILayout.Label(entry.AverageSteps.ToString("F1"), ValueLabelStyle(), GUILayout.Width(columnWidths[3]));
                    GUILayout.Label(entry.AverageWrongMoves.ToString("F1"), ValueLabelStyle(), GUILayout.Width(columnWidths[4]));
                    DrawInlineMiniBar(entry.DifficultyScore.ToString("F1") + "/10", entry.DifficultyScore / 10f, GetClassificationColor(entry.Classification), columnWidths[5]);
                    DrawClassificationBadge(entry.Classification);

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);

                    // PREMIUM REDESIGN v2: satirlar arasi cok ince, dusuk kontrastli bir
                    // ayrac cizgisi - "better row separation" icin. Satirlarin genisligi/
                    // hizalanmasi/verisi degismedi, sadece altlarina 1px'lik bir cizgi
                    // eklendi.
                    Rect rowDivider = GUILayoutUtility.GetRect(10, 1, GUILayout.Width(contentWidth));
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(rowDivider, new Color(ColorBorder.r, ColorBorder.g, ColorBorder.b, 0.18f));
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        EndColoredCard(cardRect);
    }

    private int CountResultRows()
    {
        if (MultiLevelTestRunner.LastSummary == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
        {
            count += level.Profiles.Count;
        }

        return count;
    }

    // ---------------------------------------------------------------
    // EMPTY STATE
    // ---------------------------------------------------------------

    private void DrawEmptyState(string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = ColorTextMuted;
        GUILayout.Space(10);
        GUILayout.Label(message, style, GUILayout.ExpandWidth(true));
        GUILayout.Space(10);
    }

    // ---------------------------------------------------------------
    // GENERIC GRID
    // ---------------------------------------------------------------

    private void DrawGrid(List<System.Action> cardDrawers, int columns, float availableWidth, float spacing = 14f)
    {
        if (columns < 1)
        {
            columns = 1;
        }

        float cardWidth = (availableWidth - spacing * (columns - 1)) / columns;
        if (cardWidth < 120f)
        {
            cardWidth = 120f;
        }

        for (int i = 0; i < cardDrawers.Count; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < columns && (i + c) < cardDrawers.Count; c++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(cardWidth));
                cardDrawers[i + c]();
                EditorGUILayout.EndVertical();

                if (c < columns - 1)
                {
                    GUILayout.Space(spacing);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (i + columns < cardDrawers.Count)
            {
                GUILayout.Space(spacing);
            }
        }
    }

    // ---------------------------------------------------------------
    // AGGREGATION (mevcut SuccessCount/TotalTests/AverageTime/DifficultyScore
    // degerlerinin toplanmasi/ortalamasi; yeni bir metrik uretilmiyor)
    // ---------------------------------------------------------------

    private class ProfileAggregate
    {
        public int SuccessCount;
        public int TotalCount;

        public float SuccessRate
        {
            get { return TotalCount > 0 ? (float)SuccessCount / TotalCount * 100f : 0f; }
        }
    }

    private class ProfileDisplayStats
    {
        public float AverageSteps;
        public float AverageWrongMoves;
        public float AverageDifficulty;
        public string Classification = "";
    }

    private Dictionary<string, ProfileAggregate> ComputeProfileAggregates()
    {
        Dictionary<string, ProfileAggregate> result = new Dictionary<string, ProfileAggregate>();

        foreach (KeyValuePair<string, MetricsSummary> kvp in MultiLevelTestRunner.LastPlayerSummaries)
        {
            string profileName = GetProfilePart(kvp.Key);

            ProfileAggregate aggregate;
            if (!result.TryGetValue(profileName, out aggregate))
            {
                aggregate = new ProfileAggregate();
                result[profileName] = aggregate;
            }

            aggregate.SuccessCount += kvp.Value.SuccessCount;
            aggregate.TotalCount += kvp.Value.TotalTests;
        }

        return result;
    }

    private Dictionary<string, ProfileDisplayStats> ComputeProfileDisplayStats()
    {
        Dictionary<string, ProfileDisplayStats> result = new Dictionary<string, ProfileDisplayStats>();

        if (MultiLevelTestRunner.LastSummary == null)
        {
            return result;
        }

        Dictionary<string, float> stepsSum = new Dictionary<string, float>();
        Dictionary<string, float> wrongMovesSum = new Dictionary<string, float>();
        Dictionary<string, float> difficultySum = new Dictionary<string, float>();
        Dictionary<string, int> countByProfile = new Dictionary<string, int>();

        foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
        {
            foreach (ProfileSummaryEntry entry in level.Profiles)
            {
                if (!countByProfile.ContainsKey(entry.Profile))
                {
                    stepsSum[entry.Profile] = 0f;
                    wrongMovesSum[entry.Profile] = 0f;
                    difficultySum[entry.Profile] = 0f;
                    countByProfile[entry.Profile] = 0;
                }

                stepsSum[entry.Profile] += entry.AverageSteps;
                wrongMovesSum[entry.Profile] += entry.AverageWrongMoves;
                difficultySum[entry.Profile] += entry.DifficultyScore;
                countByProfile[entry.Profile]++;
            }
        }

        foreach (KeyValuePair<string, int> kvp in countByProfile)
        {
            string profileName = kvp.Key;
            int count = kvp.Value;

            ProfileDisplayStats stats = new ProfileDisplayStats();
            if (count > 0)
            {
                stats.AverageSteps = stepsSum[profileName] / count;
                stats.AverageWrongMoves = wrongMovesSum[profileName] / count;
                stats.AverageDifficulty = difficultySum[profileName] / count;
                stats.Classification = ClassifyForDisplay(stats.AverageDifficulty);
            }

            result[profileName] = stats;
        }

        return result;
    }

    private float ComputeAverageDifficulty(LevelProfileSummary level)
    {
        if (level.Profiles.Count == 0)
        {
            return 0f;
        }

        float sum = 0f;
        foreach (ProfileSummaryEntry entry in level.Profiles)
        {
            sum += entry.DifficultyScore;
        }

        return sum / level.Profiles.Count;
    }

    private float ComputeOverallAverageDifficulty()
    {
        if (MultiLevelTestRunner.LastSummary == null || MultiLevelTestRunner.LastSummary.Levels.Count == 0)
        {
            return 0f;
        }

        float sum = 0f;
        int count = 0;

        foreach (LevelProfileSummary level in MultiLevelTestRunner.LastSummary.Levels)
        {
            foreach (ProfileSummaryEntry entry in level.Profiles)
            {
                sum += entry.DifficultyScore;
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;
    }

    private float ComputeAverageSolverSuccessRate()
    {
        int totalSuccess = 0;
        int totalRuns = 0;

        foreach (KeyValuePair<string, MetricsSummary> kvp in MultiLevelTestRunner.LastSolverSummaries)
        {
            totalSuccess += kvp.Value.SuccessCount;
            totalRuns += kvp.Value.TotalTests;
        }

        return totalRuns > 0 ? (float)totalSuccess / totalRuns * 100f : 0f;
    }

    private float ComputeTotalDurationSeconds()
    {
        float total = 0f;

        foreach (KeyValuePair<string, MetricsSummary> kvp in MultiLevelTestRunner.LastSolverSummaries)
        {
            total += kvp.Value.AverageTime * kvp.Value.TotalTests;
        }

        foreach (KeyValuePair<string, MetricsSummary> kvp in MultiLevelTestRunner.LastPlayerSummaries)
        {
            total += kvp.Value.AverageTime * kvp.Value.TotalTests;
        }

        return total;
    }

    private string FormatDuration(float totalSeconds)
    {
        int seconds = Mathf.Max(0, Mathf.RoundToInt(totalSeconds));
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int secs = seconds % 60;
        return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + secs.ToString("00");
    }

    // NOT: Bu esikler DifficultyAnalyzer.cs'deki Classify() metoduyla ayni
    // (0-3 EASY, 3-6 MEDIUM, 6-8 HARD, 8-10 VERY HARD). Sadece toplam/ortalama
    // skorlarin GORSEL renklendirmesi icin kullanilir (bu skorlarin karsiligi
    // olan tekil bir Classification alani veride yok, cunku DifficultyAnalyzer
    // hep tek bir profil/level ciftine gore calisir). Hicbir karar veya veri
    // uretmez; gercek Classification degerleri her zaman ProfileSummaryEntry.Classification'dan okunur.
    private string ClassifyForDisplay(float score)
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

    private string GetProfilePart(string combined)
    {
        int separatorIndex = combined.IndexOf(" - ");
        return separatorIndex >= 0 ? combined.Substring(separatorIndex + 3) : combined;
    }

    // ---------------------------------------------------------------
    // CARD / BORDER HELPERS
    // ---------------------------------------------------------------

    // PREMIUM REDESIGN NOTU: BeginColoredCard/EndColoredCard'in imzasi ve tum ~15+
    // cagri noktasi AYNEN korundu - sadece ic gorsel implementasyon (duz GUI.skin.box
    // yerine gercek yuvarlatilmis kose textur'u + ince glow) degisti. Hicbir Compute*/
    // veri-okuma metoduna dokunulmadi.
    private Rect BeginColoredCard(Color backgroundColor)
    {
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = backgroundColor;
        Rect rect = EditorGUILayout.BeginVertical(RoundedCardStyle());
        GUI.backgroundColor = previous;
        return rect;
    }

    private void EndColoredCard(Rect rect)
    {
        EditorGUILayout.EndVertical();
        if (Event.current.type == EventType.Repaint)
        {
            DrawBorder(rect, ColorBorder, 1f);
            // PREMIUM REDESIGN v2: kartin ust ve sol kenarinda, disaridaki normal
            // border'dan (ColorBorder) AYRI, cok dusuk alfali (0.035) ince bir "ic
            // isik" cizgisi - sanki ustten hafif bir isik dusuyormus hissi verir.
            // Tamamen dekoratif; kart genisligi/yerlesimi degismedi.
            DrawInnerBorder(rect, new Color(1f, 1f, 1f, 0.035f), 1f);
        }
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawInnerBorder(Rect rect, Color color, float thickness)
    {
        Rect inner = new Rect(rect.x + 1f, rect.y + 1f, Mathf.Max(0f, rect.width - 2f), Mathf.Max(0f, rect.height - 2f));
        EditorGUI.DrawRect(new Rect(inner.x, inner.y, inner.width, thickness), color);
        EditorGUI.DrawRect(new Rect(inner.x, inner.y, thickness, inner.height), color);
    }

    private GUIStyle BoxWithPadding()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.padding = new RectOffset(16, 16, 14, 14);
        style.margin = new RectOffset(0, 0, 0, 0);
        return style;
    }

    // ---------------------------------------------------------------
    // ROUNDED CORNERS (procedural texture, IMGUI'nin sagladigi tek guvenilir yol)
    //
    // Unity IMGUI'de gercek CSS-tarzi border-radius yok. Bunun yerine, kucuk
    // (32x32) bir alpha-maskeli beyaz kare texture'i BIR KEZ, kod ile (shader/
    // harici dependency olmadan) uretip GUIStyle.border (9-slice) ile kart
    // genisligine orantisiz sekilde geren bir teknik kullanildi. Baz texture
    // saf beyaz oldugu icin GUI.backgroundColor ile ayni mevcut tint mekanizmasi
    // (dosyanin her yerinde zaten kullanilan) calismaya devam eder.
    // ---------------------------------------------------------------

    private static readonly Dictionary<int, Texture2D> _roundedRectCache = new Dictionary<int, Texture2D>();
    private const int RoundedTextureSize = 32;

    private Texture2D GetRoundedRectTexture(int radius)
    {
        Texture2D cached;
        if (_roundedRectCache.TryGetValue(radius, out cached) && cached != null)
        {
            return cached;
        }

        int size = RoundedTextureSize;
        float r = radius;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                bool inLeftBand = px < r;
                bool inRightBand = px > size - r;
                bool inTopBand = py < r;
                bool inBottomBand = py > size - r;

                float alpha = 1f;

                if ((inLeftBand || inRightBand) && (inTopBand || inBottomBand))
                {
                    float cx = inLeftBand ? r : size - r;
                    float cy = inTopBand ? r : size - r;
                    float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    alpha = Mathf.Clamp01(r - dist + 0.5f);
                }

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _roundedRectCache[radius] = tex;
        return tex;
    }

    // PREMIUM REDESIGN v2: "katmanli yuzey" (layered surface) hissi icin, sadece
    // kart arka planlarinda kullanilan ayri bir rounded-rect texture. Tek fark:
    // alfa maskesi ayni (kose yuvarlama mantigi GetRoundedRectTexture ile birebir
    // ayni), ama RGB kanali ustten (parlak) alta (hafifce koyu) dogru cok ince bir
    // gradyan tasir (0.90 - 1.0 araligi -> gozle zor fark edilir, "extremely subtle").
    // Bu da GUI.backgroundColor ile carpimsal calistigi icin mevcut tint sistemini
    // BOZMAZ - sadece dogal, hafif bir "sheen" ekler.
    private static readonly Dictionary<int, Texture2D> _cardSurfaceCache = new Dictionary<int, Texture2D>();

    private Texture2D GetCardSurfaceTexture(int radius)
    {
        Texture2D cached;
        if (_cardSurfaceCache.TryGetValue(radius, out cached) && cached != null)
        {
            return cached;
        }

        int size = RoundedTextureSize;
        float r = radius;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            float verticalT = 1f - (float)y / (size - 1);
            float brightness = Mathf.Lerp(0.90f, 1.0f, verticalT);
            byte rgb = (byte)Mathf.RoundToInt(brightness * 255f);

            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                bool inLeftBand = px < r;
                bool inRightBand = px > size - r;
                bool inTopBand = py < r;
                bool inBottomBand = py > size - r;

                float alpha = 1f;

                if ((inLeftBand || inRightBand) && (inTopBand || inBottomBand))
                {
                    float cx = inLeftBand ? r : size - r;
                    float cy = inTopBand ? r : size - r;
                    float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    alpha = Mathf.Clamp01(r - dist + 0.5f);
                }

                pixels[y * size + x] = new Color32(rgb, rgb, rgb, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _cardSurfaceCache[radius] = tex;
        return tex;
    }

    // PREMIUM REDESIGN v2: kartlar artik duz tek renkli bir texture yerine, ustten
    // alta cok hafif bir parlaklik gecisi (katmanli yuzey / "sheen") tasiyan ayri bir
    // texture kullanir (GetCardSurfaceTexture). GUI.backgroundColor ile carpimsal
    // calisir - mevcut tint mekanizmasi (dosyanin her yerinde kullanilan) birebir
    // ayni sekilde gecerlidir, sadece tabanin kendisi artik duz beyaz degil.
    private GUIStyle RoundedCardStyle()
    {
        GUIStyle style = new GUIStyle();
        style.normal.background = GetCardSurfaceTexture(10);
        style.border = new RectOffset(10, 10, 10, 10);
        style.padding = new RectOffset(16, 16, 14, 14);
        style.margin = new RectOffset(0, 0, 0, 0);
        return style;
    }

    private GUIStyle RoundedPillStyle()
    {
        GUIStyle style = new GUIStyle();
        style.normal.background = GetRoundedRectTexture(7);
        style.border = new RectOffset(7, 7, 7, 7);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 10;
        style.normal.textColor = Color.white;
        return style;
    }

    private GUIStyle RoundedButtonStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.normal.background = GetRoundedRectTexture(8);
        style.hover.background = GetRoundedRectTexture(8);
        style.active.background = GetRoundedRectTexture(8);
        style.border = new RectOffset(8, 8, 8, 8);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = new Color(0.9f, 0.9f, 0.9f);
        return style;
    }

    // "Premium subtle glow": tek bir renkte, disari dogru giderek saydamlasan
    // 3 katmanli rounded-rect ciziyor. Gercek bir blur/shader degil ama IMGUI
    // icinde ekrani bogmadan hafif bir isik hissi veriyor - talep edilen
    // "subtle glow" hedefine uygun, performans acisindan ucuz (3 ekstra DrawTexture).
    private void DrawGlow(Rect rect, Color glowColor, int radius = 10, float spread = 5f)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        Texture2D tex = GetRoundedRectTexture(radius);
        Color previous = GUI.color;

        for (int i = 3; i >= 1; i--)
        {
            float expand = spread * i / 3f;
            Rect glowRect = new Rect(rect.x - expand, rect.y - expand, rect.width + expand * 2f, rect.height + expand * 2f);
            GUI.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * 0.10f);
            GUI.DrawTexture(glowRect, tex);
        }

        GUI.color = previous;
    }

    // ---------------------------------------------------------------
    // COLOR LOOKUP
    // ---------------------------------------------------------------

    private Color GetProfileColor(string profileName)
    {
        switch (profileName)
        {
            case "Careful":
                return ColorSuccess;
            case "Normal":
                return ColorSecondary;
            case "Impulsive":
                return ColorWarning;
            default:
                return ColorTextSecondary;
        }
    }

    private Color GetClassificationColor(string classification)
    {
        switch (classification)
        {
            case "EASY":
                return ColorSuccess;
            case "MEDIUM":
                return ColorPrimary;
            case "HARD":
                return ColorWarning;
            case "VERY HARD":
                return ColorDanger;
            default:
                return ColorTextMuted;
        }
    }

    // PREMIUM REDESIGN v2: "compact badges" icin boyut kucultuldu (80x18 -> 72x16).
    // Gosterilen "classification" metni ve rengi (GetClassificationColor) AYNI -
    // sadece rozetin fiziksel boyutu daha kompakt.
    private void DrawClassificationBadge(string classification)
    {
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = GetClassificationColor(classification);
        GUILayout.Box(classification, RoundedPillStyle(), GUILayout.Width(72), GUILayout.Height(16));
        GUI.backgroundColor = previousBackground;
    }

    // ---------------------------------------------------------------
    // TEXT / GUISTYLE HELPERS
    // ---------------------------------------------------------------

    private GUIStyle CardHeaderStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 14;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle SectionHeaderStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 16;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle MutedLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontSize = 11;
        style.normal.textColor = ColorTextSecondary;
        return style;
    }

    private GUIStyle ValueLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontSize = 12;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle KpiTitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
        style.fontSize = 10;
        style.normal.textColor = ColorTextSecondary;
        return style;
    }

    private GUIStyle KpiValueStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 30;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    // PREMIUM REDESIGN v2: sadece "PLAYER SUCCESS RATE" karti icin kullanilan,
    // biraz daha buyuk deger tipografisi (30 -> 34) - o karti gorsel odak noktasi
    // yapmanin bir parcasi. Gosterilen deger metninin kendisi degismedi.
    private GUIStyle KpiValueEmphasizedStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 34;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle KpiSubtitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.fontSize = 10;
        style.normal.textColor = ColorTextMuted;
        style.wordWrap = true;
        return style;
    }

    private GUIStyle PageTitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 21;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle PageSubtitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.fontSize = 12;
        style.normal.textColor = ColorTextSecondary;
        style.wordWrap = true;
        return style;
    }

    // NOT: StartButtonStyle()/BadgeStyle() (eski GUI.skin.button / GUI.skin.box tabanli
    // duz stiller) premium redesign'de RoundedButtonStyle()/RoundedPillStyle() ile
    // degistirildi ve tum cagri noktalari guncellendi; artik hicbir yerden
    // cagrilmadigi icin kaldirildi (unused-method temizligi).

    private GUIStyle ProfileNameStyle(Color color)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 13;
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle StatusTextStyle(Color color)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 13;
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle SidebarTitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 14;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle InsightValueStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 22;
        style.normal.textColor = ColorTextPrimary;
        return style;
    }

    private GUIStyle SidebarSubtitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = ColorTextMuted;
        style.wordWrap = true;
        return style;
    }

    private GUIStyle ActiveNavStyle()
    {
        GUIStyle style = new GUIStyle();
        style.normal.background = GetRoundedRectTexture(7);
        style.border = new RectOffset(7, 7, 7, 7);
        style.alignment = TextAnchor.MiddleLeft;
        style.padding = new RectOffset(12, 12, 4, 4);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        return style;
    }

    private GUIStyle InactiveNavStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.alignment = TextAnchor.MiddleLeft;
        style.padding = new RectOffset(12, 12, 4, 4);
        style.normal.textColor = ColorTextMuted;
        return style;
    }

    private GUIStyle FooterStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = ColorTextMuted;
        return style;
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}
