using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Game configuration (should be provided by GameMain or external source)
    /// Uses AConfig from ASimulatorConfig.cs
    /// </summary>
    [Serializable]
    public class ALocationData
    {
        public string id;
        public string name;
        public string description;
        public bool hasEvent;
        public bool isMilestone;
        public List<string> connectedLocationIds = new();
        public float baseTravelTime = 10f;

        public ALocationData(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    /// <summary>
    /// Progress manager simulation
    /// </summary>
    [Serializable]
    public class ASimulatedProgress
    {
        public float points;
        public float travelRate; // Points per second (60-second sliding window average)
        private float[] _rateSamples = new float[60];
        private int _sampleIndex;
        private float _sampleAccumulator;

        public int milestoneCount;

        public void AddPoints(float pointsToAdd)
        {
            points += pointsToAdd;

            // Update sliding window
            _sampleAccumulator -= _rateSamples[_sampleIndex];
            _rateSamples[_sampleIndex] = pointsToAdd;
            _sampleAccumulator += pointsToAdd;
            _sampleIndex = (_sampleIndex + 1) % 60;

            // Calculate average
            travelRate = _sampleAccumulator / 60f;

            // Check milestones (every 1000 points)
            while (points >= 1000f)
            {
                points -= 1000f;
                milestoneCount++;
            }
        }

        public void Reset()
        {
            points = 0f;
            milestoneCount = 0;
            travelRate = 0f;
            _rateSamples = new float[60];
            _sampleIndex = 0;
            _sampleAccumulator = 0f;
        }
    }

    /// <summary>
    /// Travel simulation (mirrors TravelManager + ProgressManager)
    /// </summary>
    public class ASimulatedTravel
    {
        private ASimulatedPlayer _player;
        private ASimulatedProgress _progress;
        private List<ALocationData> _locations;
        private ALocationData _currentLocation;
        private ALocationData _nextLocation;
        private float _travelProgress;
        private float _travelTimeRequired;
        private System.Random _rng;

        // Events
        public event System.Action<ALocationData> onArrived;
        public event System.Action<int> onMilestone;
        public event System.Action<float> onProgressUpdated;

        public ASimulatedTravel(ASimulatedPlayer player, int seed)
        {
            _player = player;
            _progress = new ASimulatedProgress();
            _locations = new List<ALocationData>();
            _rng = new System.Random(seed);

            GenerateLocations();
            SetCurrentLocation(_locations[0]);
        }

        #region Location Generation
        private void GenerateLocations()
        {
            _locations.Clear();

            // Generate a chain of locations
            var locationNames = new[] { "起点", "森林", "河流", "山脉", "城镇", "沙漠", "废墟", "神殿", "终点" };

            for (int i = 0; i < locationNames.Length; i++)
            {
                var loc = new ALocationData(
                    $"loc_{i}",
                    locationNames[i]
                );
                loc.description = $"位于{locationNames[i]}的地区";
                loc.hasEvent = _rng.NextDouble() < 0.3;
                loc.isMilestone = i == locationNames.Length - 1;
                loc.baseTravelTime = 5f + (float)_rng.NextDouble() * 10f;

                if (i > 0)
                    loc.connectedLocationIds.Add(_locations[i - 1].id);
                if (i < locationNames.Length - 1)
                    loc.connectedLocationIds.Add($"loc_{i + 1}");

                _locations.Add(loc);
            }
        }

        private void SetCurrentLocation(ALocationData location)
        {
            _currentLocation = location;
            _player.travelState.currentLocationId = location.id;
        }
        #endregion

        #region Travel Control
        /// <summary>
        /// Start traveling to next location
        /// </summary>
        public void StartTravel()
        {
            if (_locations == null || _locations.Count < 2) return;

            int currentIndex = _locations.FindIndex(l => l.id == _currentLocation.id);
            int nextIndex = Mathf.Min(currentIndex + 1, _locations.Count - 1);

            _nextLocation = _locations[nextIndex];

            // Calculate travel time with bonuses
            float speedBonus = _player.GetTotalBonus("travel_speed");
            _travelTimeRequired = _nextLocation.baseTravelTime / (1f + speedBonus);

            _player.travelState.currentState = ATravelState.State.Traveling;
            _player.travelState.nextLocationId = _nextLocation.id;
            _player.travelState.realTimeRequired = _travelTimeRequired;
            _travelProgress = 0f;

            Debug.Log($"[ASimulatedTravel] Started travel: {_currentLocation.name} -> {_nextLocation.name}, Time: {_travelTimeRequired:F1}s");
        }

        /// <summary>
        /// Update travel progress
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Add progress points based on travel
            if (_player.travelState.currentState == ATravelState.State.Traveling)
            {
                // Calculate points per second (travelRate = TP/s)
                float pointsPerSecond = AConfig.Active.pointsPerSecond * (1f + _player.GetTotalBonus("travel_speed"));
                _progress.AddPoints(pointsPerSecond * deltaTime);

                // Update travel progress
                _travelProgress += deltaTime / _travelTimeRequired;
                _player.travelState.progress = _travelProgress;
                onProgressUpdated?.Invoke(_travelProgress);

                // Check if arrived
                if (_travelProgress >= 1f)
                {
                    CompleteTravel();
                }
            }
            else if (_player.travelState.currentState == ATravelState.State.Idle)
            {
                // Idle generates fewer points
                float pointsPerSecond = AConfig.Active.pointsPerSecond * 0.5f;
                _progress.AddPoints(pointsPerSecond * deltaTime);
            }
        }

        private void CompleteTravel()
        {
            _player.travelState.progress = 1f;
            _player.travelState.currentState = ATravelState.State.Arrived;
            SetCurrentLocation(_nextLocation);

            Debug.Log($"[ASimulatedTravel] Arrived at {_currentLocation.name}!");
            onArrived?.Invoke(_currentLocation);

            // Check for milestone
            if (_currentLocation.isMilestone)
            {
                _progress.milestoneCount++;
                onMilestone?.Invoke(_progress.milestoneCount);
                Debug.Log($"[ASimulatedTravel] Milestone reached! Count: {_progress.milestoneCount}");
            }

            // Trigger event if present
            if (_currentLocation.hasEvent)
            {
                _player.travelState.currentState = ATravelState.State.EventPending;
                Debug.Log($"[ASimulatedTravel] Event triggered at {_currentLocation.name}!");
            }

            // Reset for next travel
            _player.travelState.currentState = ATravelState.State.Idle;
            _player.travelState.nextLocationId = null;
            _player.travelState.realTimeRequired = 0f;
            _player.travelState.progress = 0f;
        }
        #endregion

        #region Getters
        public ALocationData CurrentLocation => _currentLocation;
        public ALocationData NextLocation => _nextLocation;
        public ASimulatedProgress Progress => _progress;
        public float TravelProgress => _travelProgress;
        public List<ALocationData> Locations => _locations;
        public bool IsTraveling => _player.travelState.currentState == ATravelState.State.Traveling;
        #endregion

        #region Simulation Helpers
        /// <summary>
        /// Fast-forward travel without events
        /// </summary>
        public void SkipToNextLocation()
        {
            while (!_currentLocation.isMilestone)
            {
                SetCurrentLocation(_locations[Mathf.Min(
                    _locations.FindIndex(l => l.id == _currentLocation.id) + 1,
                    _locations.Count - 1)]);
                _progress.AddPoints(200f); // Normal event threshold
            }
            _progress.AddPoints(1000f); // Event tree threshold
        }

        /// <summary>
        /// Get travel statistics
        /// </summary>
        public (int locationsVisited, float totalPoints, float avgTravelRate) GetStatistics()
        {
            int visited = _locations.FindIndex(l => l.id == _currentLocation.id) + 1;
            return (visited, _progress.points, _progress.travelRate);
        }
        #endregion
    }
}