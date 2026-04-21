using UnityEngine;

namespace Game1
{
    public class GameTest : MonoBehaviour
    {
        public void Start()
        {
            var item = 
            UIManager.instance.inventory.Append("item1");
            UIManager.instance.inventory.UpdateUI();
        }
    }
}