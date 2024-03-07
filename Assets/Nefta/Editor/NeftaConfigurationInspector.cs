using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nefta.Editor
{
    [CustomEditor(typeof(NeftaConfiguration))]
    public class NeftaConfigurationInspector : UnityEditor.Editor
    {
        private NeftaConfiguration _configuration;
        private bool _isLoggingEnabled;
        
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
        }

        public void OnDisable()
        {
            _configuration = null;
        }
        
        public override void OnInspectorGUI()
        {
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
        
    }
}