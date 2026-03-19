namespace SpineTools.Core
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Spine动画队列
    /// </summary>
    public partial class SpineAnimationEventManager : MonoBehaviour
    {
        #region QueueItem

        /// <summary>
        /// 动画队列项基类
        /// </summary>
        private abstract class AnimationItemBase
        {
            /// <summary>
            /// 动画结束回调
            /// </summary>
            public Action OnComplete;

            /// <summary>
            /// 播放动画
            /// </summary>
            public abstract void Play(SpineAnimationEventManager manager, Action onPlayComplete);
        }

        /// <summary>
        /// 走配置的动画信息
        /// </summary>
        private class ConfigAnimationItem : AnimationItemBase
        {
            public int AnimationHash;

            public override void Play(SpineAnimationEventManager manager, Action onPlayComplete)
            {
                manager.SetAnimation(AnimationHash, onPlayComplete);
            }
        }

        /// <summary>
        /// 不走配置的动画信息
        /// </summary>
        private class RawAnimationItem : AnimationItemBase
        {
            /// <summary>
            /// 动画轨道
            /// </summary>
            public int TrackIndex;

            /// <summary>
            /// 动画名
            /// </summary>
            public string AnimationName;

            /// <summary>
            /// 是否循环播放
            /// </summary>
            public bool Loop;

            /// <summary>
            /// 播放速度
            /// </summary>
            public float TimeScale;

            public override void Play(SpineAnimationEventManager manager, Action onPlayComplete)
            {
                manager.SetAnimation(TrackIndex, AnimationName, Loop, TimeScale, onPlayComplete);
            }
        }

        #endregion

        /// <summary>
        /// 动画队列
        /// </summary>
        private Queue<AnimationItemBase> _animationQueue = new Queue<AnimationItemBase>();

        /// <summary>
        /// 是否正在播放队列
        /// </summary>
        private bool _isPlayingQueue = false;

        /// <summary>
        /// 队列是否循环播放
        /// </summary>
        private bool _loopQueue = false;

        #region 动画入队

        /// <summary>
        /// 将动画加入队列（走配置）（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="animationName">动画名</param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(string animationName, Action onComplete = null)
        {
            EnqueueAnimation(Animator.StringToHash(animationName), onComplete);
        }

        /// <summary>
        /// 将动画加入队列（走配置）（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="animationHash">动画名哈希值</param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(int animationHash, Action onComplete = null)
        {
            _animationQueue.Enqueue(new ConfigAnimationItem
            {
                AnimationHash = animationHash,
                OnComplete = onComplete
            });
        }

        /// <summary>
        /// 将动画加入队列，默认走轨道0（不走配置）（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(string animationName, bool loop = false, float timeScale = 1f,
            Action onComplete = null)
        {
            EnqueueAnimation(0, animationName, loop, timeScale, onComplete);
        }

        /// <summary>
        /// 将动画加入队列（不走配置）（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="trackIndex"></param>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(int trackIndex, string animationName, bool loop = false, float timeScale = 1f,
            Action onComplete = null)
        {
            _animationQueue.Enqueue(new RawAnimationItem
            {
                TrackIndex = trackIndex,
                AnimationName = animationName,
                Loop = loop,
                TimeScale = timeScale,
                OnComplete = onComplete
            });
        }

        #endregion

        /// <summary>
        /// 开始播放队列
        /// </summary>
        /// <param name="loop">是否循环播放整个队列的动画</param>
        public void PlayQueue(bool loop = false)
        {
            if (_animationQueue.Count == 0)
            {
                Debug.LogWarning("动画队列为空！", this);
                return;
            }

            _loopQueue = loop;
            _isPlayingQueue = true;
            PlayNextInQueue();
        }

        /// <summary>
        /// 停止播放队列（当前动画会继续播完）
        /// </summary>
        public void StopQueue()
        {
            _isPlayingQueue = false;
            _loopQueue = false;
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void ClearQueue()
        {
            _animationQueue.Clear();
            _isPlayingQueue = false;
            _loopQueue = false;
        }

        /// <summary>
        /// 获取当前队列长度
        /// </summary>
        /// <returns></returns>
        public int GetQueueCount() => _animationQueue.Count;

        /// <summary>
        /// 是否正在播放队列
        /// </summary>
        public bool IsPlayingQueue() => _isPlayingQueue;

        #region 插入动画

        /// <summary>
        /// 插入动画到队头（走配置）
        /// </summary>
        /// <param name="animationName">动画名</param>
        /// <param name="onComplete"></param>
        public void InsertAnimationToFront(string animationName, Action onComplete = null)
        {
            InsertAnimationToFront(Animator.StringToHash(animationName), onComplete);
        }

        /// <summary>
        /// 插入动画到队头（走配置）
        /// </summary>
        /// <param name="animationHash">动画名哈希值</param>
        /// <param name="onComplete"></param>
        public void InsertAnimationToFront(int animationHash, Action onComplete = null)
        {
            InsertAnimationToFront(new ConfigAnimationItem
            {
                AnimationHash = animationHash,
                OnComplete = onComplete
            });
        }

        /// <summary>
        /// 插入动画到队头，默认走轨道0（不走配置）
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="onComplete"></param>
        public void InsertAnimationToFront(string animationName, bool loop = false,
            float timeScale = 1f, Action onComplete = null)
        {
            InsertAnimationToFront(0, animationName, loop, timeScale, onComplete);
        }

        /// <summary>
        /// 插入动画到队头（不走配置）
        /// </summary>
        /// <param name="trackIndex"></param>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="onComplete"></param>
        public void InsertAnimationToFront(int trackIndex, string animationName, bool loop = false,
            float timeScale = 1f, Action onComplete = null)
        {
            InsertAnimationToFront(new RawAnimationItem
            {
                TrackIndex = trackIndex,
                AnimationName = animationName,
                Loop = loop,
                TimeScale = timeScale,
                OnComplete = onComplete
            });
        }

        /// <summary>
        /// 通用插入方法
        /// </summary>
        /// <param name="item"></param>
        private void InsertAnimationToFront(AnimationItemBase item)
        {
            var tempList = new List<AnimationItemBase>(1 + _animationQueue.Count) { item };
            tempList.AddRange(_animationQueue);
            _animationQueue = new Queue<AnimationItemBase>(tempList);
        }

        #endregion

        /// <summary>
        /// 播放队列中的下一个动画
        /// </summary>
        private void PlayNextInQueue()
        {
            if (!_isPlayingQueue || _animationQueue.Count == 0)
            {
                _isPlayingQueue = false;
                return;
            }

            // 取出队列项
            var item = _animationQueue.Dequeue();

            // 如果是循环队列，把取出的项重新放回队尾
            if (_loopQueue)
            {
                _animationQueue.Enqueue(item);
            }

            // 播放动画
            item.Play(this, () =>
            {
                // 先执行该动画单独的回调
                item.OnComplete?.Invoke();
                // 再播放下一个
                PlayNextInQueue();
            });
        }
    }
}