#if UNITY_EDITOR
using Nefta.Editor;
#elif UNITY_IOS
using System.Runtime.InteropServices;
using AOT;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using GoogleMobileAds.Api;
using Nefta.Events;
using UnityEngine;

namespace Nefta
{
    public class Adapter
    {
        public delegate void OnInsightsCallback(Insights insights);

        public enum AdType
        {
            Other = 0,
            Banner = 1,
            Interstitial = 2,
            Rewarded = 3
        }


        public enum ContentRating
        {
            Unspecified = 0,
            General = 1,
            ParentalGuidance = 2,
            Teen = 3,
            MatureAudience = 4
        }

        public struct ExtParams
        {
            public const string TestGroup = "test_group";
            public const string AttributionSource = "attribution_source";
            public const string AttributionCampaign = "attribution_campaign";
            public const string AttributionAdset = "attribution_adset";
            public const string AttributionCreative = "attribution_creative";
            public const string AttributionIncentivized = "attribution_incentivized";
        }
        
        private class InsightRequest
        {
            public int _id;
            public IEnumerable<string> _insights;
            public SynchronizationContext _returnContext;
            public OnInsightsCallback _callback;

            public InsightRequest(int id, OnInsightsCallback callback)
            {
                _id = id;
                _returnContext = SynchronizationContext.Current;
                _callback = callback;
            }
        }

        private class AMRequest
        {
            public string _adUnitId;
            public string _id0;
            public string _id2;
            public bool _hasImpression;
        }
        
        private static List<AMRequest> _amRequests = new List<AMRequest>();

#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnInsightsDelegate(int requestId, int adapterResponseType, string adapterResponse);

        [MonoPInvokeCallback(typeof(OnInsightsDelegate))] 
        private static void OnInsights(int requestId, int adapterResponseType, string adapterResponse) {
            IOnInsights(requestId, adapterResponseType, adapterResponse);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetExtraParameter(string key, string value);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnInsightsDelegate onInsights);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string provider, int adType, string id, string requestedAdUnitId, double requestedFloorPrice, int adOpportunityId);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationResponse(string provider, string id, string id2, double revenue, string precision, int status, string providerStatus, string networkStatus);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationImpressionAsString(bool isClick, string provider, string data, string id, string id2);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetInsights(int requestId, int insights, int previousAdOpportunityId, int timeoutInSeconds);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetOverride(string root);
#elif UNITY_ANDROID
        private static AndroidJavaClass _neftaPluginClass;
        private static AndroidJavaClass NeftaPluginClass {
            get
            {
                if (_neftaPluginClass == null)
                {
                    _neftaPluginClass = new AndroidJavaClass("com.nefta.sdk.NeftaPlugin");
                }
                return _neftaPluginClass;
            }
        }
        private static AndroidJavaObject _plugin;
#endif

        private static List<InsightRequest> _insightRequests;
        private static int _insightId;
        
        public static Action<string[]> OnReady;

        public static void EnableLogging(bool enable)
        {
#if UNITY_EDITOR
            NeftaPlugin.EnableLogging(enable);
#elif UNITY_IOS
            NeftaPlugin_EnableLogging(enable);
#elif UNITY_ANDROID
            NeftaPluginClass.CallStatic("EnableLogging", enable);
#endif
        }

        public static void Init(string appId, bool simulateAdsInEditor=false)
        {
#if UNITY_EDITOR
            _plugin = NeftaPlugin.Init(appId, simulateAdsInEditor);
            _plugin._adapterListener = new NeftaAdapterListener();
#elif UNITY_IOS
            NeftaPlugin_Init(appId, OnInsights);
#elif UNITY_ANDROID
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            _plugin = NeftaPluginClass.CallStatic<AndroidJavaObject>("Init", unityActivity, appId, new NeftaAdapterListener());
#endif
            _insightRequests = new List<InsightRequest>();
        }

        public static void Record(GameEvent gameEvent)
        {
            var type = gameEvent._eventType;
            var category = gameEvent._category;
            var subCategory = gameEvent._subCategory;
            var value = gameEvent._value;
            Record(type, category, subCategory, gameEvent._name, value, gameEvent._customString);
        }

        internal static void Record(int type, int category, int subCategory, string name, long value, string customPayload)
        {
#if UNITY_EDITOR
            _plugin.Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_IOS
            NeftaPlugin_Record(type, category, subCategory, name, value, customPayload);
#elif UNITY_ANDROID
            _plugin.Call("Record", type, category, subCategory, name, value, customPayload);
#endif
        }

        public static void OnExternalMediationRequest(AdInsight insight, AdRequest request, string adUnitId)
        {
            var adOpportunityId = insight._adOpportunityId;
            OnExternalMediationRequest(insight._type, request, adUnitId, adOpportunityId);
        }


        public static void OnExternalMediationRequest(AdType adType, AdRequest request, string adUnitId)
        {
            OnExternalMediationRequest(adType, request, adUnitId, -1);
        }

        private static void OnExternalMediationRequest(AdType adType, AdRequest request, string adUnitId, int adOpportunityId)
        {
            var id = request.GetHashCode().ToString();
            AMRequest amRequest = null;
            foreach (var amr in _amRequests)
            {
                if (amr._adUnitId == adUnitId && amr._hasImpression)
                {
                    amRequest = amr;
                    amRequest._id0 = id;
                    amRequest._id2 = null;
                    amRequest._hasImpression = false;
                    break;
                }
            }
            if (amRequest == null)
            {
                _amRequests.Add(new AMRequest() { _adUnitId = adUnitId, _id0 = id, _id2 = null });   
            }
            double requestedFloorPrice = -1;
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("google-admob", (int)adType, id, adUnitId, requestedFloorPrice, adOpportunityId);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest("google-admob", (int)adType, id, adUnitId, requestedFloorPrice, adOpportunityId);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "google-admob", (int)adType, id, adUnitId, requestedFloorPrice, adOpportunityId);
#endif
        }
        
        /// <summary>
        /// Should be called when AdMob loads a rewarded ad (RewardedAd.OnAdLoadedEvent)
        /// </summary>
        /// <param name="banner">Google AdMob InterstitialAd that was loaded</param>
        /// <param name="request">Google AdMob request that initiated this response and was also used in corresponding OnExternalMediationRequest call</param>
        public static void OnExternalMediationRequestLoaded(BannerView banner, AdRequest request)
        {
            var id0 = request.GetHashCode().ToString();
            var id2 = banner.GetHashCode().ToString();
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, null);
        }
        
        /// <summary>
        /// Should be called when AdMob loads a rewarded ad (RewardedAd.OnAdLoadedEvent)
        /// </summary>
        /// <param name="interstitial">Google AdMob InterstitialAd that was loaded</param>
        /// <param name="request">Google AdMob request that initiated this response and was also used in corresponding OnExternalMediationRequest call</param>
        public static void OnExternalMediationRequestLoaded(InterstitialAd interstitial, AdRequest request)
        {
            var id0 = request.GetHashCode().ToString();
            var id2 = interstitial.GetHashCode().ToString();
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, null);
        }
        
        /// <summary>
        /// Should be called when AdMob loads a rewarded ad (RewardedAd.OnAdLoadedEvent)
        /// </summary>
        /// <param name="rewarded">Google AdMob InterstitialAd that was loaded</param>
        /// <param name="request">Google AdMob request that initiated this response and was also used in corresponding OnExternalMediationRequest call</param>
        public static void OnExternalMediationRequestLoaded(RewardedAd rewarded, AdRequest request)
        {
            var id0 = request.GetHashCode().ToString();
            var id2 = rewarded.GetHashCode().ToString();
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, null);
        }

        /// <summary>
        /// Should be called when AdMob fails to load an ad ([AdMob AdType wrapper].OnBannerAdLoadFailed)
        /// </summary>
        /// <param name="request">Google AdMob AdRequest that failed and was also used in corresponding OnExternalMediationRequest call</param>
        /// <param name="error">Load fail reason</param>
        public static void OnExternalMediationRequestFailed(AdRequest request, LoadAdError error)
        {
            var errorCode = error.GetCode();
            var status = 0;
#if UNITY_IOS
            if (errorCode == 1)
#else
            if (errorCode == 3)
#endif
            {
                status = 2;
            }

            var providerStatus = errorCode.ToString(CultureInfo.CurrentCulture);
            String networkStatus = null;
            try
            {
                var responseInfo = error.GetResponseInfo();
                if (responseInfo != null)
                {
                    var adapterResponse = responseInfo.GetLoadedAdapterResponseInfo();
                    if (adapterResponse != null)
                    {
                        var adapterError = adapterResponse.AdError;
                        if (adapterError != null)
                        {
                            networkStatus = adapterError.GetCode().ToString(CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            var id = request.GetHashCode().ToString();
            for (var i = _amRequests.Count - 1; i >= 0; i--)
            {
                if (_amRequests[i]._id0 == id)
                {
                    _amRequests.RemoveAt(i);
                    break;
                }
            }
            OnExternalMediationResponse(id, null, -1, null, status, providerStatus, networkStatus);
        }

        private static void OnExternalMediationResponse(string id, string id2, double revenue, string precision, int status, string providerStatus, string networkStatus)
        {
            if (id2 != null)
            {
                for (var i = _amRequests.Count - 1; i >= 0; i--)
                {
                    if (_amRequests[i]._id0 == id)
                    {
                        _amRequests[i]._id2 = id2;
                        break;
                    }
                }
            }
#if UNITY_EDITOR
            _plugin.OnExternalMediationResponse("google-admob", id, id2, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationResponse("google-admob", id, id2, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationResponse", "google-admob", id, id2, revenue, precision, status, providerStatus, networkStatus);
#endif
        }

        public static void OnExternalMediationImpression(BannerView banner, AdValue adValue)
        {
            var id2 = banner.GetHashCode().ToString();
            OnExternalMediationImpression(false, AdType.Banner, id2, banner.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(InterstitialAd interstitial, AdValue adValue)
        {
            var id2 = interstitial.GetHashCode().ToString();
            OnExternalMediationImpression(false, AdType.Interstitial, id2, interstitial.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(RewardedAd rewarded, AdValue adValue)
        {
            var id2 = rewarded.GetHashCode().ToString();
            OnExternalMediationImpression(false, AdType.Rewarded, id2, rewarded.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationClick(BannerView banner)
        {
            var id2 = banner.GetHashCode().ToString();
            OnExternalMediationImpression(true, AdType.Banner, id2, null, null);
        }
        
        public static void OnExternalMediationClick(InterstitialAd interstitial)
        {
            var id2 = interstitial.GetHashCode().ToString();
            OnExternalMediationImpression(true, AdType.Interstitial, id2, null, null);
        }
        
        public static void OnExternalMediationClick(RewardedAd rewarded)
        {
            var id2 = rewarded.GetHashCode().ToString();
            OnExternalMediationImpression(true, AdType.Rewarded, id2, null, null);
        }

        private static void OnExternalMediationImpression(bool isClick, AdType adType, string id2, ResponseInfo responseInfo, AdValue adValue)
        {
            string internalAdType = null;
            if (adType == AdType.Banner)
            {
                internalAdType = "banner";
            }
            else if (adType == AdType.Interstitial)
            {
                internalAdType = "interstitial";
            }
            else if (adType == AdType.Rewarded)
            {
                internalAdType = "rewarded";
            }

            string adUnitId = null;
            for (var i = _amRequests.Count - 1; i >= 0; i--)
            {
                if (_amRequests[i]._id2 == id2)
                {
                    adUnitId = _amRequests[i]._adUnitId;
                    _amRequests[i]._hasImpression = true;
                    break;
                }
            }
            
            var sb = new StringBuilder();
            sb.Append("{\"ad_unit_id\":\"");
            sb.Append(adUnitId);
            sb.Append("\",\"ad_type\":\"");
            sb.Append(internalAdType);
            sb.Append("\"");
            
            if (adValue != null)
            {
                sb.Append(",\"currency_code\":\"");
                sb.Append(adValue.CurrencyCode);
                sb.Append("\",\"value\":");
                sb.Append((adValue.Value / 1000000.0).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"precision\":");
                sb.Append((int)adValue.Precision);
            }
            
            if (responseInfo != null)
            {
                var placement = responseInfo.GetResponseExtras()["mediation_group_name"];
                if (!string.IsNullOrEmpty(placement))
                {
                    sb.Append(",\"placement\":\"");
                    sb.Append(placement);
                    sb.Append("\"");
                }
                var others = responseInfo.GetAdapterResponses();
                if (others != null && others.Count > 0)
                {
                    sb.Append(",\"waterfall\":[\"");
                    for (var i = 0; i < others.Count; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append("\",\"");
                        }
                        sb.Append(others[i].AdSourceInstanceName);
                    }
                    sb.Append("\"]");
                }
            }
            var data = sb.ToString();

#if UNITY_EDITOR
            _plugin.OnExternalMediationImpressionAsString(isClick, "google-admob", data, null, id2);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationImpressionAsString(isClick, "google-admob", data, null, id2);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationImpressionAsString", isClick, "google-admob", data, null, id2);
#endif
        }

        public static void GetInsights(int insights, AdInsight previousInsight, OnInsightsCallback callback, int timeoutInSeconds=0)
        {
            var id = 0;
            var previousAdOpportunityId = -1;
            if (previousInsight != null)
            {
                previousAdOpportunityId = previousInsight._adOpportunityId;
            }
            lock (_insightRequests)
            {
                id = _insightId;
                var request = new InsightRequest(id, callback);
                _insightRequests.Add(request);
                _insightId++;
            }
            
#if UNITY_EDITOR
            _plugin.GetInsights(id, insights, previousAdOpportunityId, timeoutInSeconds);
#elif UNITY_IOS
            NeftaPlugin_GetInsights(id, insights, previousAdOpportunityId, timeoutInSeconds);
#elif UNITY_ANDROID
            _plugin.Call("GetInsightsBridge", id, insights, previousAdOpportunityId, timeoutInSeconds);
#endif
        }
        
        public static string GetNuid(bool present)
        {
            string nuid = null;
#if UNITY_EDITOR
            nuid = _plugin.GetNuid(present);
#elif UNITY_IOS
            nuid = NeftaPlugin_GetNuid(present);
#elif UNITY_ANDROID
            nuid = _plugin.Call<string>("GetNuid", present);
#endif
            return nuid;
        }
        
        public static void SetExtraParameter(string key, string value)
        {
#if UNITY_EDITOR
            NeftaPlugin.SetExtraParameter(key, value);
#elif UNITY_IOS
            NeftaPlugin_SetExtraParameter(key, value);
#elif UNITY_ANDROID
            NeftaPluginClass.CallStatic("SetExtraParameter", key, value);
#endif
        }
        
        public static void SetContentRating(ContentRating rating)
        {
            var r = "";
            switch (rating)
            {
                case ContentRating.General:
                    r = "G";
                    break;
                case ContentRating.ParentalGuidance:
                    r = "PG";
                    break;
                case ContentRating.Teen:
                    r = "T";
                    break;
                case ContentRating.MatureAudience:
                    r = "MA";
                    break;
            }
#if UNITY_EDITOR
            _plugin.SetContentRating(r);
#elif UNITY_IOS
            NeftaPlugin_SetContentRating(r);
#elif UNITY_ANDROID
            _plugin.Call("SetContentRating", r);
#endif
        }
        
        public static void SetOverride(string root) {
#if UNITY_EDITOR
            NeftaPlugin.SetOverride(root);
#elif UNITY_IOS
            NeftaPlugin_SetOverride(root);
#elif UNITY_ANDROID
            _neftaPluginClass.CallStatic("SetOverride", root);
#endif
        }
        
        internal static void IOnReady(string response)
        {
            if (OnReady != null)
            {
                string[] adUnits = null;
                if (response != null)
                {
                    adUnits = response.Split(',');
                }
                OnReady.Invoke(adUnits);
            }
        }
        
        internal static void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            var insights = new Insights();
            if (adapterResponseType == Insights.Churn)
            {
                insights._churn = new Churn(JsonUtility.FromJson<ChurnDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Banner)
            {
                insights._banner = new AdInsight(AdType.Banner, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Interstitial)
            {
                insights._interstitial = new AdInsight(AdType.Interstitial, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            else if (adapterResponseType == Insights.Rewarded)
            {
                insights._rewarded = new AdInsight(AdType.Rewarded, JsonUtility.FromJson<AdConfigurationDto>(adapterResponse));
            }
            
            try
            {
                lock (_insightRequests)
                {
                    for (var i = _insightRequests.Count - 1; i >= 0; i--)
                    {
                        var insightRequest = _insightRequests[i];
                        if (insightRequest._id == id)
                        {
                            insightRequest._returnContext.Post(_ => insightRequest._callback(insights), null);
                            _insightRequests.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}