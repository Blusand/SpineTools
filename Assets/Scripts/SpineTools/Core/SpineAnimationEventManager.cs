namespace SpineTools.Core
{
    using System;
    using Spine;
    using Spine.Unity;
    using UnityEngine;

    public partial class SpineAnimationEventManager : MonoBehaviour
    {
        [Tooltip("Spine动画组件")] public SkeletonAnimation SkeletonAnimation;

        [Tooltip("事件配置文件"), SerializeField] private SpineEventConfig _spineEventConfig;

        [Tooltip("是否能连续播放相同动画"), SerializeField]
        private bool _playSameAnimation = true;

        /// <summary>
        /// 触发事件（动画名，件名哈希值，事件参数）
        /// </summary>
        public event Action<string, int, string> OnSpineEvent;

        /// <summary>
        /// 动画开始事件
        /// </summary>
        public event Action<TrackEntry> AnimationStart;

        /// <summary>
        /// 动画结束事件
        /// </summary>
        public event Action<TrackEntry> AnimationComplete;

        /// <summary>
        /// 当前动画事件
        /// </summary>
        private SpineAnimationEventList _currentEventList;

        /// <summary>
        /// 上一个动画名称哈希值
        /// </summary>
        private int _lastAnimationHash;

        private bool _initialized;

        private void Awake()
        {
            // 初始化
            _spineEventConfig.Initialize();
            _initialized = true;
        }

        private void Start()
        {
            if (SkeletonAnimation != null)
            {
                SkeletonAnimation.AnimationState.Start += OnAnimationStart;
                SkeletonAnimation.AnimationState.Complete += OnAnimationComplete;
            }
        }

        private void Update()
        {
            if (!_initialized || _currentEventList == null)
            {
                return;
            }

            TrackEntry trackEntry = SkeletonAnimation.AnimationState.GetCurrent(_currentEventList.TrackIndex);
            if (trackEntry == null || Animator.StringToHash(trackEntry.Animation.Name) != _lastAnimationHash)
            {
                return;
            }

            // 当前动画进度
            float progress = Mathf.Clamp01(trackEntry.TrackTime / trackEntry.Animation.Duration);

            // 处理同一时间点触发多个事件的情况
            while (_currentEventList.currentEventIndex < _currentEventList.Events.Count)
            {
                var currentEvent = _currentEventList.Events[_currentEventList.currentEventIndex];

                // 还未到触时间，退出循环
                if (progress < currentEvent.TriggerProgress)
                {
                    break;
                }

                // 检查是否要触发(每次循环触发 || (触发一次 && (动画还未播完一次))
                bool needTrigger = currentEvent.TriggerMode == EventTriggerMode.EveryLoop ||
                                   (currentEvent.TriggerMode == EventTriggerMode.Once
                                    && !_currentEventList.hasCompletedOnce);

                // 触发事件
                if (needTrigger)
                {
                    OnSpineEvent?.Invoke(_currentEventList.AnimationName, currentEvent.eventHash,
                        currentEvent.EventParam);
                }

                // 索引+1，检查下一个事件
                ++_currentEventList.currentEventIndex;
            }
        }

        private void OnDestroy()
        {
            if (SkeletonAnimation != null)
            {
                SkeletonAnimation.AnimationState.Start -= OnAnimationStart;
                SkeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
            }
        }

        private void OnAnimationStart(TrackEntry trackEntry)
        {
            int animationHash = Animator.StringToHash(trackEntry.Animation.Name);
            // 不能连续播放相同动画，退出
            if (!_playSameAnimation && _lastAnimationHash == animationHash)
            {
                return;
            }

            _currentEventList = _spineEventConfig.GetEventList(animationHash);
            if (_currentEventList != null)
            {
                // 重置状态
                _lastAnimationHash = animationHash;
                trackEntry.TimeScale = _currentEventList.TimeScale;

                // 重置索引
                _currentEventList.currentEventIndex = 0;
                _currentEventList.hasCompletedOnce = false;
            }

            AnimationStart?.Invoke(trackEntry);
        }

        private void OnAnimationComplete(TrackEntry trackEntry)
        {
            if (_currentEventList == null)
            {
                return;
            }

            _currentEventList.currentEventIndex = 0;
            // 动画已经播完一次
            _currentEventList.hasCompletedOnce = true;

            // 只处理EveryLoop模式的事件
            while (_currentEventList.currentEventIndex < _currentEventList.Events.Count)
            {
                var evt = _currentEventList.Events[_currentEventList.currentEventIndex];
                // 遇到第一个EveryLoop事件，停止跳过
                if (evt.TriggerMode == EventTriggerMode.EveryLoop)
                {
                    break;
                }

                ++_currentEventList.currentEventIndex;
            }

            AnimationComplete?.Invoke(trackEntry);
        }

        /// <summary>
        /// 播放Spine动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="onComplete">动画结束回调（仅非循环动画有效）</param>
        public void SetAnimation(string animationName, Action onComplete = null)
        {
            SetAnimation(Animator.StringToHash(animationName), onComplete);
        }

        /// <summary>
        /// 播放Spine动画
        /// </summary>
        /// <param name="animationHash">动画名称哈希值</param>
        /// <param name="onComplete">动画结束回调（仅非循环动画有效）</param>
        public void SetAnimation(int animationHash, Action onComplete = null)
        {
            _currentEventList = _spineEventConfig.GetEventList(animationHash);

            if (_currentEventList == null)
            {
                Debug.LogWarning($"未找到动画哈希 {animationHash} 的配置", this);
                return;
            }

            SetAnimation(_currentEventList.TrackIndex, _currentEventList.AnimationName, _currentEventList.Loop,
                _currentEventList.TimeScale, onComplete);
        }

        /// <summary>
        /// 播放Spine动画
        /// </summary>
        /// <param name="trackIndex">动画播放轨道</param>
        /// <param name="animationName">动画名</param>
        /// <param name="loop">是否循环播放</param>
        /// <param name="timeScale">播放速度</param>
        /// <param name="onComplete">动画结束回调（仅非循环动画有效）</param>
        public void SetAnimation(int trackIndex, string animationName, bool loop = false, float timeScale = 1f,
            Action onComplete = null)
        {
            // 1.播放动画（可根据需求修改轨道索引）
            TrackEntry trackEntry = SkeletonAnimation.AnimationState.SetAnimation(trackIndex, animationName, loop);

            // 2.设置播放速度
            trackEntry.TimeScale = timeScale;

            // 3.如果有结束回调且不是循环动画，监听Complete事件
            if (onComplete != null && !loop)
            {
                // 定义本地回调函数（用于自动取消订阅）
                void OnAnimationComplete(TrackEntry entry)
                {
                    // 确保是当前这个动画轨道触发的回调（防止被其他动画覆盖）
                    if (entry == trackEntry)
                    {
                        // 触发回调
                        onComplete?.Invoke();
                        // 取消订阅，防止内存泄漏
                        trackEntry.Complete -= OnAnimationComplete;
                    }
                }

                // 订阅事件
                trackEntry.Complete += OnAnimationComplete;
            }
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void PauseAnimation()
        {
            SetAnimationTimeScale(0f);
        }

        /// <summary>
        /// 暂停动画（不依赖配置）
        /// </summary>
        public void PauseAnimation(int trackIndex)
        {
            SetAnimationTimeScale(trackIndex, 0f);
        }

        /// <summary>
        /// 继续播放动画
        /// </summary>
        public void ResumeAnimation()
        {
            SetAnimationTimeScale(1f);
        }

        /// <summary>
        /// 继续播放动画（不依赖配置）
        /// </summary>
        public void ResumeAnimation(int trackIndex)
        {
            SetAnimationTimeScale(trackIndex, 1f);
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        /// <param name="fadeOutDuration"></param>
        public void StopAnimation(float fadeOutDuration = 0f)
        {
            if (_currentEventList == null)
            {
                return;
            }

            SkeletonAnimation.AnimationState.SetEmptyAnimation(_currentEventList.TrackIndex, fadeOutDuration);
        }

        /// <summary>
        /// 停止动画（不依赖配置）
        /// </summary>
        /// <param name="trackIndex"></param>
        /// <param name="fadeOutDuration"></param>
        public void StopAnimation(int trackIndex, float fadeOutDuration = 0f)
        {
            if (_currentEventList == null)
            {
                return;
            }

            SkeletonAnimation.AnimationState.SetEmptyAnimation(trackIndex, fadeOutDuration);
        }

        /// <summary>
        /// 设置动画TimeScale
        /// </summary>
        /// <param name="timeScale"></param>
        public void SetAnimationTimeScale(float timeScale)
        {
            if (_currentEventList == null)
            {
                return;
            }

            var trackEntry = SkeletonAnimation.AnimationState.GetCurrent(_currentEventList.TrackIndex);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = timeScale;
            }
        }

        /// <summary>
        /// 设置动画TimeScale（不依赖配置）
        /// </summary>
        /// <param name="trackIndex"></param>
        /// <param name="timeScale"></param>
        public void SetAnimationTimeScale(int trackIndex, float timeScale)
        {
            var trackEntry = SkeletonAnimation.AnimationState.GetCurrent(trackIndex);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = timeScale;
            }
        }
    }
}