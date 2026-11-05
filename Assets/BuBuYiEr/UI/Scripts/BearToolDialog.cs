using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearToolDialog : MonoBehaviour
    {
        private IngameViewController owner;
        private GameObject root;
        private Text description, balance, confirmText;
        private Button confirm;
        private BearIllustration icon;
        private BoostButtonController selected;
        private bool addingSlot;
        private float refreshAt;
        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(IngameViewController view, BearMenuArt art)
        {
            owner = view;
            RectTransform overlay = BearMenuFactory.Node(view.transform, "BearToolDialog", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(overlay);
            root = overlay.gameObject;
            overlay.gameObject.AddComponent<Image>().color = new Color(.23f, .12f, .05f, .55f);
            RectTransform safe = BearGameTheme.Frame(overlay, "ToolDialogSafeArea");
            RectTransform card = BearGameTheme.Paper(safe, "ToolPaper", 128, 400, 768, 790, BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Picture(art, card, "TitleRibbon", "Setting/TitleBg", 76, -40, 616, 136, true);
            BearGameTheme.Label(art, card, "Title", "糖果小帮手", 91, -19, 586, 97, 49);
            icon = BearIllustration.Create(card, "ToolIllustration", BearDrawing.Slot, 288, 110, 192, 192);
            description = BearGameTheme.Label(art, card, "Description", "", 65, 329, 638, 121, 31);
            balance = BearGameTheme.Label(art, card, "Balance", "", 65, 466, 638, 50, 28);
            confirm = BearGameTheme.Button(art, card, "Confirm", "", 100, 547, 568, 96, BearGameTheme.Gold, Confirm);
            confirmText = confirm.GetComponentInChildren<Text>();
            BearGameTheme.Button(art, card, "Cancel", "先不用", 223, 680, 322, 75, BearGameTheme.Pink, Close);
            root.SetActive(false);
        }

        public void Show(BoostButtonController tool, bool slot)
        {
            selected = tool;
            addingSlot = slot;
            icon.drawing = BearIllustration.Tool(tool.PowerUpType);
            icon.SetVerticesDirty();
            description.text = Description(tool.PowerUpType);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            RefreshBalance();
        }

        public static string Description(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return "点击棋盘上的空位，用磁铁收回传送带上暂时接不到糖果的罐子。";
                case PowerUpType.Hammer: return "点击冰冻的糖果罐，用小木锤敲开冰层，让它继续收集糖果。";
                case PowerUpType.Key: return "点击上锁的糖果罐，用钥匙打开它。";
                case PowerUpType.Cooler: return "点击发烫的糖果罐，帮它降降温。";
                default: return "给传送带增加一个空位，让糖果罐排得更轻松。新增空位在本局内有效。";
            }
        }

        private void Update()
        {
            if (IsOpen && Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + .2f;
                RefreshBalance();
            }
        }

        private void RefreshBalance()
        {
            if (selected == null || IngameManager.Instance == null) return;
            int coins = ServicesManager.Instance.CoinManager.TotalCoins;
            string text = addingSlot ? "本次消耗 1 个加一格道具" : "当前金币：" + coins + " · 本次补充 1 个";
            if (balance.text != text) balance.text = text;
            confirm.interactable = addingSlot ? PlayerDataHandler.GetSlotCount() > 0 && IngameManager.Instance.ConveyorManager.CanIncreaseSlot
                : coins >= selected.PurchasePrice;
            string caption = addingSlot ? IngameManager.Instance.ConveyorManager.CanIncreaseSlot ? "增加一个空位" : "空位已经加满啦"
                : coins >= selected.PurchasePrice ? "购买 1 个 · " + selected.PurchasePrice + " 金币" : "金币还不够";
            if (confirmText.text != caption) confirmText.text = caption;
        }

        private void Confirm()
        {
            if (!confirm.interactable || IngameManager.Instance == null || IngameManager.Instance.IsPaused ||
                IngameManager.Instance.IngameState != IngameState.Ingame_Playing) return;
            if (addingSlot)
            {
                if (!IngameManager.Instance.ConveyorManager.CanIncreaseSlot || PlayerDataHandler.GetSlotCount() <= 0) return;
                IngameManager.Instance.ConveyorManager.IncreaseSlot();
                PlayerDataHandler.UpdateSlotCount(-1);
                PlayerPrefs.Save();
                selected.UpdateCurrentAmount();
                owner.ShowHint("传送带多了一个空位，继续收集糖果吧");
            }
            else
            {
                if (!ServicesManager.Instance.CoinManager.TrySpendCoins(selected.PurchasePrice)) return;
                selected.AddBoostAmount(1);
                PlayerPrefs.Save();
                owner.ShowHint("小帮手已补充，点击道具就能使用");
            }
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            Close();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
