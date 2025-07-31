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
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;

        private BannerView _bannerView;
        private string _selectedAdUnit;
        private AdInsight _usedInsight;
        
        private static readonly Queue<Action> _mainThreadQueue = new();
        
        private void GetInsightsAndLoad()
        {
            Adapter.GetInsights(Insights.Banner, Load, 5);
        }
        
        private void Load(Insights insights)
        {
            _selectedAdUnit = DefaultAdUnitId;
            _usedInsight = insights._banner;
            if (_usedInsight != null) {
                _selectedAdUnit = _usedInsight._adUnit;
            }
            
            _bannerView = new BannerView(_selectedAdUnit, AdSize.Banner, AdPosition.Top);
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
            Adapter.OnExternalMediationRequestFailed(Adapter.AdType.Banner, _usedInsight, _selectedAdUnit, error);
            
            SetStatus($"BannerOnAdLoadFailedEvent {error}");
            
            StartCoroutine(ReTryLoad());
        }
        
        private void BannerOnAdLoadedEvent()
        {
            Adapter.OnExternalMediationRequestLoaded(_usedInsight, _selectedAdUnit, _bannerView);

            SetStatus("BannerOnAdLoadedEvent");
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            Adapter.OnExternalMediationImpression(_selectedAdUnit, _bannerView, adValue);

            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { SetStatus($"OnAdPaid {adValue.Value}"); });
            }
        }
        
        private IEnumerator ReTryLoad()
        {
            yield return new WaitForSeconds(5f);
            
            GetInsightsAndLoad();
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
            
            _show.interactable = true;
            _hide.interactable = false;
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
            _status.text = status;
        }
        
        private void Update()
        {
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    _mainThreadQueue.Dequeue()?.Invoke();
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