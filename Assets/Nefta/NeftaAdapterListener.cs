using UnityEngine;
#if UNITY_EDITOR
using Nefta.Editor;
#endif

namespace Nefta
{
    public class NeftaAdapterListener : AndroidJavaProxy, IAdapterListener
    {
        public NeftaAdapterListener() : base("com.nefta.sdk.AdapterCallback")
        {
        }
        
        public void IOnReady(string adUnits)
        {
            Adapter.IOnReady(adUnits);
        }

        public void IOnInsights(int id, int adapterResponseType, string adapterResponse)
        {
            Adapter.IOnInsights(id, adapterResponseType, adapterResponse);
        }
    }
}