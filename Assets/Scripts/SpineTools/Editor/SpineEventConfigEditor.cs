#if UNITY_EDITOR
namespace SpineTools.Editor
{
    using SpineTools.Core;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;

    [CustomEditor(typeof(SpineEventConfig))]
    public class SpineEventConfigEditor : Editor
    {
        // EditorPrefs的Key，用于持久化保存用户输入的路径和文件名
        private const string PREF_KEY_SAVE_PATH = "SpineEventConfig_SavePath";
        private const string PREF_KEY_SPINE_ANIMATION_NAMES_FILE_NAME = "SpineEventConfig_SpineAnimationNames_FileName";
        private const string PREF_KEY_EVENT_HASHES_FILE_NAME = "SpineEventConfig_EventHashes_FileName";
        private const string PREF_KEY_NAMESPACE_NAME = "SpineEventConfig_NamespaceName";
        private const string PREF_KEY_SELECT_GENERATE_FILE = "SpineEventConfig_SelectGenerateFileName";

        // 输入框的当前值
        private string _savePath;
        private string _spineAnimationNamesFileName;
        private string _eventHashesFileName;
        private string _namespaceName;

        // 开关
        private bool _selectGenerateFile;

        private void OnEnable()
        {
            // 从EditorPrefs中读取上次保存的配置（如果没有则使用默认值）
            _savePath = EditorPrefs.GetString(PREF_KEY_SAVE_PATH, "Assets/Scripts/Generated");
            _spineAnimationNamesFileName =
                EditorPrefs.GetString(PREF_KEY_SPINE_ANIMATION_NAMES_FILE_NAME, "SpineAnimationNames");
            _eventHashesFileName = EditorPrefs.GetString(PREF_KEY_EVENT_HASHES_FILE_NAME, "SpineEventHashes");
            _namespaceName = EditorPrefs.GetString(PREF_KEY_NAMESPACE_NAME, "SpineTools");
            _selectGenerateFile = EditorPrefs.GetBool(PREF_KEY_SELECT_GENERATE_FILE, false);
        }

        public override void OnInspectorGUI()
        {
            // 绘制默认的Inspector面板
            DrawDefaultInspector();

            // 空行分隔
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // 绘制代码生成区域
            EditorGUILayout.LabelField("🚀 代码生成工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1.绘制路径输入框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("保存路径:", GUILayout.Width(130));
            _savePath = EditorGUILayout.TextField(_savePath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("选择保存路径", _savePath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 将绝对路径转换为Unity相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        _savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        _savePath = selectedPath;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // 2.绘制文件名与命名空间输入框（没有命名空间名则不添加）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动画名文件名称:", GUILayout.Width(130));
            _spineAnimationNamesFileName = EditorGUILayout.TextField(_spineAnimationNamesFileName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("事件哈希值文件名称:", GUILayout.Width(130));
            _eventHashesFileName = EditorGUILayout.TextField(_eventHashesFileName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("命名空间名:", GUILayout.Width(130));
            _namespaceName = EditorGUILayout.TextField(_namespaceName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _selectGenerateFile = EditorGUILayout.Toggle("是否自动选中生成后的代码文件:", _selectGenerateFile);
            EditorGUILayout.EndHorizontal();

            // 3.显示完整路径预览
            EditorGUILayout.Space();
            string spineAnimationFullPath = Path.Combine(_savePath, _spineAnimationNamesFileName + ".cs");
            string eventHashFullPath = Path.Combine(_savePath, _eventHashesFileName + ".cs");
            EditorGUILayout.LabelField("完整路径:", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.SelectableLabel($"{spineAnimationFullPath}\n{eventHashFullPath}", EditorStyles.helpBox,
                GUILayout.Height(40));

            // 4.保存配置到EditorPrefs（只要输入框内容变化就保存）
            if (GUI.changed)
            {
                EditorPrefs.SetString(PREF_KEY_SAVE_PATH, _savePath);
                EditorPrefs.SetString(PREF_KEY_SPINE_ANIMATION_NAMES_FILE_NAME, _spineAnimationNamesFileName);
                EditorPrefs.SetString(PREF_KEY_EVENT_HASHES_FILE_NAME, _eventHashesFileName);
                EditorPrefs.SetString(PREF_KEY_NAMESPACE_NAME, _namespaceName);
                EditorPrefs.SetBool(PREF_KEY_SELECT_GENERATE_FILE, _selectGenerateFile);
            }

            EditorGUILayout.Space();

            // 5.绘制生成按钮
            if (GUILayout.Button("生成动画名代码", GUILayout.Height(30)))
            {
                if (CheckGenerateConfig(_spineAnimationNamesFileName, out var config))
                {
                    GenerateAnimationNames(config);
                }
            }

            if (GUILayout.Button("生成事件名代码", GUILayout.Height(30)))
            {
                if (CheckGenerateConfig(_eventHashesFileName, out var config))
                {
                    GenerateEventHashesCode(config);
                }
            }
        }

        /// <summary>
        /// 检查生成配置
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="eventConfig"></param>
        /// <returns></returns>
        private bool CheckGenerateConfig(string fileName, out SpineEventConfig eventConfig)
        {
            eventConfig = (SpineEventConfig)target;

            // 1.检查是否有配置的事件
            if (eventConfig.AnimationEventList == null || eventConfig.AnimationEventList.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前配置中没有任何动画事件，请先添加！", "确定");
                return false;
            }

            // 2.验证输入
            if (string.IsNullOrEmpty(_savePath))
            {
                EditorUtility.DisplayDialog("错误", "请输入保存路径！", "确定");
                return false;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                EditorUtility.DisplayDialog("错误", "请输入文件名称！", "确定");
                return false;
            }

            // 3.确保目录存在
            if (!Directory.Exists(_savePath))
            {
                bool createDir = EditorUtility.DisplayDialog(
                    "提示",
                    $"路径不存在，是否创建目录？\n{_savePath}",
                    "是",
                    "否"
                );

                if (createDir)
                {
                    Directory.CreateDirectory(_savePath);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 生成动画名脚本
        /// </summary>
        /// <param name="eventConfig"></param>
        private void GenerateAnimationNames(SpineEventConfig eventConfig)
        {
            // 1.收集所有动画名信息
            HashSet<string> animationNames = new HashSet<string>();
            foreach (var animEventList in eventConfig.AnimationEventList)
            {
                animationNames.Add(animEventList.AnimationName);
            }

            // 2.生成代码字符串
            StringBuilder sb = new StringBuilder();
            string className = _spineAnimationNamesFileName;

            sb.AppendLine("// =========================================");
            sb.AppendLine("// 自动生成，请勿手动修改！");
            sb.AppendLine("// 生成来源：" + eventConfig.name);
            sb.AppendLine("// 生成时间：" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("// =========================================");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                sb.AppendLine($"namespace {_namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{GetTab()}using UnityEngine;");
            sb.AppendLine();

            sb.AppendLine($"{GetTab()}public static class {className}");
            sb.AppendLine($"{GetTab()}{{");

            foreach (string animName in animationNames)
            {
                string safeVarName = SanitizeVariableName(animName);
                // 运行时动态计算（只计算一次）
                sb.AppendLine(
                    $"{GetTab()}\tpublic static readonly int {safeVarName} = Animator.StringToHash(\"{animName}\");");
                // 编译时存储哈希值
                //sb.AppendLine($"{GetTab()}\tpublic const string {safeVarName} = \"{animName}\";");
            }

            sb.AppendLine($"{GetTab()}}}");
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                sb.AppendLine("}");
            }

            // 3.写入文件
            string fullSavePath = Path.Combine(_savePath, _spineAnimationNamesFileName + ".cs");
            File.WriteAllText(fullSavePath, sb.ToString(), Encoding.UTF8);


            // 4.刷新Unity资源
            AssetDatabase.Refresh();

            // 5.提示成功并高亮文件
            EditorUtility.DisplayDialog("成功", $"已生成事件哈希代码到：\n{fullSavePath}", "确定");

            if (_selectGenerateFile)
            {
                // 自动选中并高亮生成的文件
                MonoScript generatedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullSavePath);
                if (generatedScript != null)
                {
                    Selection.activeObject = generatedScript;
                    EditorGUIUtility.PingObject(generatedScript);
                }
            }
        }

        /// <summary>
        /// 生成事件哈希值脚本
        /// </summary>
        /// <param name="eventConfig"></param>
        private void GenerateEventHashesCode(SpineEventConfig eventConfig)
        {
            // 1.收集所有事件信息
            Dictionary<string, string> eventDict = new Dictionary<string, string>();
            foreach (var animList in eventConfig.AnimationEventList)
            {
                if (string.IsNullOrEmpty(animList.AnimationName) || animList.Events == null)
                {
                    continue;
                }

                foreach (var evt in animList.Events)
                {
                    if (string.IsNullOrEmpty(evt.EventName))
                    {
                        continue;
                    }

                    string varName = SanitizeVariableName($"{animList.AnimationName}_{evt.EventName}");
                    // 用"动画名_事件名"来计算哈希，防止不同动画名有相同的事件名
                    eventDict[varName] = varName;
                }
            }

            if (eventDict.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前配置中没有任何有效的事件名称！", "确定");
                return;
            }

            // 2.生成代码字符串
            StringBuilder sb = new StringBuilder();
            string className = _eventHashesFileName;

            sb.AppendLine("// =========================================");
            sb.AppendLine("// 自动生成，请勿手动修改！");
            sb.AppendLine("// 生成来源：" + eventConfig.name);
            sb.AppendLine("// 生成时间：" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("// =========================================");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                sb.AppendLine($"namespace {_namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{GetTab()}using UnityEngine;");
            sb.AppendLine();

            sb.AppendLine($"{GetTab()}public static class {className}");
            sb.AppendLine($"{GetTab()}{{");

            foreach (var kvp in eventDict)
            {
                // 注意：这里 kvp.Key 就是 "动画名_事件名"，直接用它来算哈希
                sb.AppendLine(
                    $"{GetTab()}\tpublic static readonly int {kvp.Key} = Animator.StringToHash(\"{kvp.Key}\");");
            }

            sb.AppendLine($"{GetTab()}}}");
            if (!string.IsNullOrEmpty(_namespaceName))
            {
                sb.AppendLine("}");
            }

            // 3.写入文件
            string fullSavePath = Path.Combine(_savePath, _eventHashesFileName + ".cs");
            File.WriteAllText(fullSavePath, sb.ToString(), Encoding.UTF8);

            // 4.刷新Unity资源
            AssetDatabase.Refresh();

            // 5.提示成功并高亮文件
            EditorUtility.DisplayDialog("成功", $"已生成事件哈希代码到：\n{fullSavePath}", "确定");

            if (_selectGenerateFile)
            {
                // 自动选中并高亮生成的文件
                MonoScript generatedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullSavePath);
                if (generatedScript != null)
                {
                    Selection.activeObject = generatedScript;
                    EditorGUIUtility.PingObject(generatedScript);
                }
            }
        }

        /// <summary>
        /// 判断是否添加"\t"
        /// </summary>
        /// <returns></returns>
        private string GetTab()
        {
            return string.IsNullOrEmpty(_namespaceName) ? string.Empty : "\t";
        }

        /// <summary>
        /// 辅助方法：处理变量名
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string SanitizeVariableName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "_";
            }

            StringBuilder sb = new StringBuilder();
            bool lastWasUnderscore = false;

            if (char.IsDigit(input[0]))
            {
                sb.Append('_');
            }

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasUnderscore = false;
                }
                else
                {
                    if (!lastWasUnderscore)
                    {
                        sb.Append('_');
                        lastWasUnderscore = true;
                    }
                }
            }

            return sb.ToString();
        }
    }
}
#endif