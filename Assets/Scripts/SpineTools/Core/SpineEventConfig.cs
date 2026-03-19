namespace SpineTools.Core
{
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public enum EventTriggerMode
    {
        /// <summary>
        /// 只触发一次
        /// </summary>
        Once,

        /// <summary>
        /// 每次循环都触发
        /// </summary>
        EveryLoop,
    }

    /// <summary>
    /// 单个动画事件
    /// </summary>
    [System.Serializable]
    public class SpineAnimationEvent
    {
        [Tooltip("事件名称")] public string EventName;

        [Tooltip("触发时间比例（0~1）"), Range(0f, 1f)]
        public float TriggerProgress;

        [Tooltip("触发模式")] public EventTriggerMode TriggerMode;

        /// <summary>
        /// 事件参数
        /// </summary>
        public string EventParam;

        /// <summary>
        /// 事件名哈希值（动画名_事件名）
        /// </summary>
        [SerializeField, HideInInspector]
        internal int eventHash;

        // 【编辑器调试用】显示哈希值（只读）
#if UNITY_EDITOR
        [SerializeField, ReadOnly, Tooltip("事件哈希值（自动生成）")]
        internal string eventHashDebug;
#endif
    }

    /// <summary>
    /// 单个动画的事件配置
    /// </summary>
    [System.Serializable]
    public class SpineAnimationEventList
    {
        [Tooltip("Spine动画名称（需要与Spine资源的动画名称一致）")]
        public string AnimationName;

        [Tooltip("监听的动画轨道（0为主轨道）")] public int TrackIndex = 0;

        [Tooltip("是否循环播放")] public bool Loop = false;

        [Tooltip("动画播放速度")] public float TimeScale = 1;

        [Tooltip("该动画对应的事件列表")] public List<SpineAnimationEvent> Events = new List<SpineAnimationEvent>();

        /// <summary>
        /// 动画名哈希值
        /// </summary>
        [SerializeField, HideInInspector]
        internal int animationHash;

#if UNITY_EDITOR
        [SerializeField, ReadOnly, Tooltip("动画哈希值（自动生成）")]
        internal string animationHashDebug;
#endif

        /// <summary>
        /// 下一个待触发事件索引
        /// </summary>
        internal int currentEventIndex;

        /// <summary>
        /// 动画是否已经播放过一次
        /// </summary>
        internal bool hasCompletedOnce;
    }

    /// <summary>
    /// 动画事件全局配置文件
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpineEvent", menuName = "SpineTools/Animation Event Config")]
    public class SpineEventConfig : ScriptableObject
    {
        [Tooltip("所有动画事件配置列表")]
        public List<SpineAnimationEventList> AnimationEventList = new List<SpineAnimationEventList>();

        private Dictionary<int, SpineAnimationEventList> _animationEventDic;

        private bool _initialized;

        /// <summary>
        /// 初始化，第一次使用时调用
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _animationEventDic = new Dictionary<int, SpineAnimationEventList>(AnimationEventList.Count);

            foreach (var eventList in AnimationEventList)
            {
                if (string.IsNullOrEmpty(eventList.AnimationName))
                {
                    Debug.LogWarning("配置中存在空的动画名，已跳过", this);
                    continue;
                }

                // 计算动画名哈希值
                eventList.animationHash = Animator.StringToHash(eventList.AnimationName);
                _animationEventDic[eventList.animationHash] = eventList;

                var events = eventList.Events;
                // 按照事件触发时间比例排序
                events.Sort((a, b) => a.TriggerProgress.CompareTo(b.TriggerProgress));

                // 初始化索引和标记
                eventList.currentEventIndex = 0;
                eventList.hasCompletedOnce = false;

                // 计算每个事件名哈希值（用"动画名_事件名"来计算哈希，防止不同动画名有相同的事件名）
                foreach (var evt in events)
                {
                    if (string.IsNullOrEmpty(evt.EventName))
                    {
                        continue;
                    }

                    evt.eventHash = Animator.StringToHash($"{eventList.AnimationName}_{evt.EventName}");
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// 获取单个动画的事件配置
        /// </summary>
        /// <param name="animationHash">动画名哈希值</param>
        /// <returns></returns>
        internal SpineAnimationEventList GetEventList(int animationHash)
        {
            if (!_initialized)
            {
                Debug.LogError($"SpineEventConfig 未初始化！请先调用 {nameof(Initialize)}()", this);
                return null;
            }

            if (_animationEventDic.TryGetValue(animationHash, out var spineEventList))
            {
                return spineEventList;
            }

            Debug.LogWarning($"未找到动画哈希 {animationHash} 的配置！", this);
            return null;
        }

        /// <summary>
        /// 获取单个动画的事件配置（建议存储哈希值后通过哈希值来获取）
        /// </summary>
        /// <param name="animationName">动画名</param>
        /// <returns></returns>
        internal SpineAnimationEventList GetEventList(string animationName)
        {
            return GetEventList(Animator.StringToHash(animationName));
        }

        #region 编辑器功能

        // ==========================================
        // 编辑器工具（预计算、排序、验证）
        // ==========================================

#if UNITY_EDITOR
        /// <summary>
        /// 刷新所有预计算数据（排序、算哈希）
        /// </summary>
        [ContextMenu("刷新配置数据")]
        private void RefreshConfigData()
        {
            Undo.RecordObject(this, "Refresh SpineEventConfig");

            foreach (var eventList in AnimationEventList)
            {
                if (string.IsNullOrEmpty(eventList.AnimationName))
                {
                    continue;
                }

                // 1.计算动画哈希
                eventList.animationHash = Animator.StringToHash(eventList.AnimationName);
                eventList.animationHashDebug = eventList.animationHash.ToString();

                // 2.按触发时间排序事件
                eventList.Events.Sort((a, b) => a.TriggerProgress.CompareTo(b.TriggerProgress));

                // 3.预计算每个事件的哈希
                foreach (var evt in eventList.Events)
                {
                    if (string.IsNullOrEmpty(evt.EventName))
                    {
                        continue;
                    }

                    string combinedName = $"{eventList.AnimationName}_{evt.EventName}";
                    evt.eventHash = Animator.StringToHash(combinedName);
                    evt.eventHashDebug = evt.eventHash.ToString();
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("SpineEventConfig 配置数据刷新成功！", this);
        }

        /// <summary>
        /// 验证配置是否合法
        /// </summary>
        [ContextMenu("验证配置")]
        private void ValidateConfig()
        {
            bool hasError = false;
            HashSet<string> animNames = new HashSet<string>();

            foreach (var eventList in AnimationEventList)
            {
                // 检查1：动画名是否为空
                if (string.IsNullOrEmpty(eventList.AnimationName))
                {
                    Debug.LogError("配置中存在空的动画名！", this);
                    hasError = true;
                    continue;
                }

                // 检查2：是否有重复的动画名
                if (animNames.Contains(eventList.AnimationName))
                {
                    Debug.LogError($"动画名重复：{eventList.AnimationName}", this);
                    hasError = true;
                }

                animNames.Add(eventList.AnimationName);

                // 检查3：事件是否有重复的触发时间
                HashSet<float> progressSet = new HashSet<float>();
                foreach (var evt in eventList.Events)
                {
                    if (progressSet.Contains(evt.TriggerProgress))
                    {
                        Debug.LogWarning($"动画 {eventList.AnimationName} 中存在重复的触发时间：{evt.TriggerProgress}", this);
                    }

                    progressSet.Add(evt.TriggerProgress);
                }
            }

            if (!hasError)
            {
                Debug.Log("SpineEventConfig 配置验证通过！", this);
            }
        }

        // 在Inspector中修改数据后自动刷新
        private void OnValidate()
        {
            // 注意：OnValidate 中不要做太耗时的操作，这里只做轻量级提示
            // 真正的刷新建议手动点击「刷新配置数据」
            foreach (var eventList in AnimationEventList)
            {
                if (!string.IsNullOrEmpty(eventList.AnimationName) && eventList.animationHash == 0)
                {
                    // 只在第一次发现时提示，避免刷屏
                    Debug.LogWarning("数据有误，请刷新配置！", this);
                    return;
                }
            }
        }
#endif

        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// 辅助：只读属性 Drawer（用于在 Inspector 中显示只读字段）
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
#endif
}