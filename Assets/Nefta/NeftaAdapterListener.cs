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

        public void IOnBehaviourInsight(int id, string behaviourInsight)
        {
            Adapter.IOnBehaviourInsight(id, behaviourInsight);
        }
    }
}