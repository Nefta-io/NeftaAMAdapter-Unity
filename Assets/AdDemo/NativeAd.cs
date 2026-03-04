using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS
using AOT;
using System.Runtime.InteropServices;
#endif

namespace AdDemo
{
    public class NativeAd : MonoBehaviour
    {
#if UNITY_IOS
        private delegate void OnCallback();
        
        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnShowBridge() { _actions.Enqueue(OnShow); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnClickBridge() { _actions.Enqueue(OnClick); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnRewardBridge() { _actions.Enqueue(OnReward); }

        [MonoPInvokeCallback(typeof(OnCallback))] 
        private static void OnCloseBridge() { _actions.Enqueue(OnClose); }

        [DllImport ("__Internal")]
        private static extern void NetworkConfig_Open(string title, OnCallback onShow, OnCallback onClick, OnCallback onReward, OnCallback onClose);
#elif UNITY_ANDROID
        private class AdCallback : AndroidJavaProxy
        {
            public Action _onShow;
            public Action _onClick;
            public Action _onReward;
            public Action _onClose;
            
            public AdCallback() : base("com.nefta.networkconfig.Callback") { }
        
            public void onShow() { _actions.Enqueue(_onShow); }
            public void onClick() { _actions.Enqueue(_onClick); }
            public void onReward() { _actions.Enqueue(_onReward); }
            public void onClose() { _actions.Enqueue(_onClose); }
        }
#endif
        public static void ShowAd(string title, Action onShow, Action onClick, Action onReward, Action onClose)
        {
            if (_instance == null)
            {
                _instance = new GameObject("SimulatorAd").AddComponent<NativeAd>();
            }
            
            OnShow = onShow;
            OnClick = onClick;
            OnReward = onReward;
            OnClose = onClose;
#if UNITY_EDITOR
            OnShow();
            _ = CloseAfterDelay();
#elif UNITY_IOS
            NetworkConfig_Open(title, OnShowBridge, OnClickBridge, OnRewardBridge, OnCloseBridge);
#elif UNITY_ANDROID
            var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"); 

            var networkConfigClass = new AndroidJavaClass("com.nefta.networkconfig.NetworkConfig");
            networkConfigClass.CallStatic("Open", title, unityActivity, new AdCallback { _onShow = onShow, _onClick = onClick, _onReward = onReward, _onClose = onClose });
#endif
        }

        private static async Task CloseAfterDelay()
        {
            await Task.Delay(100);
            OnClick();
            OnClose();
        }
        
        private static Action OnShow;
        private static Action OnClick;
        private static Action OnReward;
        private static Action OnClose;
        private static ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
        private static NativeAd _instance;

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}