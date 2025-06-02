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
        public delegate void OnBehaviourInsightCallback(Dictionary<string, Insight> insights);

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
            public OnBehaviourInsightCallback _callback;

            public InsightRequest(OnBehaviourInsightCallback callback)
            {
                _id = _insightId;
                _insightId++;
                _returnContext = SynchronizationContext.Current;
                _callback = callback;
            }
        }

#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnBehaviourInsightDelegate(int requestId, string behaviourInsight);

        [MonoPInvokeCallback(typeof(OnBehaviourInsightDelegate))] 
        private static void OnBehaviourInsight(int requestId, string behaviourInsight) {
            IOnBehaviourInsight(requestId, behaviourInsight);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnBehaviourInsightDelegate onBehaviourInsight);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string mediationProvider, int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus);

        [DllImport ("__Internal")]
        private static extern void NeftaAdapter_OnExternalMediationImpressionAsString(int adType, string network, string data);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetBehaviourInsight(int requestId, string insights);
        
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
        private static Dictionary<string, ResponseInfo> _responses = new Dictionary<string, ResponseInfo>();

        public static OnBehaviourInsightCallback BehaviourInsightCallback;

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
            NeftaPlugin_Init(appId, OnBehaviourInsight);
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
            var name = gameEvent._name;
            if (name != null)
            {
                name = JavaScriptStringEncode(gameEvent._name);
            }

            var value = gameEvent._value;
            var customPayload = gameEvent._customString;
            if (customPayload != null)
            {
                customPayload = JavaScriptStringEncode(gameEvent._customString);
            }

            Record(type, category, subCategory, name, value, customPayload);
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
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="recommendedAdUnitId">Recommended adUnitId, retrieved from "recommended_[AdType]_ad_unit_id"</param>
        /// <param name="calculatedFloorPrice">Predicted bid floor, retrieved from "calculated_user_floor_price_[AdType]"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="banner">Google AdMob BannerView instance</param>
        public static void OnExternalMediationRequestLoaded(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, BannerView banner)
        {
            OnAdLoad(adType, recommendedAdUnitId, calculatedFloorPrice, adUnitId, banner.GetResponseInfo());
        }
        
        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="recommendedAdUnitId">Recommended adUnitId, retrieved from "recommended_[AdType]_ad_unit_id"</param>
        /// <param name="calculatedFloorPrice">Predicted bid floor, retrieved from "calculated_user_floor_price_[AdType]"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="interstitial">Google AdMob InterstitialAd instance</param>
        public static void OnExternalMediationRequestLoaded(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, InterstitialAd interstitial)
        {
            OnAdLoad(adType, recommendedAdUnitId, calculatedFloorPrice, adUnitId, interstitial.GetResponseInfo());
        }
        
        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="recommendedAdUnitId">Recommended adUnitId, retrieved from "recommended_[AdType]_ad_unit_id"</param>
        /// <param name="calculatedFloorPrice">Predicted bid floor, retrieved from "calculated_user_floor_price_[AdType]"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="rewarded">Google AdMob RewardedAd instance</param>
        public static void OnExternalMediationRequestLoaded(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, RewardedAd rewarded)
        {
            OnAdLoad(adType, recommendedAdUnitId, calculatedFloorPrice, adUnitId, rewarded.GetResponseInfo());
        }

        private static void OnAdLoad(AdType adType, String recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, ResponseInfo responseInfo)
        {
            _responses[adUnitId] = responseInfo;
            OnExternalMediationRequest((int)adType, recommendedAdUnitId, -1, calculatedFloorPrice, adUnitId, -1, null, 1, null, null);
        }

        /// <summary>
        /// Should be called when MAX loads any ad (MaxSdkCallbacks.[AdType].OnAdLoadedEvent
        /// </summary>
        /// <param name="adType">Ad format of the loaded ad</param>
        /// <param name="recommendedAdUnitId">Recommended adUnitId, retrieved from "recommended_[AdType]_ad_unit_id"</param>
        /// <param name="calculatedFloorPrice">Predicted bid floor, retrieved from "calculated_user_floor_price_[AdType]"</param>
        /// <param name="adUnitId">Google AdMob AdUnit id that was request to load</param>
        /// <param name="error">Load fail reason</param>
        public static void OnExternalMediationRequestFailed(AdType adType, string recommendedAdUnitId, double calculatedFloorPrice, string adUnitId, LoadAdError error)
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

            OnExternalMediationRequest((int)adType, recommendedAdUnitId, -1, calculatedFloorPrice, adUnitId, -1, null, status, providerStatus, networkStatus);
        }

        private static void OnExternalMediationRequest(int adType, string recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, string adUnitId, double revenue, string precision, int status, string providerStatus, string networkStatus)
        {
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest("google-admob", adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest("google-admob", adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", "google-admob", adType, recommendedAdUnitId, requestedFloorPrice, calculatedFloorPrice, adUnitId, revenue, precision, status, providerStatus, networkStatus);
#endif
        }

        public static void OnExternalMediationImpression(AdType adType, String adUnitId, AdValue adValue)
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
            
            var sb = new StringBuilder();
            sb.Append("{\"mediation_provider\":\"google-admob\",\"ad_unit_id\":\"");
            sb.Append(adUnitId);
            sb.Append("\",\"ad_type\":\"");
            sb.Append(internalAdType);
            sb.Append("\",\"currency_code\":\"");
            sb.Append(adValue.CurrencyCode);
            sb.Append("\",\"value\":");
            sb.Append((adValue.Value / 1000000.0).ToString(CultureInfo.InvariantCulture));

            String network = null;
            var responseInfo = _responses[adUnitId];
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
            
            sb.Append(",\"precision\":");
            sb.Append((int)adValue.Precision);
            var data = sb.ToString();
#if UNITY_EDITOR
            _plugin.OnExternalMediationImpression(data);
#elif UNITY_IOS
             NeftaAdapter_OnExternalMediationImpressionAsString((int)adType, network, data);
#elif UNITY_ANDROID
            if (_adapter == null) {
                _adapter = new AndroidJavaClass("com.google.ads.mediation.nefta.NeftaAdapter");
            }
            _adapter.CallStatic("OnExternalMediationImpressionAsString", (int)adType, network, data);
#endif
        }

        public static void GetBehaviourInsight(string[] insightList, OnBehaviourInsightCallback callback=null)
        {
            var request = new InsightRequest(callback ?? BehaviourInsightCallback);
            _insightRequests.Add(request);
            
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var insight in insightList)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append(insight);
            }
            var insights = sb.ToString();
#if UNITY_EDITOR
            _plugin.GetBehaviourInsight(request._id, insights);
#elif UNITY_IOS
            NeftaPlugin_GetBehaviourInsight(request._id, insights);
#elif UNITY_ANDROID
            _plugin.Call("GetBehaviourInsightBridge", request._id, insights);
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
            _plugin.Override(root);
#elif UNITY_IOS
            NeftaPlugin_SetOverride(root);
#elif UNITY_ANDROID
            NeftaPluginClass.CallStatic("SetOverride", root);
#endif
        }
        
        internal static void IOnBehaviourInsight(int id, string bi)
        {
            var behaviourInsight = new Dictionary<string, Insight>();
            if (bi != null)
            {
                try
                {
                    var start = bi.IndexOf("s\":", StringComparison.InvariantCulture) + 5;

                    while (start != -1 && start < bi.Length)
                    {
                        var end = bi.IndexOf("\":{", start, StringComparison.InvariantCulture);
                        var key = bi.Substring(start, end - start);
                        long intVal = 0;
                        double floatVal = 0;
                        string stringVal = null;

                        start = end + 4;
                        for (var f = 0; f < 4; f++)
                        {
                            if (bi[start] == 'f')
                            {
                                start += 11;
                                end = start + 1;
                                for (; end < bi.Length; end++)
                                {
                                    if (bi[end] == ',' || bi[end] == '}')
                                    {
                                        break;
                                    }
                                }

                                var doubleString = bi.Substring(start, end - start);
                                floatVal = Double.Parse(doubleString, NumberStyles.Float, CultureInfo.InvariantCulture);
                            }
                            else if (bi[start] == 'i')
                            {
                                start += 9;
                                end = start + 1;
                                for (; end < bi.Length; end++)
                                {
                                    if (bi[end] == ',' || bi[end] == '}')
                                    {
                                        break;
                                    }
                                }

                                var intString = bi.Substring(start, end - start);
                                intVal = long.Parse(intString, NumberStyles.Number, CultureInfo.InvariantCulture);
                            }
                            else if (bi[start] == 's' && bi[start + 2] == 'r')
                            {
                                start += 13;
                                end = bi.IndexOf("\"", start, StringComparison.InvariantCulture);
                                stringVal = bi.Substring(start, end - start);
                                end++;
                            }

                            if (bi[end] == '}')
                            {
                                break;
                            }

                            start = end + 2;
                        }

                        behaviourInsight[key] = new Insight(intVal, floatVal, stringVal);

                        if (bi[end + 1] == '}')
                        {
                            break;
                        }

                        start = end + 3;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            try
            {
                InsightRequest request = null;
                foreach (var iR in _insightRequests)
                {
                    if (iR._id == id)
                    {
                        request = iR;
                        break;
                    }   
                }
                if (request == null)
                {
                    return;
                }
                
                request._returnContext.Post(_ => request._callback(behaviourInsight), null);

                for (var i = _insightRequests.Count - 1; i >= 0; i--)
                {
                    if (_insightRequests[i]._id == id)
                    {
                        _insightRequests.RemoveAt(i);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        internal static string JavaScriptStringEncode(string value)
        {
            int len = value.Length;
            bool needEncode = false;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = value [i];

                if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
                {
                    needEncode = true;
                    break;
                }
            }

            if (!needEncode)
            {
                return value;
            }
            
            var sb = new StringBuilder ();
            for (int i = 0; i < len; i++)
            {
                c = value [i];
                if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
                {
                    sb.AppendFormat ("\\u{0:x4}", (int)c);
                }
                else switch ((int)c)
                {
                    case 8:
                        sb.Append ("\\b");
                        break;

                    case 9:
                        sb.Append ("\\t");
                        break;

                    case 10:
                        sb.Append ("\\n");
                        break;

                    case 12:
                        sb.Append ("\\f");
                        break;

                    case 13:
                        sb.Append ("\\r");
                        break;

                    case 34:
                        sb.Append ("\\\"");
                        break;

                    case 92:
                        sb.Append ("\\\\");
                        break;

                    default:
                        sb.Append (c);
                        break;
                }
            }
            return sb.ToString ();
        }
    }
}