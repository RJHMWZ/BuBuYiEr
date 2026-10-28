using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;

namespace ClawbearGames
{
    [CustomEditor(typeof(AdmobController))]
    public class AdmobControllerCustomEditor : Editor
    {
        private bool isAdmobNamespaceExists = false;
        private bool isAddedAdmobSymbols = false;
        private void OnEnable()
        {
#if UNITY_ANDROID || UNITY_IOS
            isAdmobNamespaceExists = ScriptingSymbolsHandler.NamespaceExists(NamespaceData.GoogleMobileAdsNameSpace);
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            string symbolStr = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            List<string> currentSymbols = new List<string>(symbolStr.Split(';'));
            isAddedAdmobSymbols = currentSymbols.Contains(ScriptingSymbolsData.ADMOB);
#endif
        }

        public override void OnInspectorGUI()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!isAdmobNamespaceExists)
            {
                EditorGUILayout.HelpBox("Google Mobile Ads plugin is not imported. Please click the button bellow to download the plugin.", MessageType.Warning);
                if (GUILayout.Button("Download Plugin", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                {
                    Application.OpenURL("https://github.com/googleads/googleads-mobile-unity/releases");
                }
            }
            else
            {
                if (!isAddedAdmobSymbols)
                {
                    isAddedAdmobSymbols = true;
                    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                    NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
                    string symbolStr = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                    List<string> currentSymbols = new List<string>(symbolStr.Split(';'));
                    if (!currentSymbols.Contains(ScriptingSymbolsData.ADMOB))
                    {
                        List<string> sbs = new List<string>();
                        sbs.Add(ScriptingSymbolsData.ADMOB);
                        ScriptingSymbolsHandler.AddDefinedScriptingSymbol(sbs.ToArray());
                    }
                }
            }
            base.OnInspectorGUI();
#endif
        }
    }
}

