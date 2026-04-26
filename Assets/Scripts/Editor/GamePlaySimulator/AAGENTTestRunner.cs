using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Test result data
    /// </summary>
    [Serializable]
    public class ATestResult
    {
        public string testName = string.Empty;
        public bool passed;
        public string message = string.Empty;
        public float value;
        public float expected;
        public string details = string.Empty;
    }

    /// <summary>
    /// Test session result
    /// </summary>
    [Serializable]
    public class ATestSessionResult
    {
        public int seed;
        public int duration; // seconds
        public int goldEarned;
        public int expEarned;
        public int levelReached;
        public int battlesWon;
        public int battlesLost;
        public int locationsVisited;
        public int milestonesReached;
        public float winRate;
        public List<ATestResult> testResults = new();
    }

    /// <summary>
    /// Balance issue found during testing
    /// </summary>
    [Serializable]
    public class ABalanceIssue
    {
        public string category = string.Empty; // combat, economy, progression
        public string severity = string.Empty; // critical, major, minor
        public string description = string.Empty;
        public string suggestion = string.Empty;
        public float measuredValue;
        public float expectedValue;
    }

    /// <summary>
    /// Automated test runner for game simulation
    /// </summary>
    public class AAGENTTestRunner
    {
        private List<ATestSessionResult> _sessionResults = new();
        private List<ABalanceIssue> _issues = new();

        #region Test Cases
        /// <summary>
        /// Test idle reward accumulation rate
        /// </summary>
        public ATestResult TestIdleRewardRate(int seed, float durationSeconds, float expectedGoldPerSecond)
        {
            var player = new ASimulatedPlayer(seed);
            var idle = new ASimulatedIdle(player);

            idle.Simulate(durationSeconds);

            float actualRate = idle.GoldEarned / durationSeconds;
            float diff = Mathf.Abs(actualRate - expectedGoldPerSecond) / expectedGoldPerSecond;

            return new ATestResult
            {
                testName = "IdleRewardRate",
                passed = diff < 0.1f, // 10% tolerance
                value = actualRate,
                expected = expectedGoldPerSecond,
                message = diff < 0.1f
                    ? $"Idle rate OK: {actualRate:F2} g/s"
                    : $"Idle rate mismatch: {actualRate:F2} vs expected {expectedGoldPerSecond:F2}",
                details = $"Seed={seed}, Duration={durationSeconds}s"
            };
        }

        /// <summary>
        /// Test combat win rate against enemy of similar level
        /// </summary>
        public ATestResult TestCombatWinRate(int seed, int playerLevel, int enemyLevel, int iterations)
        {
            int wins = 0;
            var combat = new ASimulatedCombat(seed);

            for (int i = 0; i < iterations; i++)
            {
                int sessionSeed = seed + i;
                var player = new ASimulatedPlayer(sessionSeed);
                player.Generate(sessionSeed, playerLevel);

                var enemy = combat.GenerateEnemy(playerLevel, sessionSeed + 1000);

                var result = combat.Execute(player, enemy);
                if (result.playerVictory) wins++;
            }

            float winRate = (float)wins / iterations;
            float expectedWinRate = 0.7f; // 70% expected

            return new ATestResult
            {
                testName = "CombatWinRate",
                passed = Mathf.Abs(winRate - expectedWinRate) < 0.2f, // 20% tolerance
                value = winRate,
                expected = expectedWinRate,
                message = $"Win rate: {winRate:P0} (expected ~{expectedWinRate:P0})",
                details = $"PlayerLv={playerLevel}, EnemyLv={enemyLevel}, Iterations={iterations}"
            };
        }

        /// <summary>
        /// Test level progression speed
        /// </summary>
        public ATestResult TestLevelProgression(int seed, int targetLevel, float maxDurationSeconds)
        {
            var player = new ASimulatedPlayer(seed);
            var idle = new ASimulatedIdle(player);
            var combat = new ASimulatedCombat(seed);

            float elapsed = 0f;
            float tick = 0.1f;

            while (elapsed < maxDurationSeconds && player.level < targetLevel)
            {
                idle.Module.Tick(tick);

                // Simulate combat every 30 seconds
                if (elapsed % 30 < tick)
                {
                    var enemy = combat.GenerateEnemy(player.level, seed + (int)elapsed);
                    var result = combat.Execute(player, enemy);
                    if (result.playerVictory)
                    {
                        player.AddExp(result.expReward);
                    }
                }

                elapsed += tick;
            }

            bool passed = player.level >= targetLevel;

            return new ATestResult
            {
                testName = "LevelProgression",
                passed = passed,
                value = player.level,
                expected = targetLevel,
                message = passed
                    ? $"Reached level {player.level} in {elapsed:F0}s"
                    : $"Only reached level {player.level} in {elapsed:F0}s (max {maxDurationSeconds}s)",
                details = $"Seed={seed}, TargetLevel={targetLevel}"
            };
        }

        /// <summary>
        /// Test gold economy balance
        /// </summary>
        public ATestResult TestEconomyBalance(int seed, float durationSeconds)
        {
            var player = new ASimulatedPlayer(seed);
            var idle = new ASimulatedIdle(player);
            var combat = new ASimulatedCombat(seed);

            int goldBefore = player.carryItems.gold;

            // Simulate with combat
            float elapsed = 0f;
            float tick = 0.1f;
            int combatCount = 0;

            while (elapsed < durationSeconds)
            {
                idle.Module.Tick(tick);

                // Combat every 10 seconds
                if (elapsed % 10 < tick)
                {
                    var enemy = combat.GenerateEnemy(player.level, seed + combatCount);
                    var result = combat.Execute(player, enemy);
                    if (result.playerVictory)
                    {
                        player.AddExp(result.expReward);
                    }
                    combatCount++;
                }

                elapsed += tick;
            }

            int goldGained = player.carryItems.gold - goldBefore;
            float goldPerMinute = goldGained / (durationSeconds / 60f);

            // Expected: gold shouldn't grow too fast (inflation) or too slow
            float expectedGoldPerMinute = 60f; // Adjust based on balance
            float ratio = goldPerMinute / expectedGoldPerMinute;

            return new ATestResult
            {
                testName = "EconomyBalance",
                passed = ratio > 0.5f && ratio < 2f,
                value = goldPerMinute,
                expected = expectedGoldPerMinute,
                message = $"Gold rate: {goldPerMinute:F1}/min (expected ~{expectedGoldPerMinute:F1}/min)",
                details = $"Seed={seed}, CombatCount={combatCount}, GoldGained={goldGained}"
            };
        }

        /// <summary>
        /// Test offline reward calculation
        /// </summary>
        public ATestResult TestOfflineReward(int seed, float offlineHours)
        {
            var player = new ASimulatedPlayer(seed);
            var idle = new ASimulatedIdle(player);

            int goldBefore = player.carryItems.gold;
            int reward = idle.SimulateOffline(offlineHours);
            int goldAfter = player.carryItems.gold;

            // Verify offline reward was calculated
            bool passed = goldAfter > goldBefore;

            return new ATestResult
            {
                testName = "OfflineReward",
                passed = passed,
                value = reward,
                expected = 0,
                message = passed
                    ? $"Offline reward: {reward} gold for {offlineHours:F1} hours"
                    : "No offline reward received",
                details = $"GoldBefore={goldBefore}, GoldAfter={goldAfter}"
            };
        }

        /// <summary>
        /// Test travel progress rate
        /// </summary>
        public ATestResult TestTravelProgress(int seed, float durationSeconds)
        {
            var player = new ASimulatedPlayer(seed);
            var travel = new ASimulatedTravel(player, seed);

            travel.StartTravel();

            float elapsed = 0f;
            float tick = 0.1f;
            float maxProgress = 0f;

            while (elapsed < durationSeconds)
            {
                travel.Tick(tick);
                maxProgress = Mathf.Max(maxProgress, travel.TravelProgress);
                elapsed += tick;

                if (travel.CurrentLocation != null &&
                    travel.CurrentLocation.isMilestone &&
                    travel.TravelProgress >= 1f)
                {
                    break; // Reached milestone
                }
            }

            var stats = travel.GetStatistics();

            return new ATestResult
            {
                testName = "TravelProgress",
                passed = stats.locationsVisited > 0,
                value = stats.locationsVisited,
                expected = 1,
                message = $"Visited {stats.locationsVisited} locations, TravelRate={stats.avgTravelRate:F2} TP/s",
                details = $"Seed={seed}, Elapsed={elapsed:F0}s"
            };
        }
        #endregion

        #region Test Session
        /// <summary>
        /// Run a complete test session
        /// </summary>
        public ATestSessionResult RunSession(int seed, int durationSeconds)
        {
            var result = new ATestSessionResult
            {
                seed = seed,
                duration = durationSeconds
            };

            var player = new ASimulatedPlayer(seed);
            var idle = new ASimulatedIdle(player);
            var combat = new ASimulatedCombat(seed);
            var travel = new ASimulatedTravel(player, seed);

            float elapsed = 0f;
            float tick = 0.1f;

            // Subscribe to events
            combat.GetStatistics(); // Reset stats

            while (elapsed < durationSeconds)
            {
                // Update systems
                idle.Module.Tick(tick);
                travel.Tick(tick);

                // Combat every 15 seconds
                if (elapsed % 15 < tick)
                {
                    var enemy = combat.GenerateEnemy(player.level, seed + (int)(elapsed / 15));
                    var combatResult = combat.Execute(player, enemy);
                    if (combatResult.playerVictory)
                    {
                        player.AddExp(combatResult.expReward);
                    }
                }

                // Auto travel
                if (!travel.IsTraveling)
                {
                    travel.StartTravel();
                }

                elapsed += tick;

                // Safety break
                if (elapsed > durationSeconds * 2) break;
            }

            // Collect results
            result.goldEarned = player.carryItems.gold;
            result.expEarned = player.level * 100; // Approximate
            result.levelReached = player.level;

            var combatStats = combat.GetStatistics();
            result.battlesWon = combatStats.victories;
            result.battlesLost = combatStats.defeats;
            result.winRate = combatStats.winRate;

            var travelStats = travel.GetStatistics();
            result.locationsVisited = travelStats.locationsVisited;
            result.milestonesReached = travel.Progress.milestoneCount;

            _sessionResults.Add(result);
            return result;
        }

        /// <summary>
        /// Run multiple sessions and analyze
        /// </summary>
        public List<ATestSessionResult> RunMultiSession(int sessionCount, int durationPerSession)
        {
            var results = new List<ATestSessionResult>();

            for (int i = 0; i < sessionCount; i++)
            {
                int seed = 1000 + i;
                var result = RunSession(seed, durationPerSession);
                results.Add(result);
            }

            return results;
        }
        #endregion

        #region Balance Analysis
        /// <summary>
        /// Analyze results and identify balance issues
        /// </summary>
        public List<ABalanceIssue> AnalyzeBalance()
        {
            _issues.Clear();

            if (_sessionResults.Count == 0)
            {
                Debug.LogWarning("[AAGENTTestRunner] No session results to analyze");
                return _issues;
            }

            // Calculate averages
            float avgWinRate = 0f;
            float avgGoldPerMinute = 0f;
            float avgLevelProgress = 0f;

            foreach (var session in _sessionResults)
            {
                avgWinRate += session.winRate;
                avgGoldPerMinute += session.goldEarned / (session.duration / 60f);
                avgLevelProgress += session.levelReached;
            }

            avgWinRate /= _sessionResults.Count;
            avgGoldPerMinute /= _sessionResults.Count;
            avgLevelProgress /= _sessionResults.Count;

            // Check combat balance
            if (avgWinRate < 0.5f)
            {
                _issues.Add(new ABalanceIssue
                {
                    category = "combat",
                    severity = "critical",
                    description = $"Win rate too low: {avgWinRate:P0}",
                    suggestion = "Reduce enemy stats or increase player damage",
                    measuredValue = avgWinRate,
                    expectedValue = 0.7f
                });
            }
            else if (avgWinRate > 0.9f)
            {
                _issues.Add(new ABalanceIssue
                {
                    category = "combat",
                    severity = "minor",
                    description = $"Win rate too high: {avgWinRate:P0}",
                    suggestion = "Enemy stats can be increased for challenge",
                    measuredValue = avgWinRate,
                    expectedValue = 0.7f
                });
            }

            // Check economy
            if (avgGoldPerMinute < 30f)
            {
                _issues.Add(new ABalanceIssue
                {
                    category = "economy",
                    severity = "major",
                    description = $"Gold acquisition too slow: {avgGoldPerMinute:F1}/min",
                    suggestion = "Increase idle reward rate or combat gold rewards",
                    measuredValue = avgGoldPerMinute,
                    expectedValue = 60f
                });
            }
            else if (avgGoldPerMinute > 200f)
            {
                _issues.Add(new ABalanceIssue
                {
                    category = "economy",
                    severity = "minor",
                    description = $"Gold acquisition high: {avgGoldPerMinute:F1}/min",
                    suggestion = "Monitor for inflation issues",
                    measuredValue = avgGoldPerMinute,
                    expectedValue = 60f
                });
            }

            return _issues;
        }

        /// <summary>
        /// Generate test report
        /// </summary>
        public string GenerateReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== AGENT Test Report ===");
            report.AppendLine($"Sessions Run: {_sessionResults.Count}");
            report.AppendLine();

            if (_sessionResults.Count > 0)
            {
                float avgWinRate = 0f;
                float avgGold = 0f;
                float avgLevel = 0f;

                foreach (var session in _sessionResults)
                {
                    avgWinRate += session.winRate;
                    avgGold += session.goldEarned;
                    avgLevel += session.levelReached;
                }

                avgWinRate /= _sessionResults.Count;
                avgGold /= _sessionResults.Count;
                avgLevel /= _sessionResults.Count;

                report.AppendLine($"Average Win Rate: {avgWinRate:P0}");
                report.AppendLine($"Average Gold Earned: {avgGold:F0}");
                report.AppendLine($"Average Level Reached: {avgLevel:F1}");
                report.AppendLine();
            }

            if (_issues.Count > 0)
            {
                report.AppendLine("Balance Issues:");
                foreach (var issue in _issues)
                {
                    report.AppendLine($"  [{issue.severity.ToUpper()}] {issue.category}: {issue.description}");
                    report.AppendLine($"    Suggestion: {issue.suggestion}");
                }
            }
            else
            {
                report.AppendLine("No critical balance issues found.");
            }

            return report.ToString();
        }
        #endregion

        #region Quick Tests
        /// <summary>
        /// Run quick validation tests
        /// </summary>
        public List<ATestResult> RunQuickTests()
        {
            var results = new List<ATestResult>();

            results.Add(TestIdleRewardRate(42, 60f, 1f));
            results.Add(TestCombatWinRate(42, 5, 5, 10));
            results.Add(TestOfflineReward(42, 8f));
            results.Add(TestLevelProgression(42, 3, 300f));

            return results;
        }
        #endregion

        #region Getters
        public IReadOnlyList<ATestSessionResult> SessionResults => _sessionResults;
        public IReadOnlyList<ABalanceIssue> Issues => _issues;
        #endregion
    }
}