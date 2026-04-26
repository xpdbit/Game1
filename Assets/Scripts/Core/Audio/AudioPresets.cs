#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Game1.Core.Audio
{
    /// <summary>
    /// 预设音效ID
    /// 实际音效资源路径: Resources/Audio/SFX/{category}/{name}
    /// </summary>
    public static class SFXPreset
    {
        // === UI音效 ===
        public const string UI_Click = "SFX/UI/Click";
        public const string UI_Hover = "SFX/UI/Hover";
        public const string UI_Confirm = "SFX/UI/Confirm";
        public const string UI_Cancel = "SFX/UI/Cancel";
        public const string UI_Open = "SFX/UI/Open";
        public const string UI_Close = "SFX/UI/Close";

        // === 战斗音效 ===
        public const string Combat_Hit = "SFX/Combat/Hit";
        public const string Combat_Crit = "SFX/Combat/Crit";
        public const string Combat_Death = "SFX/Combat/Death";
        public const string Combat_Victory = "SFX/Combat/Victory";
        public const string Combat_Defeat = "SFX/Combat/Defeat";
        public const string Combat_Skill = "SFX/Combat/Skill";

        // === 事件音效 ===
        public const string Event_Complete = "SFX/Event/Complete";
        public const string Event_Failed = "SFX/Event/Failed";
        public const string Event_Discovery = "SFX/Event/Discovery";
        public const string Event_Treasure = "SFX/Event/Treasure";

        // === 旅行音效 ===
        public const string Travel_Step = "SFX/Travel/Step";
        public const string Travel_Arrive = "SFX/Travel/Arrive";
        public const string Travel_Blocked = "SFX/Travel/Blocked";

        // === 物品音效 ===
        public const string Item_Get = "SFX/Item/Get";
        public const string Item_Equip = "SFX/Item/Equip";
        public const string Item_Use = "SFX/Item/Use";
    }

    /// <summary>
    /// 预设BGM ID
    /// 实际BGM资源路径: Resources/Audio/BGM/{name}
    /// </summary>
    public static class BGMPreset
    {
        public const string MainMenu = "BGM/MainMenu";
        public const string Overworld = "BGM/Overworld";
        public const string Combat = "BGM/Combat";
        public const string Victory = "BGM/Victory";
        public const string Defeat = "BGM/Defeat";
        public const string EventStory = "BGM/EventStory";
        public const string Shop = "BGM/Shop";
    }

    /// <summary>
    /// 预设语音ID
    /// 实际语音资源路径: Resources/Audio/Voice/{category}/{name}
    /// </summary>
    public static class VoicePreset
    {
        public const string Combat_Attack = "Voice/Combat/Attack";
        public const string Combat_Skill = "Voice/Combat/Skill";
        public const string Combat_Crit = "Voice/Combat/Crit";
        public const string Combat_Death = "Voice/Combat/Death";
        public const string Event_Start = "Voice/Event/Start";
        public const string Event_Choice = "Voice/Event/Choice";
    }
}