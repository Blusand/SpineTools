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
        /// <summary>
        /// 队列项结构
        /// </summary>
        private class AnimationQueueItem
        {
            /// <summary>
            /// 动画名
            /// </summary>
            public int AnimationName;

            /// <summary>
            /// 动画结束回调
            /// </summary>
            public Action OnComplete;
        }

        /// <summary>
        /// 动画队列
        /// </summary>
        private Queue<AnimationQueueItem> _animations = new Queue<AnimationQueueItem>();

        /// <summary>
        /// 是否正在播放队列
        /// </summary>
        private bool _isPlayingQueue = false;

        /// <summary>
        /// 队列是否循环播放
        /// </summary>
        private bool _loopQueue = false;

        /// <summary>
        /// 将动画加入队列（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="animationName">动画名</param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(string animationName, Action onComplete = null)
        {
            EnqueueAnimation(Animator.StringToHash(animationName), onComplete);
        }

        /// <summary>
        /// 将动画加入队列（不立即播放，需调用PlayQueue）
        /// </summary>
        /// <param name="animationHash">动画名哈希值</param>
        /// <param name="onComplete"></param>
        public void EnqueueAnimation(int animationHash, Action onComplete = null)
        {
            _animations.Enqueue(new AnimationQueueItem
            {
                AnimationName = animationHash,
                OnComplete = onComplete
            });
        }

        /// <summary>
        /// 开始播放队列
        /// </summary>
        /// <param name="loop">是否循环播放整个队列的动画</param>
        public void PlayQueue(bool loop = false)
        {
            if (_animations.Count == 0)
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
            _animations.Clear();
            _isPlayingQueue = false;
            _loopQueue = false;
        }

        /// <summary>
        /// 播放队列中的下一个动画
        /// </summary>
        private void PlayNextInQueue()
        {
            if (!_isPlayingQueue || _animations.Count == 0)
            {
                _isPlayingQueue = false;
                return;
            }

            // 取出队列项
            AnimationQueueItem item = _animations.Dequeue();

            // 如果是循环队列，把取出的项重新放回队尾
            if (_loopQueue)
            {
                _animations.Enqueue(item);
            }

            // 播放动画
            SetAnimation(animationHash: item.AnimationName, onComplete: () =>
                {
                    // 先执行该动画单独的回调
                    item.OnComplete?.Invoke();
                    // 再播放下一个
                    PlayNextInQueue();
                }
            );
        }
    }
}