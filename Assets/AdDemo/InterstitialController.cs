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
    public class InterstitialController : MonoBehaviour
    {
        private class State
        {
            public String _status;
            public bool _canShow;
            public bool _isDirty;
        }
        
#if UNITY_IPHONE
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/4419202401";
#else
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/9381753032";
#endif
        private const string AdUnitIdInsightName = "recommended_interstitial_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_interstitial";
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private Coroutine _fallbackCoroutine;
        private string _loadedAdUnitId;
        private InterstitialAd _interstitialAd;
        
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

            Debug.Log($"OnBehaviourInsight for Interstitial recommended AdUnit: {_recommendedAdUnitId}, calculated bid floor: {_calculatedBidFloor}");
            
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
            InterstitialAd.Load(_loadedAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Adapter.OnExternalMediationRequestFailed(Adapter.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, error);
                    
                    SetStatus("Interstitial ad failed to load an ad with error : " + error);
                    return;
                }
                if (ad == null)
                {
                    SetStatus("Unexpected error: Interstitial load event fired with null ad and null error.");
                    return;
                }
                
                Adapter.OnExternalMediationRequestLoaded(Adapter.AdType.Interstitial, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, ad);
                
                SetStatus("Interstitial ad loaded with response : " + ad.GetResponseInfo(), true);
                _interstitialAd = ad;
                _interstitialAd.OnAdPaid += OnAdPaid;
                _interstitialAd.OnAdImpressionRecorded += OnAdImpressionRecorded;
                _interstitialAd.OnAdClicked += OnAdClicked;
                _interstitialAd.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                _interstitialAd.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                _interstitialAd.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
            });
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            Adapter.OnExternalMediationImpression(Adapter.AdType.Interstitial, _loadedAdUnitId, adValue);

            SetStatus($"OnAdPaid {adValue.Value}");
        }
        
        public void Init()
        {
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            SetStatus("Interstitial status", false);
        }

        private void OnLoadClick()
        {
            GetInsightsAndLoad();
            
            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                SetStatus("Show interstitial", false);
                _interstitialAd.Show();
            }
            else
            {
                SetStatus("Interstitial not ready", false);
            }

            _show.interactable = false;
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
            
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
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
            var method = (ReceiveMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });

        }
    }
}