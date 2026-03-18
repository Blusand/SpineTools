namespace SpineTools.Core
{
    using System.Collections.Generic;
    using UnityEngine;

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
        /// 事件名哈希值
        /// </summary>
        internal int eventHash;
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

        [Tooltip("动画播放速度")] public float TimeScale = 1;

        [Tooltip("该动画对应的事件列表")] public List<SpineAnimationEvent> Events = new List<SpineAnimationEvent>();

        /// <summary>
        /// 动画名哈希值
        /// </summary>
        internal int animationHash;

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

        /// <summary>
        /// 初始化，第一次使用时调用
        /// </summary>
        public void Initialize()
        {
            if (_animationEventDic != null)
            {
                return;
            }

            _animationEventDic = new Dictionary<int, SpineAnimationEventList>();

            foreach (var eventList in AnimationEventList)
            {
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
                    evt.eventHash = Animator.StringToHash($"{eventList.AnimationName}_{evt.EventName}");
                }
            }
        }

        /// <summary>
        /// 获取单个动画的事件配置
        /// </summary>
        /// <param name="animationHash">动画名哈希值</param>
        /// <returns></returns>
        internal SpineAnimationEventList GetEventList(int animationHash)
        {
            _animationEventDic.TryGetValue(animationHash, out var spineEventList);
            return spineEventList;
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
    }
}