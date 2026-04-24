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
        
        [Serializable]
        public class InitConfigurationDto
        {
            public bool skipOptimization;
            public string nuid;
            public float[] delays;
            public int noDynamicResponseRetryInMs;
            public int noDefaultResponseRetryInMs;
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

        private const string _mediationProvider = "google-admob";
        
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
        private delegate void OnReadyDelegate(string initConfig);
        private delegate void OnInsightsDelegate(int requestId, int adapterResponseType, string adapterResponse);
        private delegate void OnNewSessionCallbackDelegate();

        [MonoPInvokeCallback(typeof(OnReadyDelegate))] 
        private static void OnReadyBridge(string initConfig) {
            IOnReady(initConfig);
        }

        [MonoPInvokeCallback(typeof(OnInsightsDelegate))] 
        private static void OnInsightsBridge(int requestId, int adapterResponseType, string adapterResponse) {
            IOnInsights(requestId, adapterResponseType, adapterResponse);
        }

        [MonoPInvokeCallback(typeof(OnNewSessionCallbackDelegate))] 
        private static void OnNewSessionCallbackDelegateBridge() {
            IOnNewSessionCallback();
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetExtraParameter(string key, string value);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, string clientId, OnReadyDelegate onReady, OnInsightsDelegate onInsights);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationRequest(string provider, int adType, string id, string requestedAdUnitId, double requestedFloorPrice, int adOpportunityId);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationResponseAsString(string provider, string id, string id2, double revenue, string precision, int status, string providerStatus, string networkStatus, string baseData);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_OnExternalMediationImpressionAsString(bool isClick, string provider, string data, string id, string id2);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetInsights(int requestId, int insights, int previousRequestId);
        
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
        private static List<float> _delays;

        private static SynchronizationContext _mainContext;
        private static Action<InitConfiguration> _onReady;
        private static List<Action> _newSessionCallbacks;

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

        public static void InitWithAppId(string appId, Action<InitConfiguration> onReady)
        {
            Init(appId, null, onReady);
        }

        public static void InitWithClientId(string clientId, Action<InitConfiguration> onReady)
        {
            Init(null, clientId, onReady);
        }
        
        private static void Init(string appId, string clientId, Action<InitConfiguration> onReady)
        {
            _mainContext = SynchronizationContext.Current;
            _onReady = onReady;
            _newSessionCallbacks = new List<Action>();
#if UNITY_EDITOR
            _plugin = NeftaPlugin.Init(appId, clientId, "unity-google-admob", "/");
            _plugin.Listener = new NeftaListener();
#elif UNITY_IOS
            NeftaPlugin_Init(appId, clientId, OnReadyBridge, OnInsightsBridge);
#elif UNITY_ANDROID
            AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");

            _plugin = NeftaPluginClass.CallStatic<AndroidJavaObject>("UnityInit", unityActivity, appId, clientId, new NeftaListener(), "unity-google-admob", "/");
#endif
            _insightRequests = new List<InsightRequest>();
            _delays = new List<float>() { 2 };
        }

        public static void Record(GameEvent gameEvent)
        {
            var type = gameEvent._eventType;
            var category = gameEvent._category;
            var subCategory = gameEvent._subCategory;
            var name = gameEvent._name;
            var value = gameEvent._value;
            var customString = gameEvent._customString;
            Record(type, category, subCategory, name, value, customString);
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

        public static void OnExternalMediationRequest(AdInsight insight, AdRequest request, string adUnitId, double customBidFloor=-1)
        {
            if (customBidFloor < 0)
            {
                customBidFloor = insight._floorPrice;
            }
            OnExternalMediationRequest(insight._type, request, adUnitId, customBidFloor, insight._requestId);
        }

        public static void OnExternalMediationRequest(AdType adType, AdRequest request, string adUnitId, double requestedFloorPrice=-1)
        {
            OnExternalMediationRequest(adType, request, adUnitId, requestedFloorPrice, -1);
        }
        
        private static void OnExternalMediationRequest(AdType adType, AdRequest request, string adUnitId, double requestedFloorPrice, int requestId)
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
#if UNITY_EDITOR
            _plugin.OnExternalMediationRequest(_mediationProvider, (int)adType, id, adUnitId, requestedFloorPrice, requestId);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationRequest(_mediationProvider, (int)adType, id, adUnitId, requestedFloorPrice, requestId);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationRequest", _mediationProvider, (int)adType, id, adUnitId, requestedFloorPrice, requestId);
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
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, banner.GetResponseInfo());
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
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, interstitial.GetResponseInfo());
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
            OnExternalMediationResponse(id0, id2, -1, null, 1, null, rewarded.GetResponseInfo());
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
            
            var id = request.GetHashCode().ToString();
            for (var i = _amRequests.Count - 1; i >= 0; i--)
            {
                if (_amRequests[i]._id0 == id)
                {
                    _amRequests.RemoveAt(i);
                    break;
                }
            }
            OnExternalMediationResponse(id, null, -1, null, status, providerStatus, error.GetResponseInfo());
        }

        private static void OnExternalMediationResponse(string id, string id2, double revenue, string precision, int status, string providerStatus, ResponseInfo responseInfo)
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
            
            var data = new StringBuilder();
            data.Append('{');
            var networkStatus = TryParseResponseInfo(data, responseInfo);
            data.Append('}');
            
#if UNITY_EDITOR
            _plugin.OnExternalMediationResponseAsString(_mediationProvider, id, id2, revenue, precision, status, providerStatus, networkStatus, data.ToString());
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationResponseAsString(_mediationProvider, id, id2, revenue, precision, status, providerStatus, networkStatus, data.ToString());
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationResponseAsString", _mediationProvider, id, id2, revenue, precision, status, providerStatus, networkStatus, data.ToString());
#endif
        }

        public static void OnExternalMediationImpression(BannerView banner, AdValue adValue)
        {
            var id2 = banner.GetHashCode().ToString();
            OnExternalMediationImpression(false, id2, banner.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(InterstitialAd interstitial, AdValue adValue)
        {
            var id2 = interstitial.GetHashCode().ToString();
            OnExternalMediationImpression(false, id2, interstitial.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationImpression(RewardedAd rewarded, AdValue adValue)
        {
            var id2 = rewarded.GetHashCode().ToString();
            OnExternalMediationImpression(false, id2, rewarded.GetResponseInfo(), adValue);
        }
        
        public static void OnExternalMediationClick(BannerView banner)
        {
            var id2 = banner.GetHashCode().ToString();
            OnExternalMediationImpression(true, id2, null, null);
        }
        
        public static void OnExternalMediationClick(InterstitialAd interstitial)
        {
            var id2 = interstitial.GetHashCode().ToString();
            OnExternalMediationImpression(true, id2, null, null);
        }
        
        public static void OnExternalMediationClick(RewardedAd rewarded)
        {
            var id2 = rewarded.GetHashCode().ToString();
            OnExternalMediationImpression(true, id2, null, null);
        }

        private static void OnExternalMediationImpression(bool isClick, string id2, ResponseInfo responseInfo, AdValue adValue)
        {
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
            TryParseResponseInfo(sb, responseInfo);
            sb.Append('}');
            var data = sb.ToString();

#if UNITY_EDITOR
            _plugin.OnExternalMediationImpressionAsString(isClick, _mediationProvider, data, null, id2);
#elif UNITY_IOS
            NeftaPlugin_OnExternalMediationImpressionAsString(isClick, _mediationProvider, data, null, id2);
#elif UNITY_ANDROID
            _plugin.CallStatic("OnExternalMediationImpressionAsString", isClick, _mediationProvider, data, null, id2);
#endif
        }

        private static string TryParseResponseInfo(StringBuilder sb, ResponseInfo responseInfo)
        {
            if (responseInfo != null)
            {
                var extras = responseInfo.GetResponseExtras();
                if (extras != null && extras.TryGetValue("mediation_group_name", out var placement))
                {
                    if (!string.IsNullOrEmpty(placement))
                    {
                        if (sb.Length > 1)
                        {
                            sb.Append(",");
                        }
                        sb.Append("\"placement\":\"");
                        sb.Append(JavaScriptStringEncode(placement));
                        sb.Append("\"");
                    }
                }

                var others = responseInfo.GetAdapterResponses();
                if (others != null && others.Count > 0)
                {
                    if (sb.Length > 1)
                    {
                        sb.Append(",");
                    }
                    sb.Append("\"waterfall\":[\"");
                    for (var i = 0; i < others.Count; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append("\",\"");
                        }
                        sb.Append(JavaScriptStringEncode(others[i].AdSourceInstanceName));
                    }
                    sb.Append("\"]");
                }
                var loadedAdapter = responseInfo.GetLoadedAdapterResponseInfo();
                if (loadedAdapter != null)
                {
                    var networkName = loadedAdapter.AdSourceName;
                    if (networkName != null)
                    {
                        if (sb.Length > 1)
                        {
                            sb.Append(",");
                        }
                        sb.Append("\"network_name\":\"");
                        sb.Append(JavaScriptStringEncode(networkName));
                        sb.Append('"');
                    }

                    var adapterError = loadedAdapter.AdError;
                    if (adapterError != null)
                    {
                        return adapterError.GetCode().ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
            return null;
        }
        
        public static void AddNewSessionCallback(Action newSessionCallback)
        {
            _newSessionCallbacks.Add(newSessionCallback);
        }

        public static void RemoveNewSessionCallback(Action newSessionCallback)
        {
            _newSessionCallbacks.Remove(newSessionCallback);
        }

        public static void GetInsights(int insights, AdInsight previousInsight, OnInsightsCallback callback)
        {
            var id = 0;
            var previousRequestId = -1;
            if (previousInsight != null)
            {
                previousRequestId = previousInsight._requestId;
            }
            lock (_insightRequests)
            {
                id = _insightId;
                var request = new InsightRequest(id, callback);
                _insightRequests.Add(request);
                _insightId++;
            }
            
#if UNITY_EDITOR
            _plugin.GetInsights(id, insights, previousRequestId);
#elif UNITY_IOS
            NeftaPlugin_GetInsights(id, insights, previousRequestId);
#elif UNITY_ANDROID
            _plugin.Call("GetInsightsBridge", id, insights, previousRequestId);
#endif
        }
        
        public static float GetRetryDelayInSeconds(AdInsight insight)
        {
            var consecutiveFails = 1;
            if (insight != null) {
                if (insight._delay > 0) {
                    return insight._delay;
                }
                consecutiveFails = insight._auctionId;
            }
            var delayIndex = consecutiveFails - 1;
            if (delayIndex < 0) {
                delayIndex = 0;
            } else if (delayIndex >= _delays.Count) {
                delayIndex = _delays.Count -1;
            }
            return _delays[delayIndex];
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
        
        public static void SetOverride(string root) {
#if UNITY_EDITOR
            NeftaPlugin.SetOverride(root);
#elif UNITY_IOS
            NeftaPlugin_SetOverride(root);
#elif UNITY_ANDROID
            _neftaPluginClass.CallStatic("SetOverride", root);
#endif
        }
        
        internal static void IOnReady(string initConfig)
        {
            _mainContext.Post(_ =>
            {
                InitConfigurationDto initDto = null;
                try
                {
                    initDto = JsonUtility.FromJson<InitConfigurationDto>(initConfig);
                }
                catch (Exception e)
                {
                    Debug.Log("IOnReady error: " + e.Message);
                }
                
                _delays.Clear();
                if (initDto != null)
                {
                    if (initDto.delays != null)
                    {
                        foreach (var delay in initDto.delays)
                        {
                            _delays.Add(delay);
                        }
                    }
                }
                if (_delays.Count == 0)
                {
                    _delays.Add(2);
                }
                
                if (_onReady != null)
                {
                    _onReady.Invoke(new InitConfiguration(initDto));
                }
            }, null);
        }
        
        internal static void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            lock (_insightRequests)
            {
                for (var i = _insightRequests.Count - 1; i >= 0; i--)
                {
                    var insightRequest = _insightRequests[i];
                    if (insightRequest._id == id)
                    {
                        var insights = new Insights(adapterResponseType, adapterResponse);
                        insightRequest._returnContext.Post(_ => insightRequest._callback(insights), null);
                        _insightRequests.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        
        internal static void IOnNewSessionCallback()
        {
            foreach (var newSessionCallback in _newSessionCallbacks)
            {
                newSessionCallback.Invoke();
            }
        }
        
        internal static string JavaScriptStringEncode(string value)
        {
            if (value == null)
            {
                return null;
            }
            var len = value.Length;
            var needEncode = false;
            char c;
            for (var i = 0; i < len; i++)
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
            
            var sb = new StringBuilder();
            for (var i = 0; i < len; i++)
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