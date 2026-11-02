using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearMenuAction : MonoBehaviour
    {
        public string action;
        public int argument;
        private Button button;
        private void OnEnable()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Click);
        }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Click); }
        public void Click() => GetComponentInParent<BearHomeMenuController>()?.Handle(action, argument);
    }
}
