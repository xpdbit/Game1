/*
 * MCP Connection Watchdog (Unity Editor)
 * ──────────────────────────────────────────────
 * 功能: Unity 域重载（脚本编译）后自动恢复 MCP 连接
 *
 * 核心原则:
 *   - 零编译时依赖外部程序集 (MCP, R3, SignalR)
 *   - 反射失败通过日志暴露，绝不静默吞异常
 *   - 重连逻辑不依赖 keepConnected 反射结果
 *   - 互斥锁防止重连风暴
 */

#nullable enable

using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class McpConnectionWatchdog
{
    private const double MonitorIntervalSeconds = 3.0;
    private const int MaxRetriesBeforeLog = 3;
    private const int InitialReconnectDelayMs = 2000;
    private const int RetryDelayMs = 3000;

    private static readonly McpReflection? _mcp;
    private static double _lastMonitorTime;
    private static int _reconnectAttempts;
    private static bool _wasConnected;
    private static bool _initialized;
    private static bool _reconnecting; // 互斥锁，防止重连风暴
    private static bool _mcpUnavailable; // 仅警告一次

    static McpConnectionWatchdog()
    {
        _mcp = McpReflection.TryCreate();
        if (_mcp == null)
        {
            if (!_mcpUnavailable) { _mcpUnavailable = true; Debug.LogWarning("[MCP Watchdog] MCP Plugin 未检测到，看门狗已禁用。"); }
            return;
        }

        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // 设置日志级别 —— 仅尝试一次，失败不关键
        try { _mcp?.SetLogLevelWarning(); } catch { }
    }

    // ───── 域重载恢复 ─────

    private static void OnAfterAssemblyReload()
    {
        // 域重载后所有状态重置，准备重连
        _reconnectAttempts = 0;
        _wasConnected = false;
        _reconnecting = false;

        Debug.Log("[MCP Watchdog] 域重载 → 准备恢复 MCP 连接...");

        EditorApplication.delayCall += () =>
        {
            TryReconnect(InitialReconnectDelayMs, "域重载首次");
        };
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                TryReconnect(InitialReconnectDelayMs + 4000, "域重载二次");
            };
        };
    }

    // ───── 定期监控 ─────

    private static void OnEditorUpdate()
    {
        if (_mcp == null) return;

        var now = EditorApplication.timeSinceStartup;
        if (now - _lastMonitorTime < MonitorIntervalSeconds) return;
        _lastMonitorTime = now;

        var isConnected = _mcp.GetIsConnected();
        // 仅在无法通过反射判断时降级（不阻塞重连决策）
        if (isConnected == null)
        {
            Debug.LogWarning("[MCP Watchdog] 无法通过反射获取连接状态 (GetIsConnected 返回 null)，将尝试重连...");
            isConnected = false;
        }

        if (isConnected.Value)
        {
            if (!_wasConnected)
            {
                Debug.Log("[MCP Watchdog] ✓ MCP 连接正常");
                _reconnectAttempts = 0;
            }
            _wasConnected = true;
            return;
        }

        _wasConnected = false;

        // 重连互斥锁 —— 已有重连在进行则不重复发起
        if (_reconnecting)
            return;

        _reconnectAttempts++;
        if (_reconnectAttempts <= MaxRetriesBeforeLog)
            Debug.Log($"[MCP Watchdog] MCP 未连接，尝试重连... (第{_reconnectAttempts}次)");

        var delay = Mathf.Min(RetryDelayMs * _reconnectAttempts, 30000);
        TryReconnect(delay, "监控触发");
    }

    // ───── 重连核心逻辑 ─────

    private static async void TryReconnect(int delayMs, string reason)
    {
        if (_mcp == null) return;
        if (_reconnecting) return; // 双重校验互斥锁
        _reconnecting = true;

        await Task.Delay(delayMs);

        // 延迟后再次检查是否已连接（避免竞争）
        if (_mcp.GetIsConnected() == true)
        {
            _reconnecting = false;
            return;
        }

        try
        {
            // 策略1: ConnectIfNeeded (检查 keepConnected)
            var ok = await _mcp.ConnectIfNeeded();
            if (ok)
            {
                Debug.Log($"[MCP Watchdog] ✓ 重连成功 ({reason})");
                _reconnectAttempts = 0;
                _reconnecting = false;
                return;
            }

            // 策略2: ConnectIfNeeded 返回 false (可能 keepConnected 为 false)
            //        直接调用 Connect() 跳过 keepConnected 检查
            Debug.Log($"[MCP Watchdog] ConnectIfNeeded 返回 false, 尝试 Connect() ({reason})");
            var ok2 = await _mcp.Connect();
            if (ok2)
            {
                Debug.Log($"[MCP Watchdog] ✓ Connect() 重连成功 ({reason})");
                _reconnectAttempts = 0;
            }
            else
            {
                Debug.LogWarning($"[MCP Watchdog] Connect() 返回 false ({reason})");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MCP Watchdog] 重连失败: {ex.Message} ({reason})");
        }
        finally
        {
            _reconnecting = false;
        }
    }

    // ────────────────────────────────────────────
    //  反射封装
    // ────────────────────────────────────────────
    private class McpReflection
    {
        private readonly System.Type _editorType;
        private readonly System.Type? _logLevelType;
        private readonly object? _logLevelWarning;

        private McpReflection(System.Type editorType)
        {
            _editorType = editorType;

            _logLevelType = System.Type.GetType(
                "com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevel, com.IvanMurzak.Unity.MCP.Runtime");
            if (_logLevelType != null && _logLevelType.IsEnum)
                _logLevelWarning = System.Enum.Parse(_logLevelType, "Warning");
        }

        public static McpReflection? TryCreate()
        {
            try
            {
                var t = System.Type.GetType(
                    "com.IvanMurzak.Unity.MCP.UnityMcpPluginEditor, com.IvanMurzak.Unity.MCP.Editor");
                if (t != null) return new McpReflection(t);
                Debug.LogWarning("[MCP Watchdog] 无法解析 MCP Editor 程序集，看门狗禁用。");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MCP Watchdog] TryCreate 异常: {ex.Message}");
                return null;
            }
        }

        // ── 连接状态: 返回 true/false，null 表示反射失败 ──

        public bool? GetIsConnected()
        {
            try
            {
                var prop = _editorType.GetProperty("IsConnected",
                    BindingFlags.Public | BindingFlags.Static);
                var rp = prop?.GetValue(null);
                if (rp == null)
                {
                    Debug.LogWarning("[MCP Watchdog] IsConnected 属性反射为 null");
                    return null;
                }

                var rpType = rp.GetType();
                var val = rpType.GetProperty("CurrentValue") ?? rpType.GetProperty("Value");
                if (val == null)
                {
                    Debug.LogWarning($"[MCP Watchdog] ReadOnlyReactiveProperty 上未找到 CurrentValue/Value");
                    return null;
                }
                return (bool?)val.GetValue(rp);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MCP Watchdog] GetIsConnected 反射异常: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        // ── 连接方法 ──

        public async Task<bool> ConnectIfNeeded()
        {
            try
            {
                var method = _editorType.GetMethod("ConnectIfNeeded",
                    BindingFlags.Public | BindingFlags.Static, null, System.Type.EmptyTypes, null);
                if (method == null) { Debug.LogWarning("[MCP Watchdog] ConnectIfNeeded 方法未找到"); return false; }
                return await (Task<bool>)(method.Invoke(null, null) ?? Task.FromResult(false));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MCP Watchdog] ConnectIfNeeded 反射异常: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Connect()
        {
            try
            {
                var method = _editorType.GetMethod("Connect",
                    BindingFlags.Public | BindingFlags.Static, null, System.Type.EmptyTypes, null);
                if (method == null) { Debug.LogWarning("[MCP Watchdog] Connect 方法未找到"); return false; }
                return await (Task<bool>)(method.Invoke(null, null) ?? Task.FromResult(false));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MCP Watchdog] Connect 反射异常: {ex.Message}");
                return false;
            }
        }

        // ── 其他 ──

        public void SetLogLevelWarning()
        {
            try
            {
                if (_logLevelType == null || _logLevelWarning == null) return;
                var prop = _editorType.GetProperty("LogLevel",
                    BindingFlags.Public | BindingFlags.Static);
                prop?.SetValue(null, _logLevelWarning);
            }
            catch { }
        }
    }
}
