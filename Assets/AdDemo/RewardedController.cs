using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using Nefta;
using Nefta.Events;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedController : MonoBehaviour
    {
        private class State
        {
            public String _status;
            public bool _canShow;
            public bool _isDirty;
        }
        
#if UNITY_ANDROID
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/2054545908";
#elif UNITY_IPHONE
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/1793039068";
#endif
        private const string AdUnitIdInsightName = "recommended_rewarded_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_rewarded";
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private Coroutine _fallbackCoroutine;
        private string _loadedAdUnitId;
        private RewardedAd _rewardedAd;
        
        private State _state = new State();
        
        private void GetInsightsAndLoad()
        {
            Adapter.GetBehaviourInsight(new string[] { AdUnitIdInsightName, FloorPriceInsightName }, OnBehaviourInsight);
            
            _fallbackCoroutine = StartCoroutine(LoadFallback());
        }
        
        private void OnBehaviourInsight(Dictionary<string, Insight> insights)
        {
            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0;
            if (insights.TryGetValue(AdUnitIdInsightName, out var insight))
            {
                _recommendedAdUnitId = insight._string;
            }
            if (insights.TryGetValue(FloorPriceInsightName, out insight))
            {
                _calculatedBidFloor = insight._float;
            }

            Debug.Log($"OnBehaviourInsight for Rewarded: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");
            
            if (_fallbackCoroutine != null)
            {
                Load();
            }
        }

        private void Load()
        {
            if (_fallbackCoroutine != null)
            {
                StopCoroutine(_fallbackCoroutine);
                _fallbackCoroutine = null;
            }
            
            _loadedAdUnitId = DefaultAdUnitId;
            if (!string.IsNullOrEmpty(_recommendedAdUnitId))
            {
                _loadedAdUnitId = _recommendedAdUnitId;
            }
            
            var adRequest = new AdRequest();
            RewardedAd.Load(_loadedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Adapter.OnExternalMediationRequestFailed(Adapter.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, error);

                    SetStatus("Rewarded ad failed to load an ad with error : " + error);
                    return;
                }
                if (ad == null)
                {
                    SetStatus("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }
                
                Adapter.OnExternalMediationRequestLoaded(Adapter.AdType.Rewarded, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, ad);
                
                SetStatus($"Rewarded ad loaded with response {ad.GetResponseInfo()}", true);
                _rewardedAd = ad;
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
            });
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            Adapter.OnExternalMediationImpression(Adapter.AdType.Rewarded, _loadedAdUnitId, adValue);
            
            SetStatus($"OnAdPaid {adValue.Value}");
        }
        
        public void Init()
        {
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);

            SetStatus("Rewarded status", false);
        }
        
        private void OnLoadClick()
        {
            GetInsightsAndLoad();

            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                SetStatus("Showing rewarded", false);
                _rewardedAd.Show((Reward reward) =>
                {
                    SetStatus($"Rewarded ad granted a reward: {reward.Amount} {reward.Type}");
                });
            }
            else
            {
                SetStatus("Rewarded ad is not ready yet.", false);
            }
        }
        
        private IEnumerator LoadFallback()
        {
            yield return new WaitForSeconds(5f);

            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0;
            Load();
        }
        
        private void OnAdImpressionRecorded()
        {
            SetStatus("OnAdImpressionRecorded");
        }
        
        private void OnAdClicked()
        {
            SetStatus("OnAdClicked");
        }
        
        private void OnAdFullScreenContentOpened()
        {
            SetStatus("OnAdFullScreenContentOpened");
        }
        
        private void OnAdFullScreenContentClosed()
        {
            SetStatus("OnAdFullScreenContentClosed");
        }
        
        private void OnAdFullScreenContentFailed(AdError error)
        {
            SetStatus($"OnAdFullScreenContentFailed {error}");
        }
        
        private void SetStatus(string status)
        {
            Debug.Log(status);
            lock (_state)
            {
                _state._status = status;
                _state._isDirty = true;
            }
        }
        
        private void SetStatus(string status, bool canShow)
        {
            Debug.Log(status);
            lock (_state)
            {
                _state._status = status;
                _state._canShow = canShow;
                _state._isDirty = true;
            }
        }

        private void Update()
        {
            lock (_state)
            {
                if (_state._isDirty)
                {
                    _status.text = _state._status;
                    _show.interactable = _state._canShow;
                    _state._isDirty = false;
                }
            }
        }

        private void AddDemoGameEventExample()
        {
            var category = (ResourceCategory) UnityEngine.Random.Range(0, 9);
            var method = (SpendMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value });

        }
    }
}