#if UNITY_EDITOR
using Nefta.Editor;
#elif UNITY_IOS
using System;
using System.Runtime.InteropServices;
using AOT;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Nefta.Events;
using UnityEngine;

namespace Nefta
{
    public class Adapter
    {
        public delegate void OnBehaviourInsightCallback(Dictionary<string, Insight> insights);
        
        public enum ContentRating
        {
            Unspecified = 0,
            General = 1,
            ParentalGuidance = 2,
            Teen = 3,
            MatureAudience = 4
        }
        
#if UNITY_EDITOR
        private static NeftaPlugin _plugin;
#elif UNITY_IOS
        private delegate void OnBehaviourInsightDelegate(string behaviourInsight);

        [MonoPInvokeCallback(typeof(OnBehaviourInsightDelegate))] 
        private static void OnBehaviourInsight(string behaviourInsight) {
            IOnBehaviourInsight(behaviourInsight);
        }

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_EnableLogging(bool enable);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Init(string appId, OnBehaviourInsightDelegate onBehaviourInsight);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_Record(int type, int category, int subCategory, string nameValue, long value, string customPayload);

        [DllImport ("__Internal")]
        private static extern string NeftaPlugin_GetNuid(bool present);

        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_SetContentRating(string rating);
        
        [DllImport ("__Internal")]
        private static extern void NeftaPlugin_GetBehaviourInsight(string insights);
#elif UNITY_ANDROID
        private static AndroidJavaObject _plugin;
#endif
        
        private static IEnumerable<String> _scheduledBehaviourInsight;
        private static SynchronizationContext _threadContext;
        
        public static OnBehaviourInsightCallback BehaviourInsightCallback;
        
        public static void EnableLogging(bool enable)
        {
#if UNITY_EDITOR
            NeftaPlugin.EnableLogging(enable);
#elif UNITY_IOS
            NeftaPlugin_EnableLogging(enable);
#elif UNITY_ANDROID
            using (AndroidJavaClass neftaPlugin = new AndroidJavaClass("com.nefta.sdk.NeftaPlugin"))
            {
                neftaPlugin.CallStatic("EnableLogging", enable);
            }
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

            AndroidJavaClass neftaPluginClass = new AndroidJavaClass("com.nefta.sdk.NeftaPlugin");
            _plugin = neftaPluginClass.CallStatic<AndroidJavaObject>("Init", unityActivity, appId, new NeftaAdapterListener());
#endif
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
        
        public static void GetBehaviourInsight(string[] insightList)
        {
            if (_scheduledBehaviourInsight != null)
            {
                return;
            }
            _scheduledBehaviourInsight = insightList;
            _threadContext = SynchronizationContext.Current;
            
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
            _plugin.GetBehaviourInsight(insights);
#elif UNITY_IOS
            NeftaPlugin_GetBehaviourInsight(insights);
#elif UNITY_ANDROID
            _plugin.Call("GetBehaviourInsightWithString", insights);
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
        
        internal static void IOnBehaviourInsight(string bi)
        {
            var behaviourInsight = new Dictionary<string, Insight>();
            try
            {
                var start = bi.IndexOf("s\":", StringComparison.InvariantCulture) + 5;

                while (start != -1 && start < bi.Length)
                {
                    var end = bi.IndexOf("\":{", start, StringComparison.InvariantCulture);
                    var key = bi.Substring(start, end - start);
                    string status = null;
                    long intVal = 0;
                    double floatVal = 0;
                    string stringVal = null;

                    start = end + 4;
                    for (var f = 0; f < 4; f++)
                    {
                        if (bi[start] == 's' && bi[start + 2] == 'a')
                        {
                            start += 9;
                            end = bi.IndexOf("\"", start, StringComparison.InvariantCulture);
                            status = bi.Substring(start, end - start);
                            end++;
                        }
                        else if (bi[start] == 'f')
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
                        else if (bi[start] == 's')
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

                    behaviourInsight[key] = new Insight(status, intVal, floatVal, stringVal);

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
            foreach (var insightName in _scheduledBehaviourInsight)
            {
                if (!behaviourInsight.ContainsKey(insightName))
                {
                    behaviourInsight.Add(insightName, new Insight("Error retrieving key", 0, 0, null));
                }
            }
            _threadContext.Post(_ => BehaviourInsightCallback(behaviourInsight), null);
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