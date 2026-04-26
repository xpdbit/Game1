using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game1.GamePlay
{
    /// <summary>
    /// Simulation mode
    /// </summary>
    public enum ASimulationMode
    {
        EditMode,  // Fast simulation without visual rendering
        PlayMode   // Full simulation with visuals
    }

    /// <summary>
    /// Main game simulator coordinator
    /// Coordinates all simulation systems (Player, Combat, Idle, Travel, UI, Visuals)
    /// </summary>
    public class AGameSimulator : MonoBehaviour
    {
        #region Singleton
        private static AGameSimulator _instance;
        public static AGameSimulator instance => _instance;
        #endregion

        #region Configuration
        [Header("Simulation Settings")]
        public ASimulationMode mode = ASimulationMode.EditMode;
        public int defaultSeed = 42;
        public float simulationSpeed = 100f; // 100x real-time
        public bool autoRunTests = true;

        [Header("Visual Settings")]
        public bool showVisuals = true;
        public bool showUI = true;
        #endregion

        #region Core Systems
        private ASimulatedPlayer _player;
        private ASimulatedIdle _idle;
        private ASimulatedCombat _combat;
        private ASimulatedTravel _travel;
        private ASimulatedUI _ui;
        private ASimulatedVisuals _visuals;
        private AAGENTTestRunner _testRunner;
        #endregion

        #region UI References
        private AUIElements _uiElements;
        private Canvas _canvas;
        #endregion

        #region State
        private bool _isRunning;
        private float _simulationTime;
        private float _tickInterval = 0.1f;
        private float _tickTimer;
        private int _currentSeed;
        #endregion

        #region Events
        public event System.Action<string> onLogMessage;
        public event System.Action onSimulationStarted;
        public event System.Action onSimulationStopped;
        public event System.Action<ATestResult> onTestCompleted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isRunning) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer = 0f;
                float deltaTime = _tickInterval * simulationSpeed;
                Tick(deltaTime);
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the simulator
        /// </summary>
        public void Initialize()
        {
            Log("Initializing AGAME Simulator...");

            // Initialize RNG
            _currentSeed = defaultSeed;
            UnityEngine.Random.InitState(_currentSeed);

            // Create core systems
            _player = new ASimulatedPlayer(_currentSeed);
            _idle = new ASimulatedIdle(_player);
            _combat = new ASimulatedCombat(_currentSeed);
            _travel = new ASimulatedTravel(_player, _currentSeed);
            _testRunner = new AAGENTTestRunner();

            // Generate initial player
            _player.Generate(_currentSeed, 5); // Level 5

            // Initialize visuals if in play mode
            if (mode == ASimulationMode.PlayMode && showVisuals)
            {
                InitializeVisuals();
            }

            // Initialize UI if enabled
            if (showUI)
            {
                InitializeUI();
            }

            Log($"Simulator initialized. Seed: {_currentSeed}, Player: {_player.actorName} Lv.{_player.level}");
        }

        /// <summary>
        /// Initialize visual systems
        /// </summary>
        private void InitializeVisuals()
        {
            Log("Initializing visuals...");
            _visuals = new ASimulatedVisuals();

            // Create camera
            _visuals.CreateCamera(
                new Vector3(0, 10, -10),
                new Vector3(0, 0, 0)
            );

            // Create light
            _visuals.CreateDirectionalLight(
                new Vector3(0, -1, 1).normalized,
                1.0f
            );

            // Create player character visual
            _visuals.CreateSimpleCharacter(
                new Vector3(0, 0, 0),
                Color.blue
            );

            // Create ground plane
            _visuals.CreatePlane(
                new Vector3(0, -0.5f, 0),
                new Vector2(20, 20),
                Color.gray
            );

            Log("Visuals initialized.");
        }

        /// <summary>
        /// Initialize UI systems
        /// </summary>
        private void InitializeUI()
        {
            Log("Initializing UI...");

            // Create canvas
            _canvas = ASimulatedUI.CreateCanvas("AGameCanvas");
            var canvasTransform = _canvas.transform;

            // Create main panel
            var panel = ASimulatedUI.CreatePanel(
                canvasTransform,
                "MainPanel",
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(300, 0),
                new Vector2(0, 0),
                new Color(0, 0, 0, 0.8f)
            );

            // Create stats display
            _uiElements = new AUIElements
            {
                rootCanvas = _canvas.gameObject,
                goldText = CreateLabel(panel.transform, "Gold: 0", new Vector2(0, -30), new Vector2(250, 30)),
                levelText = CreateLabel(panel.transform, "Level: 1", new Vector2(0, -70), new Vector2(250, 30)),
                healthBar = ASimulatedUI.CreateProgressBar(
                    panel.transform,
                    "HealthBar",
                    new Vector2(0, 0.5f),
                    new Vector2(1, 0.5f),
                    new Vector2(250, 25),
                    new Vector2(0, -110)
                ),
                progressBar = ASimulatedUI.CreateProgressBar(
                    panel.transform,
                    "ProgressBar",
                    new Vector2(0, 0.5f),
                    new Vector2(1, 0.5f),
                    new Vector2(250, 25),
                    new Vector2(0, -150)
                ),
                logText = CreateLabel(panel.transform, "Log...", new Vector2(0, -200), new Vector2(250, 200))
            };

            // Create control buttons
            CreateButton(panel.transform, "Start", new Vector2(0, -420), () => StartSimulation());
            CreateButton(panel.transform, "Stop", new Vector2(0, -470), () => StopSimulation());
            CreateButton(panel.transform, "Run Tests", new Vector2(0, -520), () => RunTests());
            CreateButton(panel.transform, "Skip Time (1h)", new Vector2(0, -570), () => SkipTime(3600));

            Log("UI initialized.");
        }

        private GameObject CreateLabel(Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = 18;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;

            return go;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            ASimulatedUI.CreateButton(
                parent,
                label,
                label.Replace(" ", "") + "Btn",
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(150, 40),
                pos,
                onClick
            );
        }
        #endregion

        #region Simulation Control
        /// <summary>
        /// Start the simulation
        /// </summary>
        public void StartSimulation()
        {
            if (_isRunning) return;

            _isRunning = true;
            _simulationTime = 0f;
            Log("Simulation started.");
            onSimulationStarted?.Invoke();
        }

        /// <summary>
        /// Stop the simulation
        /// </summary>
        public void StopSimulation()
        {
            if (!_isRunning) return;

            _isRunning = false;
            Log($"Simulation stopped. Time: {_simulationTime:F1}s");
            onSimulationStopped?.Invoke();
        }

        /// <summary>
        /// Reset the simulation
        /// </summary>
        public void ResetSimulation()
        {
            StopSimulation();
            _simulationTime = 0f;

            // Reinitialize
            _player = new ASimulatedPlayer(_currentSeed);
            _player.Generate(_currentSeed, 5);
            _idle = new ASimulatedIdle(_player);
            _combat = new ASimulatedCombat(_currentSeed);
            _travel = new ASimulatedTravel(_player, _currentSeed);

            Log("Simulation reset.");
        }

        /// <summary>
        /// Skip time in simulation
        /// </summary>
        public void SkipTime(float seconds)
        {
            Log($"Skipping {seconds:F0}s...");

            float elapsed = 0f;
            float tick = 0.1f;

            while (elapsed < seconds)
            {
                float delta = Mathf.Min(tick, seconds - elapsed);
                Tick(delta);
                elapsed += delta;
            }

            Log($"Skip complete. Current time: {_simulationTime:F1}s");
        }
        #endregion

        #region Tick
        /// <summary>
        /// Main simulation tick
        /// </summary>
        private void Tick(float deltaTime)
        {
            _simulationTime += deltaTime;

            // Update all systems
            _idle.Module.Tick(deltaTime);
            _travel.Tick(deltaTime);

            // Update UI
            UpdateUI();

            // Check for milestones
            CheckMilestones();
        }

        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (_uiElements == null) return;

            // Update gold
            ASimulatedUI.UpdateText(_uiElements.goldText, $"Gold: {_player.carryItems.gold}");

            // Update level
            ASimulatedUI.UpdateText(_uiElements.levelText, $"Level: {_player.level} ({_player.stats.currentHp}/{_player.stats.maxHp} HP)");

            // Update health bar
            if (_uiElements.healthBar != null)
            {
                float hpRatio = (float)_player.stats.currentHp / _player.stats.maxHp;
                ASimulatedUI.UpdateProgressBar(_uiElements.healthBar, hpRatio * 100, 100);
            }

            // Update travel progress
            if (_uiElements.progressBar != null && _travel != null)
            {
                ASimulatedUI.UpdateProgressBar(_uiElements.progressBar, _travel.TravelProgress * 100, 100);
            }
        }

        private void CheckMilestones()
        {
            if (_travel.Progress.milestoneCount > 0)
            {
                Log($"Milestone reached! Count: {_travel.Progress.milestoneCount}");
            }
        }
        #endregion

        #region Testing
        /// <summary>
        /// Run automated tests
        /// </summary>
        public void RunTests()
        {
            Log("Running automated tests...");

            var results = _testRunner.RunQuickTests();

            foreach (var result in results)
            {
                string status = result.passed ? "PASS" : "FAIL";
                Log($"[{status}] {result.testName}: {result.message}");
                onTestCompleted?.Invoke(result);
            }

            // Generate report
            string report = _testRunner.GenerateReport();
            Log("=== Test Report ===");
            Log(report);
        }

        /// <summary>
        /// Run full simulation session
        /// </summary>
        public void RunFullSession(int durationSeconds)
        {
            Log($"Starting full session simulation for {durationSeconds}s...");

            ResetSimulation();
            StartSimulation();

            // Run in background - actual implementation would use coroutines
            // For now, just start
        }

        /// <summary>
        /// Analyze balance
        /// </summary>
        public void AnalyzeBalance()
        {
            var issues = _testRunner.AnalyzeBalance();

            if (issues.Count == 0)
            {
                Log("No critical balance issues found.");
            }
            else
            {
                foreach (var issue in issues)
                {
                    Log($"[{issue.severity.ToUpper()}] {issue.category}: {issue.description}");
                    Log($"  Suggestion: {issue.suggestion}");
                }
            }
        }
        #endregion

        #region Logging
        /// <summary>
        /// Log a message
        /// </summary>
        public void Log(string message)
        {
            string timestamped = $"[{_simulationTime:F1}s] {message}";
            Debug.Log($"[AGameSimulator] {timestamped}");
            onLogMessage?.Invoke(timestamped);
        }
        #endregion

        #region Public Access
        public ASimulatedPlayer Player => _player;
        public ASimulatedIdle Idle => _idle;
        public ASimulatedCombat Combat => _combat;
        public ASimulatedTravel Travel => _travel;
        public ASimulatedUI UI => _ui;
        public ASimulatedVisuals Visuals => _visuals;
        public AAGENTTestRunner TestRunner => _testRunner;
        public bool IsRunning => _isRunning;
        public float SimulationTime => _simulationTime;
        #endregion

        #region Editor Support
        /// <summary>
        /// Run simulation in edit mode (for quick testing)
        /// </summary>
        [ContextMenu("Run EditMode Simulation")]
        public void RunEditModeSimulation()
        {
            mode = ASimulationMode.EditMode;
            Initialize();

            // Run quick tests
            var results = _testRunner.RunQuickTests();

            foreach (var result in results)
            {
                string status = result.passed ? "✓" : "✗";
                Debug.Log($"[{status}] {result.testName}: {result.message}");
            }
        }

        /// <summary>
        /// Run full balance analysis
        /// </summary>
        [ContextMenu("Run Balance Analysis")]
        public void RunBalanceAnalysis()
        {
            Initialize();

            // Run multi-session analysis
            var sessions = _testRunner.RunMultiSession(5, 300); // 5 sessions, 5 min each
            var issues = _testRunner.AnalyzeBalance();

            Debug.Log("=== Balance Analysis ===");
            foreach (var issue in issues)
            {
                Debug.Log($"[{issue.severity.ToUpper()}] {issue.category}: {issue.description}");
            }
        }
        #endregion
    }

    /// <summary>
    /// Attribute to mark a method as a test case
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ATestCaseAttribute : Attribute
    {
        public string name;
        public string description;

        public ATestCaseAttribute(string name, string description = "")
        {
            this.name = name;
            this.description = description;
        }
    }
}