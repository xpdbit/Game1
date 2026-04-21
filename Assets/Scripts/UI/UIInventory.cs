using System;
using System.Collections.Generic;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    public class UIInventory : MonoBehaviour
    {
        public UIListItems uIListItems;

        private List<UIInventoryItem> _items = new List<UIInventoryItem>();
        public UIInventoryItem[] items => _items.ToArray();

        public UIInventoryItem Append(string id, int amount = 1)
        {
            var result = uIListItems.AddItem(id);
            var item = new UIInventoryItem
            {
                id = id,
                amount = amount,
                listItemRect = result.rectTransform
            };
            _items.Add(item);
            return item;
        }

        public UIInventoryItem Append(UIInventoryItem item)
        {
            return Append(item.id, item.amount);
        }


        /// <summary>
        /// 移除物品，如果 amount 小于 0 则移除全部，否则移除指定数量。
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        public void Remove(UIInventoryItem item, int amount = -1)
        {
            if (amount < 0)
            {
                amount = item.amount;
            }
            else
            {
                amount = Math.Min(amount, item.amount);
            }

            item.amount -= amount;
            if (item.amount <= 0)
            {
                _items.Remove(item);
                uIListItems.RemoveItem(item.id);
            }
            else
            {
                item.UpdateUI();
            }
        }

        public void Clear()
        {
            _items.Clear();
            uIListItems.Clear();
        }

        public void Open()
        {
            UpdateUI();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
        
        public void UpdateUI()
        {
            uIListItems.Clear();
            foreach (var item in items)
            {
                var inventoryItem = Append(item.id, item.amount);
                inventoryItem.UpdateUI();
            }
        }
    }

    public class UIInventoryItem
    {
        public string id;
        public int amount;

        public RectTransform listItemRect;

        public Button button => _button ??= listItemRect.Find("Button").GetComponent<Button>();
        private Button _button;

        public UIText nameText => _nameText ??= listItemRect.Find("Button/NameText").GetComponentInChildren<UIText>();
        private UIText _nameText;

        public UIText amountText => _amountText ??= listItemRect.Find("Button/AmountText").GetComponentInChildren<UIText>();
        private UIText _amountText;

        public void UpdateUI()
        {
            listItemRect.name = id;
            nameText.text = id;
            amountText.text = "x" + amount.ToString();
        }
    }
}