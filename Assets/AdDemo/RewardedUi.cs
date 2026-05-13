using UnityEngine;
using UnityEngine.UI;

namespace AdDemo
{
    public class RewardedUi : MonoBehaviour
    {
#if UNITY_IPHONE
        public const string AdUnitA = "ca-app-pub-1193175835908241/1793039068";
        public const string AdUnitB = "ca-app-pub-1193175835908241/9522486356";
#else
        public const string AdUnitA = "ca-app-pub-1193175835908241/2054545908";
        public const string AdUnitB = "ca-app-pub-1193175835908241/8239541882";
#endif
        private IRewarded _logic;
      
        [SerializeField] private Toggle _load;
        [SerializeField] private Text _status;
        [SerializeField] private Button _show;
        
        public bool IsAutoLoad { get; private set; }
        
        public void Init(IRewarded logic)
        {
            _load.onValueChanged.AddListener(OnLoadChanged);
            _show.interactable = false;
            _show.onClick.AddListener(OnShowClick);
            gameObject.SetActive(true);

            _logic = logic;
            _logic.Init(this);
        }
        
        private void Update()
        {
            _logic.OnUpdate();
        }
        
        private void OnLoadChanged(bool isOn)
        {
            IsAutoLoad = isOn;
            if (IsAutoLoad)
            {
                _logic.Load();
            }
        }
        
        public void SetAvailability(bool isAvailable)
        {
            _show.interactable = isAvailable;
        }
        
        private void OnShowClick()
        {
            _logic.Show();
        }
        
        public void SetStatus(string status)
        {
            _status.text = status;
            Debug.Log($"Rewarded: {status}");
        }
    }
}