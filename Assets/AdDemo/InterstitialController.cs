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
#if UNITY_IPHONE
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/4419202401";
#else
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/9381753032";
#endif
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Text _loadText;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private InterstitialAd _interstitialAd;
        private string _selectedAdUnit;
        private AdInsight _usedInsight;
        private bool _isLoading;
        
        private static readonly Queue<Action> _mainThreadQueue = new();
        
        private void GetInsightsAndLoad()
        {
            Adapter.GetInsights(Insights.Interstitial, Load, 5);
        }
        
        private void Load(Insights insights)
        {
            _selectedAdUnit = DefaultAdUnitId;
            _usedInsight = insights._interstitial;
            if (_usedInsight != null) {
                _selectedAdUnit = _usedInsight._adUnit;
            }
            
            var adRequest = new AdRequest();
            InterstitialAd.Load(_selectedAdUnit, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Adapter.OnExternalMediationRequestFailed(Adapter.AdType.Interstitial, _usedInsight, _selectedAdUnit, error);
                    
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() =>
                        {
                            SetStatus("Interstitial ad failed to load an ad with error : " + error);
                            StartCoroutine(ReTryLoad());
                        });
                    }
                    return;
                }
                if (ad == null)
                {
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() =>
                        {
                            SetStatus("Unexpected error: Interstitial load event fired with null ad and null error.");
                        });
                    }
                    return;
                }
                
                Adapter.OnExternalMediationRequestLoaded(_usedInsight, _selectedAdUnit, ad);
                
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        SetStatus($"Interstitial ad loaded with response: {_selectedAdUnit}");
                        SetLoadingButton(false);
                        _load.interactable = false;
                        _show.interactable = true;
                    });
                }
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
            Adapter.OnExternalMediationImpression(_selectedAdUnit, _interstitialAd, adValue);

            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { SetStatus($"OnAdPaid {adValue.Value}"); });
            }
        }
        
        private IEnumerator ReTryLoad()
        {
            yield return new WaitForSeconds(5f);
            
            if (_isLoading)
            {
                GetInsightsAndLoad();   
            }
        }
        
        public void Init()
        {
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);
            _show.interactable = false;
            
            SetStatus("Interstitial status");
        }

        private void OnLoadClick()
        {
            if (_isLoading)
            {
                SetLoadingButton(false);
            }
            else
            {
                SetStatus("GetInsightsAndLoad...");
                GetInsightsAndLoad();
                SetLoadingButton(true);
                AddDemoGameEventExample();
            }
        }
        
        private void OnShowClick()
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                SetStatus("Show interstitial");
                _interstitialAd.Show();
            }
            else
            {
                SetStatus("Interstitial not ready");
            }

            _load.interactable = true;
            _show.interactable = false;
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
            var category = (ResourceCategory) UnityEngine.Random.Range(0, 9);
            var method = (ReceiveMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new ReceiveEvent(category) { _method = method, _name = $"receive_{category} {method} {value}", _value = value });
        }
        
        private void SetLoadingButton(bool isLoading)
        {
            _isLoading = isLoading;
            if (_isLoading)
            {
                _loadText.text = "Cancel";
            }
            else
            {
                _loadText.text = "Load";
            }
        }
    }
}