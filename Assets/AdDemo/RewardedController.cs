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
#if UNITY_IPHONE
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/1793039068";
#else
        private const string DefaultAdUnitId = "ca-app-pub-1193175835908241/2054545908";
#endif

        private AdRequest _dynamicRequest;
        private AdInsight _dynamicInsight;
        private RewardedAd _dynamicRewarded;
        private AdRequest _defaultRequest;
        private RewardedAd _defaultRewarded;
        private RewardedAd _presentingRewarded;
        
        [SerializeField] private Text _title;
        [SerializeField] private Toggle _load;
        [SerializeField] private Text _loadText;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;

        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        
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
            Adapter.GetInsights(Insights.Rewarded, previousInsight, LoadWithInsights, 5);
        }
        
        private void LoadWithInsights(Insights insights)
        {
            _dynamicInsight = insights._rewarded;
            if (_dynamicInsight != null && !string.IsNullOrEmpty(_dynamicInsight._adUnit))
            {
                var recommendedAdUnit = _dynamicInsight._adUnit;
                SetStatus($"Loading Dynamic {recommendedAdUnit}");
                Adapter.OnExternalMediationRequest(_dynamicInsight, _dynamicRequest, recommendedAdUnit);
                
                RewardedAd.Load(recommendedAdUnit, _dynamicRequest, (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null)
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() =>
                            {
                                Adapter.OnExternalMediationRequestFailed(_dynamicRequest, error);
                                _dynamicRequest = null;
                                
                                SetStatus($"Dynamic failed to load with: {error}");
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
                    
                    _dynamicRewarded = ad;
                    
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
            Adapter.OnExternalMediationRequest(Adapter.AdType.Rewarded, _defaultRequest, DefaultAdUnitId);
            RewardedAd.Load(DefaultAdUnitId, _defaultRequest, (RewardedAd ad, LoadAdError error) =>
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
                
                _defaultRewarded = ad;
                
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
                        Adapter.OnExternalMediationRequestLoaded(_defaultRewarded, _defaultRequest);
                        
                        SetStatus($"Default Rewarded {DefaultAdUnitId} Loaded {_defaultRewarded}");

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
                    Adapter.OnExternalMediationImpression(_presentingRewarded, adValue);
                    
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
                    Adapter.OnExternalMediationClick(_presentingRewarded);
                    
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

            SetStatus("Rewarded status");
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
            if (_dynamicRewarded != null)
            {
                if (_dynamicRewarded.CanShowAd())
                {
                    isShown = true;
                    SetStatus("Showing Dynamic");
                    _dynamicRewarded.Show((Reward reward) =>
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() => { SetStatus($"Dynamic Ad granted a reward: {reward.Amount} {reward.Type}"); });
                        }
                    });
                    _presentingRewarded = _dynamicRewarded;
                }
                _dynamicRewarded = null;
                _dynamicRequest = null;
            }
            if (!isShown && _defaultRewarded != null)
            {
                if (_defaultRewarded.CanShowAd())
                {
                    SetStatus("Showing Default");
                    _defaultRewarded.Show((Reward reward) =>
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() => { SetStatus($"Default Ad granted a reward: {reward.Amount} {reward.Type}"); });
                        }
                    });
                    _presentingRewarded = _defaultRewarded;
                }
                _defaultRewarded = null;
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

                    _presentingRewarded = null;

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
            Debug.Log($"NeftaPluginAM Rewarded {status}");
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
            var method = (SpendMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value });
        }
        
        private void UpdateShowButton()
        {
            _show.interactable = _dynamicRewarded != null || _defaultRewarded != null;
        }
    }
}