using UnityEngine;

namespace AdDemo
{
    public class AdDemoController : MonoBehaviour
    {
        private bool _isBannerShown;

        [SerializeField] private BannerController _banner;
        [SerializeField] private InterstitialController _interstitial;
        [SerializeField] private RewardedController _rewarded;
        
        private void Awake()
        {
            Nefta.Adapter.Init();

            _banner.Init();
            _interstitial.Init();
            _rewarded.Init();
        }
    }
}
