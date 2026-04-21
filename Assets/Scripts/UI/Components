using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using XUtilities;

namespace Game1
{
  public class UIMain : MonoBehaviour
  {
    [Serializable]
    public class UIDebugText
    {
      public TextMeshProUGUI debugTMP;

      public void Update()
      {
        // TODO: 根据游戏状态更新调试信息
        if (debugTMP != null)
        {
          debugTMP.text = "Game Running...";
        }
      }
    }

    [Serializable]
    public class UIModulePanel
    {
      public RectTransform panel;
      public UIListItems moduleListItems;

      public void Show()
      {
        if (panel != null)
          panel.gameObject.SetActive(true);
        if (moduleListItems != null)
        {
          moduleListItems.Clear();
          // TODO: 根据实际模块系统配置
        }
      }
    }

    [Serializable]
    public class UIInput
    {
      public UIStick moveStick;

      public void Update()
      {

      }
    }

    public static UIMain instance => GameMain.instance?.GetComponent<UIMain>();

    public static void CloseAllPanel()
    {
      if (instance?.uIModulePanel?.panel != null)
        instance.uIModulePanel.panel.gameObject.SetActive(false);
    }

    public static void ShowModulePanel()
    {
      CloseAllPanel();
      instance?.uIModulePanel?.Show();
    }

    public static void GlobalStart()
    {
      CloseAllPanel();
    }

    public static void GlobalLateUpdate()
    {
      instance?.uIDebugText?.Update();
      instance?.uIInput?.Update();
    }


    public UIDebugText uIDebugText;
    public UIModulePanel uIModulePanel;
    public UIInput uIInput;
  }
}