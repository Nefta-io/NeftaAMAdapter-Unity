using GoogleMobileAds.Api;
using Nefta;
using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _neftaAppId = "5650928946380800";
#else // UNITY_ANDROID
        private const string _neftaAppId = "5734113336098816";
#endif
        private bool _isBannerShown;

        [SerializeField] private BannerController _banner;
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private void Awake()
        {
            Adapter.EnableLogging(true);
            Adapter.Init(_neftaAppId);
            var debugParams = GetDebugParameters();
            if (debugParams != null)
            {
                Adapter.SetOverride(debugParams[0]);
            }
            
            Adapter.SetContentRating(Adapter.ContentRating.General);
            
            _banner.Init();
            _interstitial.Init();
            _rewarded.Init();
            
            RequestConfiguration requestConfiguration = new RequestConfiguration();
            #if UNITY_IPHONE
            requestConfiguration.TestDeviceIds.Add("284dcf66160f8ea305826b4cc2abe58e");
            #else
            
            #endif
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }
        
        private string[] GetDebugParameters()
        {
            string root = null;
#if UNITY_EDITOR
            //root = "http://192.168.0.216:8080";
#elif UNITY_IOS
            string[] args = System.Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                root = args[1];
            }
#elif UNITY_ANDROID
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            root = intent.Call<string>("getStringExtra", "override");
#endif
            if (!string.IsNullOrEmpty(root))
            {
                return new[]{ root };
            }

            return null;
        }
    }
}
