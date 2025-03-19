#if !UNITY_EDITOR
namespace Nefta
{
    public interface IAdapterListener
    {
        void IOnBehaviourInsight(string playerScore);
    }
}
#endif