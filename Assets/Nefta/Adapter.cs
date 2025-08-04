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

#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnInsightsDelegate(int requestId, string insights);

        [MonoPInvokeCallback(typeof(OnInsightsDelegate))] 
        private static void OnInsights(int requestId, string insights) {
            IOnInsights(requestId, insights);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnInsightsDelegate onInsights);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string mediationProvider, int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus);

        [DllImport ("__Internal")]
        private static extern void NeftaAdapter_OnExternalMediationImpressionAsString(int adType, string network, string data, double revenue, string precision);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetInsights(int requestId, int insights, int timeoutInSeconds);
        
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
        private static AndroidJavaClass _adapter;
#endif

        private static List<InsightRequest> _insightRequests;
        private static int _insightId;

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

        public static void Init(string appId)
        {
#if UNITY_EDITOR
            var pluginGameObject = new GameObject("_NeftaPlugin");
            UnityEngine.Object.DontDestroyOnLoad(pluginGameObject);
            _plugin = NeftaPlugin.Init(pluginGameObject, appId);
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

        /// <summary>
        /// Should be called when AdMob loads an banner ad (BannerView.OnBannerAdLoaded)
        /// </summary>
        /// <param name="usedInsight">Insight used was considered for this request"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="banner">Google AdMob BannerView instance</param>
        public static void OnExternalMediationRequestLoaded(AdInsight usedInsight, string adUnitId, BannerView banner)
        {
            OnExternalMediationRequest(AdType.Banner, usedInsight, -1, adUnitId, -1, null, 1, null, null);
        }
        
        /// <summary>
        /// Should be called when AdMob loads an interstitial ad (InterstitialAd.OnAdLoadedEvent)
        /// </summary>
        /// <param name="usedInsight">Insight used was considered for this request"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="interstitial">Google AdMob InterstitialAd instance</param>
        public static void OnExternalMediationRequestLoaded(AdInsight usedInsight, string adUnitId, InterstitialAd interstitial)
        {
            OnExternalMediationRequest(AdType.Interstitial, usedInsight, -1, adUnitId, -1, null, 1, null, null);
        }
        
        /// <summary>
        /// Should be called when AdMob loads a rewarded ad (RewardedAd.OnAdLoadedEvent)
        /// </summary>
        /// <param name="usedInsight">Insight used was considered for this request"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="rewarded">Google AdMob RewardedAd instance</param>
        public static void OnExternalMediationRequestLoaded(AdInsight usedInsight, string adUnitId, RewardedAd rewarded)
        {
            OnExternalMediationRequest(AdType.Rewarded, usedInsight, -1, adUnitId, -1, null, 1, null, null);
        }

        /// <summary>
        /// Should be called when AdMob fails to load an ad ([AdMob AdType wrapper].OnBannerAdLoadFailed)
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="usedInsight">Insight used was considered for this request"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="error">Load fail reason</param>
        public static void OnExternalMediationRequestFailed(AdType adType, AdInsight usedInsight, string adUnitId,  LoadAdError error)
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
            catch (Exception e)
            {
                // ignored
            }

            OnExternalMediationRequest(adType, usedInsight, -1, adUnitId, -1, null, status, providerStatus, networkStatus);
        }

        private static void OnExternalMediationRequest(AdType adType, AdInsight insight, double requestedFloorPrice, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus)
        {
            string recommendedAdUnitId = null;
            double calculatedFloorPrice = 0;
            if (insight != null)
            {
                recommendedAdUnitId = insight._adUnit;
                calculatedFloorPrice = insight._floorPrice;
                if (insight._type != adType)
                {
                    Debug.LogWarning($"OnExternalMediationRequest reported adType: {adType} doesn't match insight adType: {insight._type}");
                }
            }
            
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("google-admob", (int)adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest("google-admob", (int)adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "google-admob", (int)adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#endif
        }

        public static void OnExternalMediationImpression(string adUnitId, BannerView banner, AdValue adValue)
        {
            OnExternalMediationImpression(AdType.Banner, adUnitId, banner.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(string adUnitId, InterstitialAd interstitial, AdValue adValue)
        {
            OnExternalMediationImpression(AdType.Interstitial, adUnitId, interstitial.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(string adUnitId, RewardedAd rewarded, AdValue adValue)
        {
            OnExternalMediationImpression(AdType.Rewarded, adUnitId, rewarded.GetResponseInfo(), adValue);
        }

        private static void OnExternalMediationImpression(AdType adType, String adUnitId, ResponseInfo responseInfo, AdValue adValue)
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

            String network = null;
            var revenue = adValue.Value / 1000000.0;
            var precision = ((int)adValue.Precision).ToString();
            var sb = new StringBuilder();
            sb.Append("{\"ad_unit_id\":\"");
            sb.Append(adUnitId);
            sb.Append("\",\"ad_type\":\"");
            sb.Append(internalAdType);
            sb.Append("\",\"currency_code\":\"");
            sb.Append(adValue.CurrencyCode);
            sb.Append("\"");
            
            if (responseInfo != null)
            {
                var placement = responseInfo.GetResponseExtras()["mediation_group_name"];
                if (!string.IsNullOrEmpty(placement))
                {
                    sb.Append(",\"placement\":\"");
                    sb.Append(placement);
                    sb.Append("\"");
                }
                
                var adapterResponse = responseInfo.GetLoadedAdapterResponseInfo();
                if (adapterResponse != null)
                {
                    network = adapterResponse.AdSourceInstanceName;
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
            _plugin.OnExternalMediationImpression(data);
#elif UNITY_IOS
             NeftaAdapter_OnExternalMediationImpressionAsString((int)adType, network, data, revenue, precision);
#elif UNITY_ANDROID
            if (_adapter == null) {
                _adapter = new AndroidJavaClass("com.google.ads.mediation.nefta.NeftaAdapter");
            }
            _adapter.CallStatic("OnExternalMediationImpressionAsString", (int)adType, network, data, revenue, precision);
#endif
        }

        public static void GetInsights(int insights, OnInsightsCallback callback, int timeoutInSeconds=0)
        {
            var id = 0;
            lock (_insightRequests)
            {
                id = _insightId;
                var request = new InsightRequest(id, callback);
                _insightRequests.Add(request);
                _insightId++;
            }
            
#if UNITY_EDITOR
            _plugin.GetInsights(id, insights, timeoutInSeconds);
#elif UNITY_IOS
            NeftaPlugin_GetInsights(id, insights, timeoutInSeconds);
#elif UNITY_ANDROID
            _plugin.Call("GetInsightsBridge", id, insights, timeoutInSeconds);
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
        
        internal static void IOnInsights(int id, string bi)
        {
            var insights = new Insights(JsonUtility.FromJson<InsightsDto>(bi));
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