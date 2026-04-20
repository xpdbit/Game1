using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Game1
{
  public class UICheck : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    public static HashSet<UICheck> triggers = new();

    public static bool isEnterUI => triggers.Count > 0;

    public bool blockMovement = true;
    public bool blockRotation = true;
    public bool blockZoom = true;
    public bool blockSelectCell = true;

    [Space]

    public bool isEnter = false;

    private void OnDestroy()
    {
      this.Exit();
    }

    private void OnDisable()
    {
      this.Exit();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      this.Enter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      this.Exit();
    }

    private void Enter()
    {
      isEnter = true;
      triggers.Add(this);
    }

    private void Exit()
    {
      isEnter = false;
      triggers.Remove(this);
    }
  }
}