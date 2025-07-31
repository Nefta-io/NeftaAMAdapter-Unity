#if !UNITY_EDITOR
namespace Nefta
{
    public interface IAdapterListener
    {
        void IOnInsights(int id, string insights);
    }
}
#endif