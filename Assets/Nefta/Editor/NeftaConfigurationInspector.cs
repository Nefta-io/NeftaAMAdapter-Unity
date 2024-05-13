using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEditor.iOS.Xcode;

namespace Nefta.Editor
{
    [CustomEditor(typeof(NeftaConfiguration))]
    public class NeftaConfigurationInspector : UnityEditor.Editor
    {
        private NeftaConfiguration _configuration;
        private bool _isLoggingEnabled;
        
        private string _error;
        private string _androidAdapterVersion;
        private string _androidVersion;
        private string _iosAdapterVersion;
        private string _iosVersion;
        
        private static PluginImporter GetImporter(bool debug)
        {
            var guid = AssetDatabase.FindAssets(debug ? "NeftaAMAdapter-debug" : "NeftaAMAdapter-release")[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return (PluginImporter) AssetImporter.GetAtPath(path);
        }
        
        public void OnEnable()
        {
            _configuration = (NeftaConfiguration)target;
            
            var importer = GetImporter(true);
            var isDebugPluginEnabled = importer.GetCompatibleWithPlatform(BuildTarget.Android);
            if (isDebugPluginEnabled != _configuration._isLoggingEnabled)
            {
                _configuration._isLoggingEnabled = isDebugPluginEnabled;
                EditorUtility.SetDirty(_configuration);
                AssetDatabase.SaveAssetIfDirty(_configuration);
            }
            
            _error = null;
#if UNITY_2021
            GetAndroidVersions();
#endif
            GetIosVersions();
        }

        public void OnDisable()
        {
            _configuration = null;
        }
        
        public override void OnInspectorGUI()
        {
            if (_error != null)
            {
                EditorGUILayout.LabelField(_error, EditorStyles.helpBox);
                return;
            }
            
#if UNITY_2021
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
            
            base.OnInspectorGUI();
            if (_isLoggingEnabled != _configuration._isLoggingEnabled)
            {
                _isLoggingEnabled = _configuration._isLoggingEnabled;
                
                var importer = GetImporter(true);
                importer.SetCompatibleWithPlatform(BuildTarget.Android, _isLoggingEnabled);
                importer.SaveAndReimport();
                
                importer = GetImporter(false);
                importer.SetCompatibleWithPlatform(BuildTarget.Android, !_isLoggingEnabled);
                importer.SaveAndReimport();
            }
        }
        private static NeftaConfiguration GetNeftaConfiguration()
        {
            NeftaConfiguration configuration = null;
            
            string[] guids = AssetDatabase.FindAssets("t:NeftaConfiguration");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                configuration = AssetDatabase.LoadAssetAtPath<NeftaConfiguration>(path);
            }

            return configuration;
        }
        
        [MenuItem("Window/Nefta/Select Nefta Configuration", false, int.MaxValue)]
        private static void SelectNeftaConfiguration()
        {
            var configuration = GetNeftaConfiguration();
            if (configuration == null)
            {
                configuration = CreateInstance<NeftaConfiguration>();
                
                var directory = "Assets/Resources";
                var assetPath = $"{directory}/{NeftaConfiguration.FileName}.asset";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                AssetDatabase.CreateAsset(configuration, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Selection.objects = new UnityEngine.Object[] { configuration };
        }
        
        [MenuItem("Window/Nefta/Export Nefta Custom Adapter SDK", false, int.MaxValue)]
        private static void ExportAdSdkPackage()
        {
            var packageName = $"NeftaAM_SDK_{Application.version}.unitypackage";
            var assetPaths = new string[] { "Assets/Nefta" };
            
            var importer = GetImporter(true);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, true);
            importer.SaveAndReimport();
            
            importer = GetImporter(false);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, false);
            importer.SaveAndReimport();
            
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
    
#if UNITY_2021
        private void GetAndroidVersions()
        {
            var guids = AssetDatabase.FindAssets("NeftaAMAdapter");
            if (guids.Length == 0)
            {
                _error = "NeftaAMAdapter AARs not found in project";
                return;
            }
            if (guids.Length > 2)
            {
                _error = "Multiple instances of NeftaAMAdapter AARs found in project";
                return;
            }
            var aarPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            using ZipArchive aar = ZipFile.OpenRead(aarPath);
            ZipArchiveEntry manifestEntry = aar.GetEntry("AndroidManifest.xml");
            if (manifestEntry == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return;
            }
            using Stream manifestStream = manifestEntry.Open();
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestStream);
            var root = manifest.DocumentElement;
            if (root == null)
            {
                _error = "Nefta SDK AAR seems to be corrupted";
                return;
            }
            _androidAdapterVersion = root.Attributes["android:versionName"].Value;
            var metaNodes = root.SelectNodes("/manifest/application/meta-data");
            foreach (XmlNode metaNode in metaNodes)
            {
                var name = metaNode.Attributes["android:name"];
                if (name.Value == "NeftaSDKVersion")
                {
                    _androidVersion = metaNode.Attributes["android:value"].Value;
                    break;
                }
            }
        }
#endif
        
        private void GetIosVersions()
        {
            var guids = AssetDatabase.FindAssets("NeftaAdapter");
            if (guids.Length == 0)
            {
                _error = "NeftaAdapter not found in project";
                return;
            }
            if (guids.Length > 2)
            {
                _error = "Multiple instances of NeftaAdapter found in project";
                return;
            }
            var wrapperPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (wrapperPath.EndsWith(".h"))
            {
                wrapperPath = AssetDatabase.GUIDToAssetPath(guids[1]);
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
            
            guids = AssetDatabase.FindAssets("NeftaSDK.xcframework");
            if (guids.Length == 0)
            {
                _error = "NeftaSDK.xcframework not found in project";
                return;
            }
            if (guids.Length > 1)
            {
                _error = "Multiple instances of NeftaSDK.xcframework found in project";
                return;
            }
            var frameworkPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var plist = new PlistDocument();
            plist.ReadFromFile(frameworkPath + "/Info.plist");
            _iosVersion = plist.root["Version"].AsString();
        }
    }
}