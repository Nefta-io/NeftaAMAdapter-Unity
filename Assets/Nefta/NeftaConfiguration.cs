using UnityEngine;

namespace Nefta
{
    public class NeftaConfiguration : ScriptableObject
    {
        public const string FileName = "NeftaConfiguration";
        
        public string _androidAppId;
        public string _iOSAppId;
        
        [Tooltip("Set to use debug version of NeftaPlugin")]
        public bool _isLoggingEnabled;
    }
}