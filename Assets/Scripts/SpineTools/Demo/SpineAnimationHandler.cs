using System.Collections.Generic;
using SpineTools.Core;
using UnityEngine;

namespace SpineTools.Demo
{
    public class SpineAnimationHandler : MonoBehaviour
    {
        [SerializeField] private SpineAnimationEventManager _eventManager;

        private Dictionary<int, System.Action<int, string>> _animationHandlers;

        private void Start()
        {
            InitAnimationHandlers();
            _eventManager.OnSpineEvent += OnSpineEvent;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _eventManager.EnqueueAnimation(SpineAnimationNames.run_shield);
                _eventManager.EnqueueAnimation(SpineAnimationNames.shield_attack);
                _eventManager.EnqueueAnimation(SpineAnimationNames.idle_3, true);
                _eventManager.PlayQueue();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _eventManager.EnqueueAnimation(SpineAnimationNames.run_shield);
                _eventManager.EnqueueAnimation(SpineAnimationNames.jump);
                _eventManager.EnqueueAnimation(SpineAnimationNames.idle_3, true);
                _eventManager.PlayQueue();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
            }
        }

        private void OnDestroy()
        {
            _eventManager.OnSpineEvent -= OnSpineEvent;
        }

        /// <summary>
        /// 订阅动画事件
        /// </summary>
        private void InitAnimationHandlers()
        {
            // 在这里面订阅动画事件
            _animationHandlers = new Dictionary<int, System.Action<int, string>>
            {
                { SpineEventHashes.shield_attack_Attack, RunShield },
                { SpineEventHashes.jump_Jump, Jump },
            };
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="animationName">动画名哈希值</param>
        /// <param name="eventNameHash">事件名哈希值</param>
        /// <param name="eventParam">事件参数</param>
        private void OnSpineEvent(string animationName, int eventNameHash, string eventParam)
        {
            if (_animationHandlers.TryGetValue(eventNameHash, out var animationHandler))
            {
                animationHandler?.Invoke(eventNameHash, eventParam);
            }
        }

        #region CustomEvent

        private void RunShield(int eventNameHash, string eventParam)
        {
            Debug.Log("RunShield Attack");
        }

        private void Jump(int eventNameHash, string eventParam)
        {
            Debug.Log("Jump");
        }

        #endregion
    }
}