using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using Nefta;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class Rewarded : MonoBehaviour
    {
#if UNITY_IPHONE
        private const string AdUnitA = "ca-app-pub-1193175835908241/1793039068";
        private const string AdUnitB = "ca-app-pub-1193175835908241/9522486356";
#else
        private const string AdUnitA = "ca-app-pub-1193175835908241/2054545908";
        private const string AdUnitB = "ca-app-pub-1193175835908241/8239541882";
#endif

        public enum State
        {
            Idle,
            LoadingWithInsights,
            Loading,
            Ready,
            Shown
        }

        public class Track
        {
            public readonly string AdUnitId;
            public double FloorPrice;
            public AdRequest Request;
            public RewardedAd Ad;
            public State State;
            public AdInsight Insight;

            public Track(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
            
            private void OnLoadCallbackOnMain(RewardedAd ad, LoadAdError error)
            {
                if (error != null)
                {
                    Adapter.OnExternalMediationRequestFailed(Request, error);

                    Instance.SetStatus($"{AdUnitId} load failed: " + error);
                    
                    RestartAfterFailedLoad();
                    return;
                }
                        
                if (ad == null)
                {
                    Request = null;
                    Instance.SetStatus("Unexpected error: Dynamic load event fired with null ad and null error.");

                    RestartAfterFailedLoad();
                    return;
                }
                
                Insight = null;
                State = State.Ready;
                Ad = ad;

                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
                        
                Adapter.OnExternalMediationRequestLoaded(ad, Request);

                Instance.SetStatus($"{AdUnitId} loaded with response: {ad}");

                Instance.OnTrackLoad(true);
            }
            
            private void OnAdPaid(AdValue adValue)
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Adapter.OnExternalMediationImpression(Ad, adValue);
                    
                        Instance.SetStatus($"OnAdPaid {adValue.Value}");
                    });
                }
            }
        
            private void OnAdClicked()
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Adapter.OnExternalMediationClick(Ad);

                        Instance.SetStatus("OnAdClicked");
                    });
                }
            }
            
            private void OnAdImpressionRecorded()
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() => { Instance.SetStatus("OnAdImpressionRecorded"); });
                }
            }

            private void OnAdFullScreenContentOpened()
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() => { Instance.SetStatus("OnAdFullScreenContentOpened"); });
                }
            }

            private void OnAdFullScreenContentClosed()
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Instance.SetStatus("OnAdFullScreenContentClosed");
                        
                        State = State.Idle;
                        Ad = null;
                        Instance.RetryLoadTracks();
                    });
                }
            }
        
            private void OnAdFullScreenContentFailed(AdError error)
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Instance.SetStatus($"OnAdFullScreenContentFailed {error}");
                        
                        State = State.Idle;
                        Ad = null;
                        Instance.RetryLoadTracks();
                    });
                }
            }
            
            public void OnLoadCallback(RewardedAd ad, LoadAdError error)
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        OnLoadCallbackOnMain(ad, error);
                    });
                }
            }
            
            public void RestartAfterFailedLoad()
            {
                _ = RetryLoadWithDelay();

                Instance.OnTrackLoad(false);
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
                State = State.Idle;
                Instance.RetryLoadTracks();
            }
        }
        
        private Track _trackA;
        private Track _trackB;
        private bool _isFirstResponseReceived;
        
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        
        public static Rewarded Instance;
        
        private void LoadTracks()
        {
            LoadTrack(_trackA, _trackB.State);
            LoadTrack(_trackB, _trackA.State);
        }
        
        private void LoadTrack(Track track, State otherState)
        {
            if (track.State == State.Idle)
            {
                if (otherState == State.LoadingWithInsights || otherState == State.Shown)
                {
                    if (_isFirstResponseReceived)
                    {
                        LoadDefault(track);
                    }
                }
                else
                {
                    GetInsightsAndLoad(track); 
                }
            }
        }
        
        private void GetInsightsAndLoad(Track track)
        {
            track.State = State.LoadingWithInsights;
            
            Adapter.GetInsights(Insights.Rewarded, track.Insight, (Insights insights) =>
            {
                var insight = insights._rewarded;
                if (insight != null)
                {
                    track.Insight = insight;
                    track.FloorPrice = insight._floorPrice;
                    track.Request = new AdRequest();
                    
                    // map floorPrice to your AdMob Pro mediation group configuration
                    // sample KVP mapping:
                    string mediationGroup = "low";
                    if (track.FloorPrice > 100)
                    {
                        mediationGroup = "high";
                    }
                    else if (track.FloorPrice > 50)
                    {
                        mediationGroup = "medium";
                    }
                    track.Request.Extras = new Dictionary<string, string>()
                    {
                        { "mediation group key", mediationGroup },
                    };
                    
                    Adapter.OnExternalMediationRequest(insight, track.Request, insight._adUnit);

                    SetStatus($"Loading {insight._adUnit} as Optimized with {track.FloorPrice}");
                    RewardedAd.Load(insight._adUnit, track.Request, track.OnLoadCallback);
                }
                else
                {
                    track.RestartAfterFailedLoad();
                }
            }, 5);
        }

        private void LoadDefault(Track track)
        {
            track.State = State.Loading;
            
            track.FloorPrice = 0;
            track.Request = new AdRequest();

            Adapter.OnExternalMediationRequest(Adapter.AdType.Interstitial, track.Request, track.AdUnitId);

            SetStatus($"Loading {track.AdUnitId} as Default");
            RewardedAd.Load(track.AdUnitId, track.Request, track.OnLoadCallback);
        }

        private void Awake()
        {
            Instance = this;
            
            _trackA = new Track(AdUnitA);
            _trackB = new Track(AdUnitB);
            
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.onClick.AddListener(OnShowClick);
            _show.interactable = false;

            SetStatus("Rewarded status");
        }
        
        private void OnLoadChanged(bool isOn)
        {
            if (isOn)
            {
                LoadTracks();   
            }
        }
        
        private void OnShowClick()
        {
            var isShown = false;
            if (_trackA.State == State.Ready)
            {
                if (_trackB.State == State.Ready && _trackB.FloorPrice > _trackA.FloorPrice)
                {
                    isShown = TryShow(_trackB);
                }
                if (!isShown)
                {
                    isShown = TryShow(_trackA);
                }
            }
            if (!isShown && _trackB.State == State.Ready)
            {
                TryShow(_trackB);
            }
            UpdateShowButton();
        }
        
        public bool TryShow(Track request)
        {
            request.FloorPrice = 0;
            if (request.Ad.CanShowAd())
            {
                request.State = State.Shown;
                request.Ad.Show(
                    (Reward reward) =>
                    {
                        lock (_mainThreadQueue)
                        {
                            _mainThreadQueue.Enqueue(() => { SetStatus($"Reward granted: {reward.Amount} {reward.Type}"); });
                        }
                    });
                return true;
            }
            request.State = State.Idle;
            RetryLoadTracks();
            return false;
        }
        
        public void RetryLoadTracks()
        {
            if (_load.isOn)
            {
                LoadTracks();
            }
        }
        
        public void OnTrackLoad(bool success)
        {
            if (success)
            {
                UpdateShowButton();
            }

            _isFirstResponseReceived = true;
            RetryLoadTracks();
        }
        
        private void SetStatus(string status)
        {
            Debug.Log($"NeftaPluginAM Rewarded {status}");
            _status.text = status;
        }
        
        private void UpdateShowButton()
        {
            _show.interactable = _trackA.State == State.Ready || _trackB.State == State.Ready;
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
    }
}