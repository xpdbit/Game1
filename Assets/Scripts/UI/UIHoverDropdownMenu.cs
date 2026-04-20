using UnityEngine;
using UnityEngine.EventSystems;

namespace Game1
{
  public class UIHoverDropdownMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    public GameObject content;  // 下拉菜单内容区域

    private void Awake()
    {
      if (content != null)
        content.SetActive(false);
    }

    // 鼠标进入时触发
    public void OnPointerEnter(PointerEventData eventData)
    {
      if (this.content != null)
      {
        this.content.SetActive(true);  // 显示下拉菜单
      }

      // ExecuteEvents.ExecuteHierarchy(this.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
    }

    // 鼠标离开时触发
    public void OnPointerExit(PointerEventData eventData)
    {
      if (this.content != null)
      {
        this.content.SetActive(false);  // 隐藏下拉菜单
      }

      // ExecuteEvents.ExecuteHierarchy(this.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
    }
  }
}
