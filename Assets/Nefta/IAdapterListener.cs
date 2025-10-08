#if !UNITY_EDITOR
namespace Nefta
{
    public interface IAdapterListener
    {
        void IOnReady(string adUnits);
        void IOnInsights(int id, int adapterResponseType, string adapterResponse);
    }
}
#endif