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

        private AdRequest _dynamicRequest;
        private AdInsight _dynamicInsight;
        private InterstitialAd _dynamicInterstitial;
        private AdRequest _defaultRequest;
        private InterstitialAd _defaultInterstitial;
        private InterstitialAd _presentingInterstitial;
        
        [SerializeField] private Text _title;
        [SerializeField] private Toggle _load;
        [SerializeField] private Text _loadText;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private static readonly Queue<Action> _mainThreadQueue = new();
        
        private void StartLoading()
        {
            if (_dynamicRequest == null)
            {
                _dynamicInsight = null;
                GetInsightsAndLoad(null);   
            }
            if (_defaultRequest == null)
            {
                LoadDefault();   
            }
        }
        
        private void GetInsightsAndLoad(AdInsight previousInsight)
        {
            _dynamicRequest = new AdRequest();
            Adapter.GetInsights(Insights.Interstitial, previousInsight, LoadWithInsights, 5);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            _dynamicInsight = insights._interstitial;
            if (_dynamicInsight != null && !string.IsNullOrEmpty(_dynamicInsight._adUnit))
            {
                var recommendedAdUnit = _dynamicInsight._adUnit;
                SetStatus($"Loading Dynamic {recommendedAdUnit}");
                Adapter.OnExternalMediationRequest(_dynamicInsight, _dynamicRequest, recommendedAdUnit);
                
                InterstitialAd.Load(recommendedAdUnit, _dynamicRequest, (InterstitialAd ad, LoadAdError error) =>
                {
                    if (error != null)
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() =>
                            {
                                Adapter.OnExternalMediationRequestFailed(_dynamicRequest, error);
                                _dynamicRequest = null;
                                
                                SetStatus("Dynamic failed to load with: " + error);
                                StartCoroutine(ReTryLoad(true));
                            });
                        }

                        return;
                    }

                    if (ad == null)
                    {
                        _dynamicRequest = null;
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() => { SetStatus("Unexpected error: Dynamic load event fired with null ad and null error."); });
                        }

                        return;
                    }
                    
                    _dynamicInterstitial = ad;
                    
                    ad.OnAdPaid += OnAdPaid;
                    ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                    ad.OnAdClicked += OnAdClicked;
                    ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                    ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                    ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                    
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() =>
                        {
                            Adapter.OnExternalMediationRequestLoaded(ad, _dynamicRequest);
                            
                            SetStatus($"Dynamic {recommendedAdUnit} loaded with response: {ad}");
                            
                            UpdateShowButton();
                        });
                    }
                });
            }
            else
            {
                _dynamicRequest = null;
            }
        }

        private void LoadDefault()
        {
            SetStatus($"Loading Default: ${DefaultAdUnitId}");
            
            _defaultRequest = new AdRequest();
            Adapter.OnExternalMediationRequest(Adapter.AdType.Interstitial, _defaultRequest, DefaultAdUnitId);
            InterstitialAd.Load(DefaultAdUnitId, _defaultRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    _defaultRequest = null;
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() =>
                        {
                            Adapter.OnExternalMediationRequestFailed(_defaultRequest, error);
                            
                            SetStatus($"Default failed to load with: {error}");
                            StartCoroutine(ReTryLoad(false));
                        });
                    }
                    return;
                }

                if (ad == null)
                {
                    _defaultRequest = null;
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() => { SetStatus("Unexpected error: Default load event fired with null ad and null error."); });
                    }
                    return;
                }
                
                _defaultInterstitial = ad;
                
                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Adapter.OnExternalMediationRequestLoaded(_defaultInterstitial, _defaultRequest);
                        
                        SetStatus($"Default {DefaultAdUnitId} loaded with response {_defaultInterstitial}");

                        UpdateShowButton();
                    });
                }
            });
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    Adapter.OnExternalMediationImpression(_presentingInterstitial, adValue);
                    
                    SetStatus($"OnAdPaid {adValue.Value}");
                });
            }
        }
        
        private void OnAdClicked()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    Adapter.OnExternalMediationClick(_presentingInterstitial);

                    SetStatus("OnAdClicked");
                });
            }
        }
        
        private IEnumerator ReTryLoad(bool isDynamic)
        {
            yield return new WaitForSeconds(5f);
            
            if (_load.isOn)
            {
                if (isDynamic)
                {
                    if (_dynamicRequest == null)
                    {
                        GetInsightsAndLoad(_dynamicInsight);
                    }    
                }
                else
                {
                    if (_defaultRequest == null)
                    {
                        LoadDefault();
                    }
                }
            }
        }
        
        public void Init()
        {
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.onClick.AddListener(OnShowClick);
            _show.interactable = false;
            
            SetStatus("Interstitial status");
        }

        private void OnLoadChanged(bool isOn)
        {
            if (isOn)
            {
                StartLoading();   
            }
            else
            {
                _dynamicInsight = null;
            }
            
            AddDemoGameEventExample();
        }
        
        private void OnShowClick()
        {
            var isShown = false;
            if (_dynamicInterstitial != null)
            {
                if (_dynamicInterstitial.CanShowAd())
                {
                    isShown = true;
                    SetStatus("Showing Dynamic");
                    _dynamicInterstitial.Show();
                    _presentingInterstitial = _dynamicInterstitial;
                }
                _dynamicInterstitial = null;
                _dynamicRequest = null;
            }

            if (!isShown && _defaultInterstitial != null)
            {
                if (_defaultInterstitial.CanShowAd())
                {
                    SetStatus("Showing Default");
                    _defaultInterstitial.Show();
                    _presentingInterstitial = _defaultInterstitial;
                }
                _defaultInterstitial = null;
                _defaultRequest = null;
            }

            UpdateShowButton();
        }

        private void OnAdImpressionRecorded()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { SetStatus("OnAdImpressionRecorded"); });
            }
        }

        private void OnAdFullScreenContentOpened()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { SetStatus("OnAdFullScreenContentOpened"); });
            }
        }

        private void OnAdFullScreenContentClosed()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    SetStatus("OnAdFullScreenContentClosed");

                    _presentingInterstitial = null;
            
                    // start new cycle
                    if (_load.isOn)
                    {
                        StartLoading();
                    }
                });
            }
        }
        
        private void OnAdFullScreenContentFailed(AdError error)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { SetStatus($"OnAdFullScreenContentFailed {error}"); });
            }
        }

        private void SetStatus(string status)
        {
            Debug.Log($"NeftaPluginAM Interstitial {status}");
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
        
        private void UpdateShowButton()
        {
            _show.interactable = _dynamicInterstitial != null || _defaultInterstitial != null;
        }
    }
}