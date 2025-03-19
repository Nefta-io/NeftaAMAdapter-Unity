using System.Collections.Generic;
using Nefta;
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
            Adapter.EnableLogging(true);
            Adapter.Init(_neftaAppId);
            
            Adapter.BehaviourInsightCallback = OnBehaviourInsight;
            Adapter.GetBehaviourInsight(new string[] { "p_churn_14d"});
            Adapter.SetContentRating(Adapter.ContentRating.General);

            _banner.Init();
            _interstitial.Init();
            _rewarded.Init();
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> behaviourInsight)
        {
            foreach (var insight in behaviourInsight)
            {
                var insightValue = insight.Value;
                Debug.Log($"BehaviourInsight {insight.Key} status:{insightValue._status} i:{insightValue._int} f:{insightValue._float} s:{insightValue._string}");
            }
        }
    }
}
