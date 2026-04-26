# Game1 CI/CD 流水线设计方案

**项目**: Game1 - Unity 6 挂机放置类游戏
**创建时间**: 2026-04-26
**基于**: Unity测试框架研究 + GameCI最佳实践

---

## 一、CI/CD架构概览

### 1.1 推荐工具链

| 组件 | 推荐工具 | 说明 |
|------|----------|------|
| 源码管理 | GitHub | 已使用 |
| CI引擎 | GitHub Actions | 免费额度，免费私有仓库 |
| Unity许可 | game-ci/unity-license-activation | 安全激活 |
| 构建服务 | game-ci/unity-builder | 官方推荐 |
| 测试报告 | com.unity.testtools.codecoverage | 官方覆盖率工具 |
| 制品存储 | GitHub Artifacts | 免费额度 |
| 发布平台 | itch.io / Steam | 目标平台 |

### 1.2 流水线阶段

```
┌─────────────────────────────────────────────────────────────┐
│                    CI/CD 流水线                              │
├─────────────────────────────────────────────────────────────┤
│  1. 触发条件: push / PR / tag                                │
│  ↓                                                           │
│  2. 检出代码: git clone + LFS                                │
│  ↓                                                           │
│  3. 缓存: Library + Packages                                │
│  ↓                                                           │
│  4. Unity激活: game-ci/unity-license-activation            │
│  ↓                                                           │
│  5. 构建: game-ci/unity-builder (多平台矩阵)                │
│  ↓                                                           │
│  6. 测试: EditMode + PlayMode + 覆盖率                      │
│  ↓                                                           │
│  7. 静态分析: Roslyn analyzers                              │
│  ↓                                                           │
│  8. 制品上传: GitHub Artifacts                               │
│  ↓                                                           │
│  9. 通知: Discord/Slack webhook (可选)                       │
│  ↓                                                           │
│  10. 发布: GitHub Release / itch.io                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 二、工作流文件设计

### 2.1 主构建工作流 (.github/workflows/build.yml)

```yaml
name: Build

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  release:
    types: [published]

env:
  UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
  UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
  UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}

jobs:
  build:
    name: Build (${{ matrix.targetPlatform }})
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        targetPlatform:
          - StandaloneWindows64
          - StandaloneOSX
          - StandaloneLinux64
          - WebGL
    
    steps:
      - name: 检出代码
        uses: actions/checkout@v4
        with:
          lfs: true
          fetch-depth: 0
      
      - name: 缓存Unity Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ matrix.targetPlatform }}-${{ hashFiles('**/Packages/packages-lock.json') }}
          restore-keys: |
            Library-${{ matrix.targetPlatform }}-
      
      - name: 激活Unity许可
        uses: game-ci/unity-license-activation@v4
        with:
          licenseType: annual
      
      - name: 构建
        uses: game-ci/unity-builder@v4
        with:
          targetPlatform: ${{ matrix.targetPlatform }}
          buildMethod: Game1.BuildScript.BuildStandalone
          versioning: Tag
      
      - name: 上传制品
        uses: actions/upload-artifact@v4
        with:
          name: Build-${{ matrix.targetPlatform }}
          path: build/${{ matrix.targetPlatform }}
          retention-days: 90

  test:
    name: Test (EditMode + PlayMode)
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      test-mode:
        - EditMode
        - PlayMode
    
    steps:
      - name: 检出代码
        uses: actions/checkout@v4
      
      - name: 缓存Unity Library
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-test-${{ hashFiles('**/Packages/packages-lock.json') }}
      
      - name: 激活Unity许可
        uses: game-ci/unity-license-activation@v4
        with:
          licenseType: annual
      
      - name: 运行测试
        uses: game-ci/unity-test-runner@v4
        with:
          testMode: ${{ matrix.test-mode }}
          artifactsPath: ${{ matrix.test-mode }}-artifacts
      
      - name: 上传测试结果
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Test-Results-${{ matrix.test-mode }}
          path: ${{ matrix.test-mode }}-artifacts

  release:
    name: Release
    needs: [build, test]
    runs-on: ubuntu-latest
    if: github.event_name == 'release'
    
    steps:
      - name: 检出代码
        uses: actions/checkout@v4
      
      - name: 下载所有制品
        uses: actions/download-artifact@v4
      
      - name: 创建Release
        uses: softprops/action-gh-release@v1
        with:
          files: Build-*/**
          draft: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### 2.2 CI触发工作流 (.github/workflows/ci.yml)

```yaml
name: CI

on:
  push:
    branches-ignore: [main, develop]
  pull_request:
    branches: [main]

env:
  UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}

jobs:
  quality-checks:
    name: Quality Checks
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: 缓存
        uses: actions/cache@v4
        with:
          path: Library
          key: Library-ci-${{ hashFiles('**/Packages/packages-lock.json') }}
      
      - name: 激活Unity许可
        uses: game-ci/unity-license-activation@v4
      
      - name: EditMode测试
        uses: game-ci/unity-test-runner@v4
        with:
          testMode: EditMode
      
      - name: 覆盖率
        uses: game-ci/unity-test-runner@v4
        with:
          testMode: EditMode
          coverageOptions: generateAdditionalReports

  android-build:
    name: Build Android
    runs-on: ubuntu-latest
    if: github.event_name == 'push'
    
    steps:
      - uses: actions/checkout@v4
      
      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-android-${{ hashFiles('**/Packages/packages-lock.json') }}
      
      - name: 激活Unity许可
        uses: game-ci/unity-license-activation@v4
      
      - name: 构建Android
        uses: game-ci/unity-builder@v4
        with:
          targetPlatform: Android
          buildMethod: Game1.BuildScript.BuildAndroid
```

---

## 三、测试框架设计

### 3.1 测试目录结构

```
Assets/
├── Tests/
│   ├── Editor/
│   │   ├── Assembly/
│   │   │   ├── GameCore.Tests.Editor.asmdef
│   │   │   └── GameCore.Tests.Editor.asmdef.meta
│   │   ├── Runtime/
│   │   │   ├── Assembly/
│   │   │   │   ├── GameCore.Tests.asmdef
│   │   │   │   └── GameCore.Tests.asmdef.meta
│   │   ├── EditMode/
│   │   │   ├── Core/
│   │   │   │   ├── SaveSystem/
│   │   │   │   │   ├── SaveManagerTests.cs
│   │   │   │   │   └── SaveDataSerializationTests.cs
│   │   │   │   ├── EventBus/
│   │   │   │   │   └── EventBusTests.cs
│   │   │   │   └── Utils/
│   │   │   │       └── UtilsTests.cs
│   │   │   ├── Modules/
│   │   │   │   ├── Inventory/
│   │   │   │   │   └── InventoryDesignTests.cs
│   │   │   │   ├── Team/
│   │   │   │   │   └── TeamDesignTests.cs
│   │   │   │   ├── Card/
│   │   │   │   │   └── CardManagerTests.cs
│   │   │   │   └── Combat/
│   │   │   │       └── CombatSystemTests.cs
│   │   │   └── Events/
│   │   │       └── EventTreeRunnerTests.cs
│   │   └── PlayMode/
│   │       ├── Integration/
│   │       │   ├── TravelFlowTests.cs
│   │       │   ├── CombatFlowTests.cs
│   │       │   └── SaveLoadFlowTests.cs
│   │       └── Performance/
│   │           └── TickPerformanceTests.cs
```

### 3.2 单元测试模板

```csharp
#if UNITY_EDITOR
namespace Game1.Tests.Core.SaveSystem
{
    /// <summary>
    /// SaveManager 单元测试
    /// 验收标准:
    /// - [ ] 存档创建成功
    /// - [ ] 存档读取成功
    /// - [ ] 存档覆盖正确
    /// - [ ] 存档删除正确
    /// - [ ] 异常情况处理正确
    /// </summary>
    public class SaveManagerTests
    {
        private SaveManager _saveManager;
        
        [UnitySetUp]
        public void Setup()
        {
            _saveManager = new SaveManager();
        }
        
        [UnityTearDown]
        public void Teardown()
        {
            // 清理测试数据
            _saveManager.DeleteSave("test_save");
        }
        
        [UnityTest]
        public IEnumerator CreateSave_ShouldCreateNewSaveFile()
        {
            // Arrange
            var testData = new GameSaveData
            {
                playerName = "TestPlayer",
                playTime = TimeSpan.FromMinutes(30),
                timestamp = DateTime.Now
            };
            
            // Act
            var result = _saveManager.CreateSave("test_save", testData);
            
            // Assert
            Assert.IsTrue(result, "存档创建应该成功");
            Assert.IsTrue(_saveManager.SaveExists("test_save"), "存档文件应该存在");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator LoadSave_ShouldReturnCorrectData()
        {
            // Arrange
            var testData = new GameSaveData { playerName = "TestPlayer" };
            _saveManager.CreateSave("test_save", testData);
            
            // Act
            var loadedData = _saveManager.LoadSave("test_save");
            
            // Assert
            Assert.IsNotNull(loadedData, "读取的存档不应该为空");
            Assert.AreEqual("TestPlayer", loadedData.playerName, "玩家名称应该匹配");
            
            yield return null;
        }
    }
}
#endif
```

### 3.3 集成测试模板

```csharp
#if UNITY_EDITOR
namespace Game1.Tests.Integration
{
    /// <summary>
    /// 旅行流程集成测试
    /// 验收标准:
    /// - [ ] 旅行开始正确
    /// - [ ] 节点前进正确
    /// - [ ] 事件触发正确
    /// - [ ] 旅行完成正确
    /// </summary>
    public class TravelFlowTests
    {
        private PlayerActor _player;
        private TravelManager _travelManager;
        
        [UnitySetUp]
        public IEnumerator Setup()
        {
            // 创建测试环境
            _player = new GameObject("TestPlayer").AddComponent<PlayerActor>();
            _player.Initialize();
            
            _travelManager = new TravelManager();
            _travelManager.Initialize(_player);
            
            yield return null;
        }
        
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_player != null)
                UnityEngine.Object.Destroy(_player.gameObject);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator StartTravel_ShouldSetStateToTraveling()
        {
            // Act
            _travelManager.StartTravel();
            
            // Assert
            Assert.AreEqual(TravelState.Traveling, _travelManager.CurrentState);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator ProgressNode_ShouldAdvanceProgress()
        {
            // Arrange
            var initialProgress = _travelManager.CurrentProgress;
            
            // Act
            _travelManager.ProgressNode();
            
            // Assert
            Assert.Greater(_travelManager.CurrentProgress, initialProgress);
            
            yield return null;
        }
    }
}
#endif
```

---

## 四、覆盖率配置

### 4.1 覆盖率报告配置

```yaml
# 在unity-builder中添加覆盖率选项
- name: 构建并测试覆盖率
  uses: game-ci/unity-test-runner@v4
  with:
    testMode: EditMode
    artifactsPath: coverage-artifacts
    coverageOptions: |
      generateAdditionalReports;
      reportFormats=HTML,Cobertura;
      assemblyFilters=GameCore.Runtime.*;
```

### 4.2 覆盖率目标

| 模块 | 覆盖率目标 |
|------|------------|
| Core (EventBus, SaveSystem) | 90% |
| Modules/Inventory | 85% |
| Modules/Team | 85% |
| Modules/Combat | 80% |
| Modules/Skill | 80% |
| Modules/Card | 80% |
| Events | 75% |
| UI | 60% |

---

## 五、版本管理策略

### 5.1 SemVer版本格式

```
{major}.{minor}.{patch}-{pre-release}+{build}
例如: 1.0.0-alpha+20260426
```

### 5.2 版本递增规则

| 触发 | 递增 |
|------|------|
| 主分支合并 | minor +1 |
| PR合并 | patch +1 |
| 正式发布 | major +1 |
| 每日构建 | 自动timestamp |

### 5.3 自动版本脚本

```csharp
// BuildScript.cs
public class BuildScript
{
    public static void BuildStandalone()
    {
        var versions = Environment.GetEnvironmentVariable("GITHUB_REF")?.Split('/');
        var version = versions?.Length > 2 ? versions[2] : "0.0.0";
        
        PlayerSettings.bundleVersion = version;
        
        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = "build/StandaloneWindows64/Game1.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        UnityEngine.Debug.Log($"Building version {version}");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
```

---

## 六、安全与通知

### 6.1 敏感信息管理

| 信息 | 存储位置 | 说明 |
|------|----------|------|
| Unity许可 | GitHub Secrets | 加密存储 |
| Unity账号 | GitHub Secrets | 加密存储 |
| 访问令牌 | GitHub Secrets | 仅CI使用 |

### 6.2 通知配置

```yaml
- name: 通知Discord
  if: always()
  uses: ravansc/discord-build-notify@v1
  with:
    webhook: ${{ secrets.DISCORD_WEBHOOK }}
    status: ${{ job.status }}
    mention: '@here'
```

### 6.3 失败处理

```yaml
- name: 构建失败通知
  if: failure()
  run: |
    echo "::error::Build failed! Check artifacts for details."
    echo "::group::Recent Commits"
    git log --oneline -5
    echo "::endgroup"
```

---

## 七、实施计划

### 7.1 第一阶段 (立即)

1. 创建 `.github/workflows/` 目录
2. 添加 `build.yml` 和 `ci.yml`
3. 配置 GitHub Secrets
4. 测试CI流水线

### 7.2 第二阶段 (短期内)

1. 添加EditMode测试
2. 配置覆盖率报告
3. 设置PR要求(必须通过CI)

### 7.3 第三阶段 (中期)

1. 添加PlayMode测试
2. 配置多平台构建
3. 设置自动发布

---

## 八、文档版本

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-04-26 | 初始版本 |

