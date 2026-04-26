using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Game1.Core.Audio
{
    /// <summary>
    /// 音频通道枚举
    /// </summary>
    public enum AudioChannel
    {
        BGM,
        SFX,
        Voice
    }

    /// <summary>
    /// 音频管理器 - 单例模式
    /// 支持BGM循环播放、音效叠加、音量控制、预设音效触发
    /// </summary>
    public class AudioManager
    {
        #region Singleton
        private static AudioManager _instance;
        public static AudioManager instance => _instance ??= new AudioManager();
        #endregion

        #region Constants
        private const string AUDIO_MIXER_PATH = "Audio/MasterMixer";
        private const float DEFAULT_FADE_TIME = 1f;
        private const float MAX_SFX_SOURCES = 16f;
        #endregion

        #region Fields
        private AudioMixer _audioMixer;
        private AudioSource _bgmSource;
        private readonly List<AudioSource> _sfxSources = new();
        private AudioSource _voiceSource;

        // 音量状态 (0~1)
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        private float _voiceVolume = 1f;

        // BGM渐变状态
        private bool _isBgmFading;
        private float _bgmFadeTargetVolume;
        private float _bgmFadeSpeed;

        // 当前BGM信息
        private string _currentBgmId;
        private bool _isBgmPaused;
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化音频管理器
        /// </summary>
        public void Initialize()
        {
            // 加载AudioMixer
            LoadAudioMixer();

            // 创建BGM AudioSource
            CreateBGMSource();

            // 创建SFX AudioSource池
            CreateSFXSourcePool();

            // 创建Voice AudioSource
            CreateVoiceSource();

            Debug.Log("[AudioManager] Initialized successfully.");
        }

        private void LoadAudioMixer()
        {
            // 预留加载路径 - 实际资源添加后启用
            // _audioMixer = Resources.Load<AudioMixer>(AUDIO_MIXER_PATH);
            _audioMixer = null;

            if (_audioMixer == null)
            {
                Debug.LogWarning("[AudioManager] AudioMixer not found. Using default settings.");
            }
        }

        private void CreateBGMSource()
        {
            var go = new GameObject("BGM_Source");
            go.transform.SetParent(GetAudioRoot());
            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.outputAudioMixerGroup = GetMixerGroup(AudioChannel.BGM);
        }

        private void CreateSFXSourcePool()
        {
            for (int i = 0; i < MAX_SFX_SOURCES; i++)
            {
                var go = new GameObject($"SFX_Source_{i}");
                go.transform.SetParent(GetAudioRoot());
                var source = go.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                source.outputAudioMixerGroup = GetMixerGroup(AudioChannel.SFX);
                source.gameObject.SetActive(false);
                _sfxSources.Add(source);
            }
        }

        private void CreateVoiceSource()
        {
            var go = new GameObject("Voice_Source");
            go.transform.SetParent(GetAudioRoot());
            _voiceSource = go.AddComponent<AudioSource>();
            _voiceSource.loop = false;
            _voiceSource.playOnAwake = false;
            _voiceSource.outputAudioMixerGroup = GetMixerGroup(AudioChannel.Voice);
        }

        private Transform GetAudioRoot()
        {
            var audioRoot = GameObject.Find("AudioManager");
            if (audioRoot == null)
            {
                audioRoot = new GameObject("AudioManager");
            }
            return audioRoot.transform;
        }

        private AudioMixerGroup GetMixerGroup(AudioChannel channel)
        {
            if (_audioMixer == null) return null;

            string groupName = channel switch
            {
                AudioChannel.BGM => "BGM",
                AudioChannel.SFX => "SFX",
                AudioChannel.Voice => "Voice",
                _ => "Master"
            };

            return _audioMixer.FindMatchingGroups(groupName).FirstOrDefault();
        }
        #endregion

        #region BGM Control
        /// <summary>
        /// 播放BGM
        /// </summary>
        /// <param name="audioId">预设BGM ID或资源路径</param>
        /// <param name="fadeInTime">渐入时间(秒)</param>
        public void PlayBGM(string audioId, float fadeInTime = DEFAULT_FADE_TIME)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                Debug.LogWarning("[AudioManager] BGM audioId is null or empty.");
                return;
            }

            var clip = LoadAudioClip(audioId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM clip not found: {audioId}");
                return;
            }

            // 如果切换BGM，先停止当前的
            if (_currentBgmId != audioId)
            {
                StopBGM(fadeInTime);
            }

            _currentBgmId = audioId;
            _bgmSource.clip = clip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            // 渐入
            if (fadeInTime > 0)
            {
                StartBGMFade(_bgmVolume, fadeInTime);
            }
            else
            {
                _bgmSource.volume = _bgmVolume;
            }

            _isBgmPaused = false;
            Debug.Log($"[AudioManager] Playing BGM: {audioId}");
        }

        /// <summary>
        /// 停止BGM
        /// </summary>
        /// <param name="fadeOutTime">渐出时间(秒)</param>
        public void StopBGM(float fadeOutTime = DEFAULT_FADE_TIME)
        {
            if (!_bgmSource.isPlaying) return;

            if (fadeOutTime > 0)
            {
                StartBGMFade(0f, fadeOutTime, () =>
                {
                    _bgmSource.Stop();
                    _bgmSource.clip = null;
                    _currentBgmId = null;
                });
            }
            else
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
                _currentBgmId = null;
            }
        }

        /// <summary>
        /// 暂停BGM
        /// </summary>
        public void PauseBGM()
        {
            if (_bgmSource.isPlaying)
            {
                _bgmSource.Pause();
                _isBgmPaused = true;
            }
        }

        /// <summary>
        /// 恢复BGM
        /// </summary>
        public void ResumeBGM()
        {
            if (_isBgmPaused)
            {
                _bgmSource.UnPause();
                _isBgmPaused = false;
            }
        }

        /// <summary>
        /// 设置BGM音量
        /// </summary>
        /// <param name="volume">音量 (0~1)</param>
        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            if (!_isBgmFading)
            {
                _bgmSource.volume = _bgmVolume;
            }
            UpdateMixerVolume(AudioChannel.BGM, _bgmVolume);
        }

        /// <summary>
        /// 获取BGM音量
        /// </summary>
        public float GetBGMVolume() => _bgmVolume;

        /// <summary>
        /// 检查BGM是否正在播放
        /// </summary>
        public bool IsBGMPlaying() => _bgmSource.isPlaying;

        private void StartBGMFade(float targetVolume, float duration, Action onComplete = null)
        {
            if (duration <= 0)
            {
                _bgmSource.volume = targetVolume;
                onComplete?.Invoke();
                return;
            }

            _isBgmFading = true;
            _bgmFadeTargetVolume = targetVolume;
            _bgmFadeSpeed = Mathf.Abs(targetVolume - _bgmSource.volume) / duration;
        }
        #endregion

        #region SFX Control
        /// <summary>
        /// 播放SFX音效
        /// </summary>
        /// <param name="audioId">预设SFX ID或资源路径</param>
        /// <param name="volumeScale">音量缩放 (0~1, 默认1)</param>
        public void PlaySFX(string audioId, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                Debug.LogWarning("[AudioManager] SFX audioId is null or empty.");
                return;
            }

            var clip = LoadAudioClip(audioId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX clip not found: {audioId}");
                return;
            }

            // 查找空闲的AudioSource
            var source = GetFreeSFXSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] No free SFX source available.");
                return;
            }

            source.gameObject.SetActive(true);
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.Play();

            // 播放完毕后隐藏
            ScheduleSFXDeactivation(source, clip.length);
        }

        /// <summary>
        /// 停止所有SFX
        /// </summary>
        public void StopAllSFX()
        {
            foreach (var source in _sfxSources)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
                source.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 设置SFX音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            UpdateMixerVolume(AudioChannel.SFX, _sfxVolume);
        }

        /// <summary>
        /// 获取SFX音量
        /// </summary>
        public float GetSFXVolume() => _sfxVolume;

        private AudioSource GetFreeSFXSource()
        {
            // 先找空闲的
            foreach (var source in _sfxSources)
            {
                if (!source.isPlaying && !source.gameObject.activeSelf)
                {
                    return source;
                }
            }

            // 如果没有空闲的，复用最旧的
            AudioSource oldest = null;
            float oldestTime = float.MaxValue;
            foreach (var source in _sfxSources)
            {
                if (source.time < oldestTime)
                {
                    oldestTime = source.time;
                    oldest = source;
                }
            }
            return oldest;
        }

        private void ScheduleSFXDeactivation(AudioSource source, float delay)
        {
            // 使用协程延迟关闭
            CoroutineHelper.StartCoroutine(DeactivateAfterDelay(source, delay));
        }

        private System.Collections.IEnumerator DeactivateAfterDelay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (source != null)
            {
                source.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Voice Control
        /// <summary>
        /// 播放语音
        /// </summary>
        /// <param name="audioId">预设语音ID或资源路径</param>
        public void PlayVoice(string audioId)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                Debug.LogWarning("[AudioManager] Voice audioId is null or empty.");
                return;
            }

            var clip = LoadAudioClip(audioId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Voice clip not found: {audioId}");
                return;
            }

            _voiceSource.Stop();
            _voiceSource.clip = clip;
            _voiceSource.volume = _voiceVolume;
            _voiceSource.Play();
        }

        /// <summary>
        /// 停止语音
        /// </summary>
        public void StopVoice()
        {
            if (_voiceSource.isPlaying)
            {
                _voiceSource.Stop();
            }
        }

        /// <summary>
        /// 设置语音音量
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            _voiceSource.volume = _voiceVolume;
            UpdateMixerVolume(AudioChannel.Voice, _voiceVolume);
        }

        /// <summary>
        /// 获取语音音量
        /// </summary>
        public float GetVoiceVolume() => _voiceVolume;
        #endregion

        #region Volume Control
        /// <summary>
        /// 设置指定通道的音量
        /// </summary>
        public void SetVolume(AudioChannel channel, float volume)
        {
            switch (channel)
            {
                case AudioChannel.BGM:
                    SetBGMVolume(volume);
                    break;
                case AudioChannel.SFX:
                    SetSFXVolume(volume);
                    break;
                case AudioChannel.Voice:
                    SetVoiceVolume(volume);
                    break;
            }
        }

        /// <summary>
        /// 获取指定通道的音量
        /// </summary>
        public float GetVolume(AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.BGM => _bgmVolume,
                AudioChannel.SFX => _sfxVolume,
                AudioChannel.Voice => _voiceVolume,
                _ => 1f
            };
        }

        private void UpdateMixerVolume(AudioChannel channel, float volume)
        {
            if (_audioMixer == null) return;

            string paramName = channel switch
            {
                AudioChannel.BGM => "BGM_Volume",
                AudioChannel.SFX => "SFX_Volume",
                AudioChannel.Voice => "Voice_Volume",
                _ => "Master_Volume"
            };

            // 将0~1映射到-80dB~0dB
            float dbValue = volume > 0 ? Mathf.Log(volume) * 20f : -80f;
            _audioMixer.SetFloat(paramName, dbValue);
        }
        #endregion

        #region Preset Sound Effects
        /// <summary>
        /// 播放战斗打击音效
        /// </summary>
        /// <param name="isCrit">是否为暴击</param>
        public void PlayCombatHit(bool isCrit)
        {
            PlaySFX(isCrit ? SFXPreset.Combat_Crit : SFXPreset.Combat_Hit);
        }

        /// <summary>
        /// 播放战斗死亡音效
        /// </summary>
        /// <param name="isPlayer">是否为玩家</param>
        public void PlayCombatDeath(bool isPlayer)
        {
            if (isPlayer)
            {
                PlaySFX(SFXPreset.Combat_Death);
            }
            else
            {
                PlaySFX(SFXPreset.Combat_Death);
            }
        }

        /// <summary>
        /// 播放UI点击音效
        /// </summary>
        public void PlayUIClick()
        {
            PlaySFX(SFXPreset.UI_Click);
        }

        /// <summary>
        /// 播放UI确认音效
        /// </summary>
        public void PlayUIConfirm()
        {
            PlaySFX(SFXPreset.UI_Confirm);
        }

        /// <summary>
        /// 播放UI取消音效
        /// </summary>
        public void PlayUICancel()
        {
            PlaySFX(SFXPreset.UI_Cancel);
        }

        /// <summary>
        /// 播放UI悬停音效
        /// </summary>
        public void PlayUIHover()
        {
            PlaySFX(SFXPreset.UI_Hover);
        }

        /// <summary>
        /// 播放事件完成音效
        /// </summary>
        /// <param name="success">是否成功</param>
        public void PlayEventComplete(bool success)
        {
            PlaySFX(success ? SFXPreset.Event_Complete : SFXPreset.Event_Failed);
        }

        /// <summary>
        /// 播放战斗胜利音效
        /// </summary>
        public void PlayCombatVictory()
        {
            PlaySFX(SFXPreset.Combat_Victory);
        }

        /// <summary>
        /// 播放获得物品音效
        /// </summary>
        public void PlayItemGet()
        {
            PlaySFX(SFXPreset.Item_Get);
        }
        #endregion

        #region Update Loop
        /// <summary>
        /// 每帧更新 (从GameLoop调用)
        /// </summary>
        public void Update(float deltaTime)
        {
            // 处理BGM渐变
            UpdateBGMFade(deltaTime);
        }

        private void UpdateBGMFade(float deltaTime)
        {
            if (!_isBgmFading) return;

            float currentVolume = _bgmSource.volume;
            float direction = _bgmFadeTargetVolume > currentVolume ? 1f : -1f;
            float newVolume = currentVolume + _bgmFadeSpeed * direction * deltaTime;

            // 检查是否到达目标
            if (direction > 0 && newVolume >= _bgmFadeTargetVolume)
            {
                newVolume = _bgmFadeTargetVolume;
                _isBgmFading = false;
            }
            else if (direction < 0 && newVolume <= _bgmFadeTargetVolume)
            {
                newVolume = _bgmFadeTargetVolume;
                _isBgmFading = false;
            }

            _bgmSource.volume = newVolume;
        }
        #endregion

        #region Resource Loading
        /// <summary>
        /// 加载音频剪辑
        /// 预留路径: Resources/Audio/{channel}/{id}
        /// </summary>
        private AudioClip LoadAudioClip(string audioId)
        {
            // 实际资源加载 - 后期添加音效资源后启用
            // string path = $"Audio/{audioId}";
            // return Resources.Load<AudioClip>(path);

            // 当前返回null表示资源未找到，框架兼容
            return null;
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// 销毁音频管理器
        /// </summary>
        public void Destroy()
        {
            StopBGM(0);
            StopAllSFX();
            StopVoice();

            var audioRoot = GameObject.Find("AudioManager");
            if (audioRoot != null)
            {
                UnityEngine.Object.Destroy(audioRoot);
            }

            _instance = null;
        }
        #endregion
    }

    #region Coroutine Helper
    /// <summary>
    /// 协程帮助器 (不依赖MonoBehaviour)
    /// </summary>
    internal static class CoroutineHelper
    {
        private static GameObject _hostObject;

        private static GameObject GetHostObject()
        {
            if (_hostObject == null)
            {
                _hostObject = new GameObject("CoroutineHelper");
                _hostObject.AddComponent<CoroutineHost>();
            }
            return _hostObject;
        }

        public static void StartCoroutine(System.Collections.IEnumerator routine)
        {
            var host = GetHostObject().GetComponent<CoroutineHost>();
            host.StartCoroutine(routine);
        }

        private class CoroutineHost : MonoBehaviour
        {
            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
    #endregion

    #region EventBus Integration
    /// <summary>
    /// 音频事件类型
    /// </summary>
    public enum AudioEventType
    {
        PlayBGM,
        StopBGM,
        PlaySFX,
        PlayVoice,
        SetBGMVolume,
        SetSFXVolume,
        SetVoiceVolume,
        Combat_Hit,
        Combat_Crit,
        Combat_Death,
        UI_Click,
        UI_Confirm,
        UI_Cancel,
        UI_Hover,
        Event_Complete,
        Event_Failed
    }

    /// <summary>
    /// 音频事件数据
    /// </summary>
    [Serializable]
    public class AudioEvent
    {
        public AudioEventType type;
        public string audioId;
        public float volumeScale = 1f;
        public bool isPlayer;

        public AudioEvent(AudioEventType type, string audioId = null, float volumeScale = 1f)
        {
            this.type = type;
            this.audioId = audioId;
            this.volumeScale = volumeScale;
        }
    }

    /// <summary>
    /// 音频事件订阅者接口
    /// </summary>
    public interface IAudioEventSubscriber
    {
        void OnAudioEvent(AudioEvent audioEvent);
    }

    /// <summary>
    /// 音频事件处理帮助类
    /// </summary>
    public static class AudioEventHandler
    {
        /// <summary>
        /// 处理战斗打击音效
        /// </summary>
        public static void HandleCombatHit(bool isCrit)
        {
            AudioManager.instance.PlayCombatHit(isCrit);
        }

        /// <summary>
        /// 处理战斗死亡音效
        /// </summary>
        public static void HandleCombatDeath(bool isPlayer)
        {
            AudioManager.instance.PlayCombatDeath(isPlayer);
        }

        /// <summary>
        /// 处理UI点击音效
        /// </summary>
        public static void HandleUIClick()
        {
            AudioManager.instance.PlayUIClick();
        }

        /// <summary>
        /// 处理事件完成音效
        /// </summary>
        public static void HandleEventComplete(bool success)
        {
            AudioManager.instance.PlayEventComplete(success);
        }
    }
    #endregion
}