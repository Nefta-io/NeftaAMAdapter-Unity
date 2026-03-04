using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using Nefta;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedSim : MonoBehaviour
    {
        private const string AdUnitA = "AdUnit-A";
        private const string AdUnitB = "AdUnit-B";
        
        private readonly Color DefaultColor = new Color(0.6509804f, 0.1490196f, 0.7490196f, 1f);
        private readonly Color FillColor = Color.green;
        private readonly Color NoFillColor = Color.red;

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
            public SRewardedAd Ad;
            public State State;
            public AdInsight Insight;
            public int _loadSelection;

            public Track(string adUnitId)
            {
                AdUnitId = adUnitId;
            }
            
            private void OnLoadCallbackOnMain(SRewardedAd ad, LoadAdError error)
            {
                if (error != null)
                {
                    Adapter.OnExternalMediationRequestFailed(Request, error);

                    Instance.SetStatus($"{AdUnitId} load failed: " + error);
                    
                    AfterLoadFail();
                    return;
                }
                        
                if (ad == null)
                {
                    Request = null;
                    Instance.SetStatus("Unexpected error: Dynamic load event fired with null ad and null error.");

                    AfterLoadFail();
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
                        
                Adapter.OnExternalMediationRequestLoaded(ad._instance, Request);

                Instance.SetStatus($"{AdUnitId} loaded with response: {ad}");

                Instance.OnTrackLoad(true);
            }
            
            private void OnAdPaid(AdValue adValue)
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        Adapter.OnExternalMediationImpression(Ad._instance, adValue);
                    
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
                        Adapter.OnExternalMediationClick(Ad._instance);

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
            
            public void OnLoadCallback(SRewardedAd ad, LoadAdError error)
            {
                lock (_mainThreadQueue)
                {
                    _mainThreadQueue.Enqueue(() =>
                    {
                        OnLoadCallbackOnMain(ad, error);
                    });
                }
            }
            
            public void AfterLoadFail()
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
        
        [Header("Controls")]
        [SerializeField] private RectTransform _rootRect;
        [SerializeField] private Toggle _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        [Header("Track A")]
        [SerializeField] private Image _aFill2Renderer;
        [SerializeField] private Button _aFill2;
        [SerializeField] private Image _aFill1Renderer;
        [SerializeField] private Button _aFill1;
        [SerializeField] private Image _aNoFillRenderer;
        [SerializeField] private Button _aNoFill;
        [SerializeField] private Image _aOtherRenderer;
        [SerializeField] private Button _aOther;
        [SerializeField] private Text _aStatus;
        
        [Header("Track B")]
        [SerializeField] private Image _bFill2Renderer;
        [SerializeField] private Button _bFill2;
        [SerializeField] private Image _bFill1Renderer;
        [SerializeField] private Button _bFill1;
        [SerializeField] private Image _bNoFillRenderer;
        [SerializeField] private Button _bNoFill;
        [SerializeField] private Image _bOtherRenderer;
        [SerializeField] private Button _bOther;
        [SerializeField] private Text _bStatus;

        private static readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        
        public static RewardedSim Instance;
        
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

                    Adapter.OnExternalMediationRequest(insight, track.Request, track.AdUnitId);

                    SetStatus($"Loading {insight._adUnit} as Optimized with {track.FloorPrice}");
                    SRewardedAd.Load(track.AdUnitId, track.Request, track.OnLoadCallback);
                }
                else
                {
                    track.AfterLoadFail();
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
            SRewardedAd.Load(track.AdUnitId, track.Request, track.OnLoadCallback);
        }

        private void Awake()
        {
            Instance = this;
            
            _trackA = new Track(AdUnitA);
            _trackB = new Track(AdUnitB);
            
            ToggleTrackA(false);
            _aFill2.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackA, true); });
            _aFill1.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackA, false); });
            _aNoFill.onClick.AddListener(() => { SimOnAdFailedEvent(_trackA, 2); });
            _aOther.onClick.AddListener(() => { SimOnAdFailedEvent(_trackA, 0); });
            
            ToggleTrackB(false);
            _bFill2.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackB, true); });
            _bFill1.onClick.AddListener(() => { SimOnAdLoadedEvent(_trackB, false); });
            _bNoFill.onClick.AddListener(() => { SimOnAdFailedEvent(_trackB, 2); });
            _bOther.onClick.AddListener(() => { SimOnAdFailedEvent(_trackB, 0); });
            
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
            if (request == _trackA)
            {
                ToggleTrackA(true);
                ToggleTrackA(false);
            }
            else
            {
                ToggleTrackB(true);
                ToggleTrackB(false);
            }
            
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
        
        private void ToggleTrackA(bool isOn)
        {
            _aFill2.interactable = isOn;
            _aFill1.interactable = isOn;
            _aNoFill.interactable = isOn;
            _aOther.interactable = isOn;
            if (isOn)
            {
                _aFill2Renderer.color = DefaultColor;
                _aFill1Renderer.color = DefaultColor;
                _aNoFillRenderer.color = DefaultColor;
                _aOtherRenderer.color = DefaultColor;
            }
        }

        private void ToggleTrackB(bool isOn)
        {
            _bFill2.interactable = isOn;
            _bFill1.interactable = isOn;
            _bNoFill.interactable = isOn;
            _bOther.interactable = isOn;
            if (isOn)
            {
                _bFill2Renderer.color = DefaultColor;
                _bFill1Renderer.color = DefaultColor;
                _bNoFillRenderer.color = DefaultColor;
                _bOtherRenderer.color = DefaultColor;
            }
        }
        
        private void SimOnAdLoadedEvent(Track request, bool high)
        {
            var revenue = high ? 0.002 : 0.001;
            if (request.Ad != null && request.Ad._hasFill)
            {
                request.Ad._hasFill = false;
                if (request == _trackA)
                {
                    if (high)
                    {
                        _aFill2Renderer.color = DefaultColor;
                        _aFill2.interactable = false;
                    }
                    else
                    {
                        _aFill1Renderer.color = DefaultColor;
                        _aFill1.interactable = false;
                    }
                }
                if (request == _trackB)
                {
                    if (high)
                    {
                        _bFill2Renderer.color = DefaultColor;
                        _bFill2.interactable = false;
                    }
                    else
                    {
                        _bFill1Renderer.color = DefaultColor;
                        _bFill1.interactable = false;
                    }
                }
                return;
            }
            
            if (request == _trackA)
            {
                ToggleTrackA(false);
                if (high)
                {
                    _aFill2Renderer.color = FillColor;
                    _aFill2.interactable = true;
                    request._loadSelection = 1;
                }
                else
                {
                    _aFill1Renderer.color = FillColor;
                    _aFill1.interactable = true;
                    request._loadSelection = 2;
                }
                _aStatus.text = $"{request.AdUnitId} loaded {revenue}";
            }
            else
            {
                ToggleTrackB(false);
                if (high)
                {
                    _bFill2Renderer.color = FillColor;
                    _bFill2.interactable = true;
                    request._loadSelection = 1;
                }
                else
                {
                    _bFill1Renderer.color = FillColor;
                    _bFill1.interactable = true;
                    request._loadSelection = 2;
                }
                _bStatus.text = $"{request.AdUnitId} loaded {revenue}";
            }
        }
        
        private void SimOnAdFailedEvent(Track adRequest, int status)
        {
            if (adRequest == _trackA)
            {
                if (status == 2)
                {
                    _aNoFillRenderer.color = NoFillColor;
                    adRequest._loadSelection = 3;
                }
                else
                {
                    _aOtherRenderer.color = NoFillColor;
                    adRequest._loadSelection = 4;
                }
                _aStatus.text = $"{adRequest.AdUnitId} failed";
                ToggleTrackA(false);
            }
            else
            {
                if (status == 2)
                {
                    _bNoFillRenderer.color = NoFillColor;
                    adRequest._loadSelection = 3;
                }
                else
                {
                    _bOtherRenderer.color = NoFillColor;
                    adRequest._loadSelection = 4;
                }
                _bStatus.text = $"{adRequest.AdUnitId} failed";
                ToggleTrackB(false);
            }
        }

        public class SRewardedAd
        {
            public bool _hasFill = true;
            public RewardedAd _instance;
            
            public event Action<AdValue> OnAdPaid;
            public event Action OnAdClicked;
            public event Action OnAdImpressionRecorded;
            public event Action OnAdFullScreenContentOpened;
            public event Action OnAdFullScreenContentClosed;
            public event Action<AdError> OnAdFullScreenContentFailed;
            
            public static void Load(
                string adUnitId,
                AdRequest request,
                Action<SRewardedAd, LoadAdError> adLoadCallback)
            {
                Track track = null;
                if (Instance._trackA.Request == request)
                {
                    track = Instance._trackA;
                    Instance.ToggleTrackA(true);
                }
                else
                {
                    track = Instance._trackB;
                    Instance.ToggleTrackB(true);
                }
                Instance.StartCoroutine(OnLoad(track, adLoadCallback));
            }

            public bool CanShowAd() => _hasFill;

            private static IEnumerator OnLoad(Track track, Action<SRewardedAd, LoadAdError> adLoadCallback)
            {
                track._loadSelection = 0;
                while (track._loadSelection == 0)
                {
                    yield return null;
                }

                if (track._loadSelection == 1)
                {
                    track.Ad = new SRewardedAd();
                    track.Ad._instance = (RewardedAd)FormatterServices.GetUninitializedObject(typeof(RewardedAd));
                    adLoadCallback(track.Ad, null);
                }
                else if (track._loadSelection == 2)
                {
                    track.Ad = new SRewardedAd();
                    track.Ad._instance = (RewardedAd)FormatterServices.GetUninitializedObject(typeof(RewardedAd));
                    adLoadCallback(track.Ad, null);
                }
                else if (track._loadSelection == 3)
                {
                    var e = new LoadAdError(new SLoadAdError(2));
                    adLoadCallback(null, e);
                }
                else if (track._loadSelection == 4)
                {
                    var e = new LoadAdError(new SLoadAdError(0));
                    adLoadCallback(null, e);
                }
            }

            public void Show(Action<Reward> onReward)
            {
                if (!_hasFill)
                {
                    OnAdFullScreenContentFailed(new AdError(null));
                    return;
                }
                
                NativeAd.ShowAd("Rewarded",
                    () =>
                    {
                        OnAdFullScreenContentOpened();
                        OnAdImpressionRecorded();
                        OnAdPaid(new AdValue() { Precision = AdValue.PrecisionType.Precise, CurrencyCode = "USD", Value = 1});
                    },
                    () => { OnAdClicked(); },
                    () => { onReward(new Reward() { Amount = 1, Type = "USD" }); },
                    () => { OnAdFullScreenContentClosed(); });
            }
            
            private class SLoadAdError : ILoadAdErrorClient
            {
                private int _code;
                private string _message;
                
                public SLoadAdError(int status)
                {
                    _code = status == 2 ? -2 : -1;
                    _message = status == 2 ? "no fill" : "other";
                }
                
                public int GetCode()
                {
                    return _code;
                }

                public string GetDomain()
                {
                    return "sim domain";
                }

                public string GetMessage()
                {
                    return _message;
                }

                public IAdErrorClient GetCause()
                {
                    return null;
                }

                public IResponseInfoClient GetResponseInfoClient()
                {
                    return null;
                }
            }
        }
    }
}