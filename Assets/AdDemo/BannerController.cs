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
    public class BannerController : MonoBehaviour
    {
        private class State
        {
            public String _status;
            public bool _isDirty;
        }
        
#if UNITY_IPHONE
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/1984610756";
#else
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/4544977870";
#endif
        private const string AdUnitIdInsightName = "recommended_banner_ad_unit_id";
        private const string FloorPriceInsightName = "calculated_user_floor_price_banner";
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;

        private BannerView _bannerView;
        private string _recommendedAdUnitId;
        private double _calculatedBidFloor;
        private Coroutine _fallbackCoroutine;
        private String _loadedAdUnitId;
        
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
            
            Debug.Log($"OnBehaviourInsight for Banner calculated bid floor: {_calculatedBidFloor}");
            
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
            
            _bannerView = new BannerView(_loadedAdUnitId, AdSize.Banner, AdPosition.Top);
            _bannerView.OnBannerAdLoaded += BannerOnAdLoadedEvent;
            _bannerView.OnBannerAdLoadFailed += BannerOnAdLoadFailedEvent;
            _bannerView.OnAdClicked += BannerOnAdClickedEvent;
            _bannerView.OnAdFullScreenContentOpened += BannerOnAdScreenPresentedEvent;
            _bannerView.OnAdFullScreenContentClosed += BannerOnAdScreenDismissedEvent;
            _bannerView.OnAdPaid += OnAdPaid;
            
            var adRequest = new AdRequest();
            _bannerView.LoadAd(adRequest);
        }
        
        private void BannerOnAdLoadFailedEvent(LoadAdError error)
        {
            Adapter.OnExternalMediationRequestFailed(Adapter.AdType.Banner, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, error);
            
            SetStatus($"BannerOnAdLoadFailedEvent {error}");
        }
        
        private void BannerOnAdLoadedEvent()
        {
            Adapter.OnExternalMediationRequestLoaded(Adapter.AdType.Banner, _recommendedAdUnitId, _calculatedBidFloor, _loadedAdUnitId, _bannerView);

            SetStatus("BannerOnAdLoadedEvent");
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            Adapter.OnExternalMediationImpression(Adapter.AdType.Banner, _loadedAdUnitId, adValue);

            SetStatus($"OnAdPaid {adValue.Value}");
        }
        
        public void Init()
        {
            _title.text = "Banner info";
            _show.onClick.AddListener(OnShowClick);
            _hide.onClick.AddListener(OnHideClick);
            _hide.interactable = false;
        }
        
        private void OnShowClick()
        {
            GetInsightsAndLoad();
            
            SetStatus("Loading banner ad..");
            
            _show.interactable = false;
            _hide.interactable = true;

            AddDemoGameEventExample();
        }
        
        private void OnHideClick()
        {
            if (_bannerView != null)
            {
                SetStatus("Destroying banner view.");
                _bannerView.Destroy();
                _bannerView = null;
            }
            _hide.interactable = false;
        }
        
        private IEnumerator LoadFallback()
        {
            yield return new WaitForSeconds(5f);

            _recommendedAdUnitId = null;
            _calculatedBidFloor = 0f;
            Load();
        }

        private void BannerOnAdClickedEvent()
        {
            SetStatus("BannerOnAdClickedEvent");
        }

        private void BannerOnAdScreenPresentedEvent()
        {
            SetStatus("BannerOnAdScreenPresentedEvent");
        }

        private void BannerOnAdScreenDismissedEvent()
        {
            SetStatus("BannerOnAdScreenDismissedEvent");
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

        private void Update()
        {
            lock (_state)
            {
                if (_state._isDirty)
                {
                    _status.text = _state._status;
                    _state._isDirty = false;
                }
            }
        }

        private void AddDemoGameEventExample()
        {
            var type = (Nefta.Events.Type) UnityEngine.Random.Range(0, 7);
            var status = (Status) UnityEngine.Random.Range(0, 3);
            var source = (Source) UnityEngine.Random.Range(0, 7);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new ProgressionEvent(type, status) { _source = source, _name = $"progression_{type}_{status} {source} {value}", _value = value });
        }
    }
}