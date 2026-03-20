#if UNITY_EDITOR
namespace SpineTools.Editor
{
    using SpineTools.Core;
    using UnityEngine;
    using UnityEditor;
    using Spine;
    using Spine.Unity;

    [CustomEditor(typeof(SpineAnimationEventManager))]
    public class SpineAnimationEventManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认的Inspector面板
            DrawDefaultInspector();

            // 获取目标脚本
            SpineAnimationEventManager manager = (SpineAnimationEventManager)target;

            // 空行分隔
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // 绘制标题
            EditorGUILayout.LabelField("🔧 自动配置工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 绘制一键获取按钮
            if (GUILayout.Button("自动获取Spine动画名称", GUILayout.Height(30)))
            {
                AutoFetchSpineAnimations(manager);
            }
        }

        private void AutoFetchSpineAnimations(SpineAnimationEventManager manager)
        {
            // 1.检查是否引用了EventConfig
            SerializedProperty eventConfigProp = serializedObject.FindProperty("_spineEventConfig");
            SpineEventConfig eventConfig = eventConfigProp.objectReferenceValue as SpineEventConfig;
            if (eventConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "请先赋值 SpineEventConfig！", "确定");
                return;
            }

            // 2.尝试获取Spine组件（支持SkeletonAnimation和SkeletonMecanim）
            SkeletonDataAsset skeletonDataAsset = null;

            // 优先检查用户手动赋值的SkeletonAnimation
            if (manager.SkeletonAnimation != null)
            {
                skeletonDataAsset = manager.SkeletonAnimation.SkeletonDataAsset;
            }
            else
            {
                // 尝试自动获取挂载对象上的Spine组件
                SkeletonAnimation sa = manager.GetComponent<SkeletonAnimation>();
                SkeletonMecanim sm = manager.GetComponent<SkeletonMecanim>();

                if (sa != null)
                {
                    skeletonDataAsset = sa.SkeletonDataAsset;
                    manager.SkeletonAnimation = sa;
                    // 立即应用修改，确保引用被保存
                    serializedObject.ApplyModifiedProperties();
                }
                else if (sm != null)
                {
                    skeletonDataAsset = sm.SkeletonDataAsset;
                    EditorUtility.DisplayDialog("提示", "检测到SkeletonMecanim，请手动将Manager里的字段类型改为SkeletonMecanim并赋值。",
                        "确定");
                    return;
                }
            }

            if (skeletonDataAsset == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到Spine组件（SkeletonAnimation或SkeletonMecanim）！", "确定");
                return;
            }

            // 3.获取SkeletonData并读取所有动画名称
            SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null || skeletonData.Animations == null)
            {
                EditorUtility.DisplayDialog("错误", "无法读取Spine动画数据！", "确定");
                return;
            }

            // 4.智能合并动画到Config（保留已有配置，只添加新动画）
            SerializedObject configSO = new SerializedObject(eventConfig);
            SerializedProperty listProp = configSO.FindProperty("AnimationEventList");

            // 记录已存在的动画名（避免重复添加）
            System.Collections.Generic.HashSet<string> existingAnimNames =
                new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty setProp = listProp.GetArrayElementAtIndex(i);
                string animName = setProp.FindPropertyRelative("AnimationName").stringValue;
                existingAnimNames.Add(animName);
            }

            // 添加新动画
            int addedCount = 0;
            foreach (Spine.Animation anim in skeletonData.Animations)
            {
                if (!existingAnimNames.Contains(anim.Name))
                {
                    listProp.InsertArrayElementAtIndex(listProp.arraySize);
                    SerializedProperty newSetProp = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);

                    // 初始化新的SpineAnimationEventSet
                    newSetProp.FindPropertyRelative("TrackIndex").intValue = 0;
                    newSetProp.FindPropertyRelative("Loop").boolValue = false;
                    newSetProp.FindPropertyRelative("TimeScale").floatValue = 1f;
                    newSetProp.FindPropertyRelative("AnimationName").stringValue = anim.Name;
                    newSetProp.FindPropertyRelative("Events").ClearArray();

                    addedCount++;
                }
            }

            // 5.保存修改
            configSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(eventConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6.提示结果
            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("成功", $"已自动添加 {addedCount} 个新动画到配置！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "所有动画已存在于配置中，无需添加。", "确定");
            }
        }
    }
}
#endif