# AudioMixer 配置指南

## 创建步骤

### 1. 创建 AudioMixer 资产
1. 在 Unity Editor 中，右键点击 `Assets/Audio/` 文件夹
2. 选择 `Create > Audio Mixer`
3. 命名为 `MasterMixer`

### 2. 配置通道组 (Groups)

创建以下三个通道组：
- [x] **Master** (根)
  - [x] **BGM**
  - [x] **SFX**
  - [x] **Voice**

### 3. 设置 Exposed Parameters

为每个通道组添加音量参数（右键通道组 → `Add Exposed Parameter`）：

| 通道组 | 参数名称 | 说明 |
|--------|----------|------|
| BGM | `BGM_Volume` | 背景音乐音量 |
| SFX | `SFX_Volume` | 音效音量 |
| Voice | `Voice_Volume` | 语音音量 |

### 4. 设置 Attenuation

为每个通道组添加 Volume Attenuation 效果：
1. 点击通道组
2. 在 Inspector 中点击 `Add Effect` → `Attenuation`
3. 将 Volume 旋钮调到 0dB

## 资源目录结构

```
Assets/
├── Audio/
│   ├── MasterMixer.mixer     # AudioMixer 资产
│   ├── BGM/
│   │   ├── MainMenu.wav
│   │   ├── Overworld.wav
│   │   ├── Combat.wav
│   │   └── ...
│   ├── SFX/
│   │   ├── UI/
│   │   │   ├── Click.wav
│   │   │   ├── Hover.wav
│   │   │   └── ...
│   │   ├── Combat/
│   │   │   ├── Hit.wav
│   │   │   ├── Crit.wav
│   │   │   └── ...
│   │   └── ...
│   └── Voice/
│       ├── Combat/
│       └── ...
```

## 预设音效 ID 映射

### UI 音效
| 预设ID | 资源路径 |
|--------|----------|
| `SFX/UI/Click` | `Assets/Audio/SFX/UI/Click.wav` |

### 战斗音效
| 预设ID | 资源路径 |
|--------|----------|
| `SFX/Combat/Hit` | `Assets/Audio/SFX/Combat/Hit.wav` |
| `SFX/Combat/Crit` | `Assets/Audio/SFX/Combat/Crit.wav` |
| `SFX/Combat/Death` | `Assets/Audio/SFX/Combat/Death.wav` |

### BGM
| 预设ID | 资源路径 |
|--------|----------|
| `BGM/MainMenu` | `Assets/Audio/BGM/MainMenu.wav` |

## 代码中启用资源加载

在 `AudioManager.cs` 中取消注释资源加载代码：

```csharp
private AudioClip LoadAudioClip(string audioId)
{
    // 实际资源加载
    string path = $"Audio/{audioId}";
    return Resources.Load<AudioClip>(path);
}
```

## 在 GameMain 中初始化

在 `GameMain.cs` 的 `Initialize()` 方法中添加：

```csharp
// 初始化音频管理器
AudioManager.instance.Initialize();
```

在 `Update()` 方法中添加：

```csharp
// 更新音频管理器
AudioManager.instance.Update(Time.deltaTime);
```