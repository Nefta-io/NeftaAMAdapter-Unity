using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Nefta.Editor
{
    public class NeftaWindow : EditorWindow
    {
        private string _error;
        private string _androidAdapterVersion;
        private string _androidVersion;
        private string _iosAdapterVersion;
        private string _iosVersion;
        
        [MenuItem("Window/Nefta/Inspect", false, 200)]
        public static void ShowWindow()
        {
            GetWindow(typeof(NeftaWindow), false, "Nefta");
        }
        
        [MenuItem("Window/Nefta/Delete Nefta Preferences", false, 300)]
        private static void DeleteNuid()
        {
            PlayerPrefs.DeleteKey("nefta.core.user_id");
            PlayerPrefs.DeleteKey("nefta.sequenceNumber");
            PlayerPrefs.DeleteKey("nefta.sessionNumber");
            PlayerPrefs.DeleteKey("nefta.sessionDuration");
            PlayerPrefs.DeleteKey("nefta.adOpportunityId");
            
            PlayerPrefs.Save();
            Debug.Log("Deleted Nefta Preferences");
        }
        
        public void OnEnable()
        {
            _error = null;
#if UNITY_2021_1_OR_NEWER
            GetAndroidVersions();
#endif
            GetIosVersions();
        }

        private void OnGUI()
        {
            if (_error != null)
            {
                EditorGUILayout.LabelField(_error, EditorStyles.helpBox);
                return;
            }
            
#if UNITY_2021_1_OR_NEWER
            if (_androidAdapterVersion != _iosAdapterVersion)
            {
                DrawVersion("Nefta AdMob Android Custom Adapter version", _androidAdapterVersion);
                DrawVersion("Nefta SDK Android version", _androidVersion);
                EditorGUILayout.Space(5);
                DrawVersion("Nefta AdMob iOS Custom Adapter version", _iosAdapterVersion);
                DrawVersion("Nefta SDK iOS version", _iosVersion);
            }
            else
#endif
            {
                DrawVersion("Nefta AdMob Custom Adapter version", _androidAdapterVersion);
                DrawVersion("Nefta SDK version", _androidVersion);
            }
            EditorGUILayout.Space(5);
        }
        
        [MenuItem("Window/Nefta/Export Nefta Custom Adapter SDK", false, 200)]
        private static void ExportAdSdkPackage()
        {
            var packageName = $"NeftaAM_SDK_{Application.version}.unitypackage";
            var assetPaths = new string[] { "Assets/Nefta" };
            
            try
            {
                AssetDatabase.ExportPackage(assetPaths, packageName, ExportPackageOptions.Recurse);
                Debug.Log($"Finished exporting {packageName}");   
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting {packageName}: {e.Message}");   
            }
        }
        
        private static void DrawVersion(string label, string version)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label); 
            EditorGUILayout.LabelField(version, EditorStyles.boldLabel, GUILayout.Width(60)); 
            EditorGUILayout.EndHorizontal();
        }
        
#if UNITY_2021_1_OR_NEWER
        private void GetAndroidVersions()
        {
            _androidAdapterVersion = GetAarVersion("NeftaAMAdapter-");
            _androidVersion = GetAarVersion("NeftaPlugin-");
        }
        
        private string GetAarVersion(string aarName)
        {
            var guids = AssetDatabase.FindAssets(aarName);
            if (guids.Length == 0)
            {
                _error = $"{aarName} AARs not found in project";
                return null;
            }
            if (guids.Length > 2)
            {
                _error = $"Multiple instances of {aarName} AARs found in project";
                return null;
            }
            var aarPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            using ZipArchive aar = ZipFile.OpenRead(aarPath);
            ZipArchiveEntry manifestEntry = aar.GetEntry("AndroidManifest.xml");
            if (manifestEntry == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return null;
            }
            using Stream manifestStream = manifestEntry.Open();
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestStream);
            var root = manifest.DocumentElement;
            if (root == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return null;
            }
            return root.Attributes["android:versionName"].Value;
        }
#endif
        
        private void GetIosVersions()
        {
            var guids = AssetDatabase.FindAssets("NeftaAdapter");
            string wrapperPath = null;
            foreach (var guid in guids)
            {
                wrapperPath = AssetDatabase.GUIDToAssetPath(guid);
                if (wrapperPath.EndsWith(".m"))
                {
                    break;
                }
            }

            if (wrapperPath == null)
            {
                _error = "NeftaAdapter.m not found in project";
                return;
            }
            using StreamReader reader = new StreamReader(wrapperPath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("GADVersionNumber version") && line.Contains(";"))
                {
                    var start = line.IndexOf('{') + 1;
                    var end = line.LastIndexOf('}');
                    _iosAdapterVersion = line.Substring(start, end - start).Replace(" ", "").Replace(',', '.');
                    break;
                }
            }
            
            var pluginPath = Path.GetDirectoryName(wrapperPath);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(pluginPath + "/NeftaSDK.xcframework/Info.plist");
            var dict = xmlDoc.ChildNodes[2].ChildNodes[0];
            for (var i = 0; i < dict.ChildNodes.Count; i++)
            {
                if (dict.ChildNodes[i].InnerText == "Version")
                {
                    _iosVersion = dict.ChildNodes[i + 1].InnerText;
                    break;
                }
            }
        }
    }
}