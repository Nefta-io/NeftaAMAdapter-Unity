using System;
using GoogleMobileAds.Api;
using Nefta;
using Nefta.Events;
using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedController : MonoBehaviour
    {
        private class State
        {
            public String _status;
            public bool _canShow;
            public bool _isDirty;
        }
        
#if UNITY_ANDROID
        private const string _adUnitId = "ca-app-pub-1193175835908241/2054545908";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-1193175835908241/1793039068";
#endif
        
        [SerializeField] private Text _title;
        [SerializeField] private Button _load;
        [SerializeField] private Button _show;
        [SerializeField] private Text _status;
        
        private RewardedAd _rewardedAd;
        private State _state = new State();
        
        public void Init()
        {
            _load.onClick.AddListener(OnLoadClick);
            _show.onClick.AddListener(OnShowClick);

            SetStatus("Rewarded status", false);
        }
        
        private void OnLoadClick()
        {
            var category = (ResourceCategory) UnityEngine.Random.Range(0, 9);
            var method = (SpendMethod) UnityEngine.Random.Range(0, 8);
            var value = UnityEngine.Random.Range(0, 101);
            Adapter.Record(new SpendEvent(category) { _method = method, _name = $"spend_{category} {method} {value}", _value = value });
            
            SetStatus("Loading rewarded...");
            var adRequest = new AdRequest();

            RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    SetStatus("Rewarded ad failed to load an ad with error : " + error);
                    return;
                }
                if (ad == null)
                {
                    SetStatus("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }
                
                SetStatus($"Rewarded ad loaded with response {ad.GetResponseInfo()}", true);
                _rewardedAd = ad;

                ad.OnAdPaid += OnAdPaid;
                ad.OnAdImpressionRecorded += OnAdImpressionRecorded;
                ad.OnAdClicked += OnAdClicked;
                ad.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
                ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
                ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
            });
        }
        
        private void OnShowClick()
        {
            _show.interactable = false;
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                SetStatus("Showing rewarded", false);
                _rewardedAd.Show((Reward reward) =>
                {
                    SetStatus(String.Format("Rewarded ad granted a reward: {0} {1}",
                        reward.Amount,
                        reward.Type));
                });
            }
            else
            {
                SetStatus("Rewarded ad is not ready yet.", false);
            }
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            SetStatus($"OnAdPaid {adValue}");
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