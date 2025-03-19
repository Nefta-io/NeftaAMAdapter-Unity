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

        public void IOnBehaviourInsight(string behaviourInsight)
        {
            Adapter.IOnBehaviourInsight(behaviourInsight);
        }
    }
}