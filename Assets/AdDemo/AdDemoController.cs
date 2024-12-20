using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
#if UNITY_IOS
        private const string _neftaAppId = "5661184053215232";
#else // UNITY_ANDROID
        private const string _neftaAppId = "5643649824063488";
#endif
        private bool _isBannerShown;

        [SerializeField] private BannerController _banner;
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private void Awake()
        {
            Nefta.Adapter.Init(_neftaAppId);

            _banner.Init();
            _interstitial.Init();
            _rewarded.Init();
        }
    }
}
