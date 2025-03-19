using System;
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
        
#if UNITY_ANDROID
        private const string _adUnitId = "ca-app-pub-1193175835908241/9381753032";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-1193175835908241/4419202401";
#endif
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private InterstitialAd _interstitialAd;
        private State _state = new State();
        
        public void Init()
        {
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            
            SetStatus("Interstitial status", false);
        }

        private void OnLoadClick()
        {
            var category = (ResourceCategory) UnityEngine.Random.Range(0, 9);
            var method = (ReceiveMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });
            
            SetStatus("Loading interstitial...");
            var adRequest = new AdRequest();
            
            InterstitialAd.Load(_adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    SetStatus("Interstitial ad failed to load an ad with error : " + error);
                    return;
                }
                if (ad == null)
                {
                    SetStatus("Unexpected error: Interstitial load event fired with null ad and null error.");
                    return;
                }
                
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
        }

        private void OnAdPaid(AdValue advalue)
        {
            SetStatus($"OnAdPaid {advalue}");
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
    }
}