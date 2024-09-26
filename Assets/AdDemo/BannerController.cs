using System;
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
        
#if UNITY_ANDROID
        private const string _adUnitId = "ca-app-pub-1193175835908241/4544977870";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-1193175835908241/1984610756";
#endif
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _show;
        [SerializeField] private Button _hide;
        [SerializeField] private Text _status;

        private BannerView _bannerView;
        private State _state = new State();
        
        public void Init()
        {
            _title.text = "Banner info";
            _show.onClick.AddListener(OnShowClick);
            _hide.onClick.AddListener(OnHideClick);
            _hide.interactable = false;
        }
        
        private void OnShowClick()
        {
            _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Top);
            
            _bannerView.OnBannerAdLoaded += BannerOnAdLoadedEvent;
            _bannerView.OnBannerAdLoadFailed += BannerOnAdLoadFailedEvent;
            _bannerView.OnAdClicked += BannerOnAdClickedEvent;
            _bannerView.OnAdFullScreenContentOpened += BannerOnAdScreenPresentedEvent;
            _bannerView.OnAdFullScreenContentClosed += BannerOnAdScreenDismissedEvent;
            
            var type = (Nefta.Events.Type) UnityEngine.Random.Range(0, 7);
            var status = (Status) UnityEngine.Random.Range(0, 3);
            var source = (Source) UnityEngine.Random.Range(0, 7);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new ProgressionEvent(type, status)
                { _source = source, _name = $"progression_{type}_{status} {source} {value}", _value = value });
            
            SetStatus("Loading banner ad..");
            var adRequest = new AdRequest();
            _bannerView.LoadAd(adRequest);
            
            _hide.interactable = true;
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
        
        private void BannerOnAdLoadedEvent()
        {
            SetStatus("BannerOnAdLoadedEvent");
        }
        
        private void BannerOnAdLoadFailedEvent(LoadAdError error)
        {
            SetStatus($"BannerOnAdLoadFailedEvent {error}");
        }

        private void BannerOnAdClickedEvent()
        {
            SetStatus("BannerOnAdClickedEvent");
        }

        void BannerOnAdScreenPresentedEvent()
        {
            SetStatus("BannerOnAdScreenPresentedEvent");
        }

        void BannerOnAdScreenDismissedEvent()
        {
            SetStatus("BannerOnAdScreenDismissedEvent");
        }
        
        private void SetStatus(string status)
        {
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
    }
}