#if !UNITY_EDITOR
namespace Nefta
{
    public interface IAdapterListener
    {
        void IOnBehaviourInsight(int id, string playerScore);
    }
}
#endif