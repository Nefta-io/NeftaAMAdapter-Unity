using GoogleMobileAds.Api;
using Nefta;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _neftaAppId = "5650928946380800";
#else // UNITY_ANDROID
        private const string _neftaAppId = "5734113336098816";
#endif
        
        [SerializeField] private Text _title;
        [SerializeField] private GameObject _groupPanel;
        [SerializeField] private Button _defaultButton;
        [SerializeField] private Button _optimizedButton;
        [SerializeField] private Button _simulatorButton;
        
        [SerializeField] private InterstitialUi _interstitialUi;
        [SerializeField] private RewardedUi _rewardedUi;
        
        [SerializeField] private InterstitialSim _interstitialSimulator;
        [SerializeField] private RewardedSim _rewardedSimulator;
        
        private void Awake()
        {
            Adapter.EnableLogging(true);
            Adapter.InitWithAppId(_neftaAppId, (InitConfiguration initConfig) => {
                Debug.Log($"Nefta Initialized, nuid: {initConfig._nuid}");
            });
            
            var requestConfiguration = new RequestConfiguration();
#if UNITY_IPHONE
            requestConfiguration.TestDeviceIds.Add("87b6abe09a8764496b8c5d1c1b4ac23d");
            requestConfiguration.TestDeviceIds.Add("284dcf66160f8ea305826b4cc2abe58e");
            requestConfiguration.TestDeviceIds.Add("b78b6e076ab7de99a8eb15adb2ab2634");
#else
            requestConfiguration.TestDeviceIds.Add("9429116F2099040F92F84E023664B484");
            requestConfiguration.TestDeviceIds.Add("0D61331B015C8F81BCEEC7FD449CDEE7");
            requestConfiguration.TestDeviceIds.Add("40E5105E483D16020842051E0FFDCB4D");
#endif
            MobileAds.SetRequestConfiguration(requestConfiguration);
            
            _defaultButton.onClick.AddListener(OnDefaultClick);
            _optimizedButton.onClick.AddListener(OnOptimizedClick);
            _simulatorButton.onClick.AddListener(OnSimulatorClick);
        }
        
        private void OnDefaultClick()
        {
            _groupPanel.SetActive(false);
            
            _interstitialUi.Init(new InterstitialDefault());
            _rewardedUi.Init(new RewardedDefault());
        }
        
        private void OnOptimizedClick()
        {
            _groupPanel.SetActive(false);
            
            _interstitialUi.Init(new InterstitialOptimized());
            _rewardedUi.Init(new RewardedOptimized());
        }
        
        private void OnSimulatorClick()
        {
            _groupPanel.SetActive(false);
            
            _interstitialSimulator.Init();
            _rewardedSimulator.Init();
        }
    }
}
