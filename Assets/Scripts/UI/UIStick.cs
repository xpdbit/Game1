using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game1
{
  public class UIStick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    public UICheck stickCheck;
    public RectTransform stickBackground;
    public float deathZone = 10f;

    /// <summary>
    /// 返回摇杆方向，如果摇杆位移超过死区则更新方向，否则保持上次方向
    /// </summary>
    public Vector2 direction
    {
      get
      {
        if (stickCheck.transform.localPosition.magnitude > deathZone)
          _direction = stickCheck.transform.localPosition.normalized;

        return _direction;
      }
    }

    private Vector2 _direction = Vector2.down;

    private bool indrag = false;

    private void Update()
    {
      Vector3 p = stickBackground.position;

      if (stickCheck.isEnter
        && !indrag)
      {
        if (Input.GetMouseButton(0)
          || Input.touchCount > 0)
        {
          indrag = true;
        }
      }
      
      if (indrag)
      {
        if (Input.GetMouseButtonUp(0)
          || (Input.touchCount == 0 && !Input.GetMouseButton(0)))
        {
          indrag = false;
        }
        else if (Input.GetMouseButton(0))
        {
          p = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
          p = Input.touches[0].position;
        }
      }

      stickCheck.transform.position = Vector3.Lerp(stickCheck.transform.position, p, Time.deltaTime * 10f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }
  }
}