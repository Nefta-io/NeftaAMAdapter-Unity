using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using Nefta;
using UnityEngine;

namespace AdDemo
{
    public class RewardedDefault : IRewarded
    {
        private string _adUnitId;
        private AdRequest _request;
        private RewardedAd _rewarded;
        private static readonly Queue<Action> _mainThreadQueue = new();
        private RewardedUi _ui;
        
        public void Init(RewardedUi ui)
        {
            _adUnitId = RewardedUi.AdUnitA;
            _ui = ui;
        }

        public void Load()
        {
            _request = new AdRequest();
            Adapter.OnExternalMediationRequest(Adapter.AdType.Rewarded, _request, _adUnitId);
            
            _ui.SetStatus($"Loading {_adUnitId} as Default");
            RewardedAd.Load(_adUnitId, _request, OnLoadCallback);
        }
        
        public void OnLoadCallback(RewardedAd ad, LoadAdError error)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    if (error != null)
                    {
                        Adapter.OnExternalMediationRequestFailed(_request, error);

                        _ui.SetStatus($"{_adUnitId} load failed: " + error);
                    
                        _ = RetryLoadWithDelay();
                        return;
                    }
                        
                    if (ad == null)
                    {
                        _request = null;
                        _ui.SetStatus("Unexpected error: Dynamic load event fired with null ad and null error.");

                        _ = RetryLoadWithDelay();
                        return;
                    }

                    _rewarded = ad;

                    ad.OnAdPaid += OnAdPaid;
                    ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                    ad.OnAdClicked += OnAdClicked;
                    ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                    ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                    ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                        
                    Adapter.OnExternalMediationRequestLoaded(ad, _request);

                    _ui.SetStatus($"{_adUnitId} loaded with response: {ad}");

                    _ui.SetAvailability(true);
                });
            }
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    Adapter.OnExternalMediationImpression(_rewarded, adValue);
                    
                    _ui.SetStatus($"OnAdPaid {adValue.Value}");
                });
            }
        }
        
        private void OnAdClicked()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    Adapter.OnExternalMediationClick(_rewarded);

                    _ui.SetStatus("OnAdClicked");
                });
            }
        }
        
        private void OnAdImpressionRecorded()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { _ui.SetStatus("OnAdImpressionRecorded"); });
            }
        }

        private void OnAdFullScreenContentOpened()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() => { _ui.SetStatus("OnAdFullScreenContentOpened"); });
            }
        }

        private void OnAdFullScreenContentClosed()
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    _ui.SetStatus("OnAdFullScreenContentClosed");
                        
                    if (_ui.IsAutoLoad)
                    {
                        _rewarded = null;
                        Load();
                    }
                });
            }
        }
        
        private void OnAdFullScreenContentFailed(AdError error)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    _ui.SetStatus($"OnAdFullScreenContentFailed {error}");
                        
                    if (_ui.IsAutoLoad)
                    {
                        _rewarded = null;
                        Load();
                    }
                });
            }
        }
        
        private async Task RetryLoadWithDelay()
        {
            await Task.Delay(5000);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (_ui.IsAutoLoad)
            {
                Load();
            }
        }

        public void Show()
        {
            if (_rewarded.CanShowAd())
            {
                _rewarded.Show(
                    (Reward reward) =>
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() => { _ui.SetStatus($"Reward granted: {reward.Amount} {reward.Type}"); });
                        }
                    });
            }
            else if (_ui.IsAutoLoad)
            {
                Load();
            }
        }

        public void OnUpdate()
        {
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    _mainThreadQueue.Dequeue()?.Invoke();
                }
            }
        }
    }
}