using UnityEngine;
#if UNITY_EDITOR
using Nefta.Editor;
#endif

namespace Nefta
{
    public class NeftaListener : AndroidJavaProxy, IPluginListener
    {
        public NeftaListener() : base("com.nefta.sdk.AdapterCallback")
        {
        }

        public void IOnReady(string adUnits)
        {
            Adapter.IOnReady(adUnits);
        }

        public void IOnInsights(int id, int adapterResponseType, string insights)
        {
            Adapter.IOnInsights(id, adapterResponseType, insights);
        }
        
        public void IOnNewSessionCallback()
        {
            Adapter.IOnNewSessionCallback();
        }
    }
}