using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XUtilities;

namespace Game1
{
  [RequireComponent(typeof(UILayout))]
  public class UIListItems : MonoBehaviour
  {
    public RectTransform templateRT;
    public UILayout layout;
    public List<RectTransform> children = new();

    private Dictionary<string, RectTransform> _bindings = new();
    private Dictionary<RectTransform, string> _bindingsReverse = new();

    /// <summary>
    /// 每个 UIListItems 之间都是孤岛，不用担心会存在不同场景的 BindingID 出现冲突的情况。
    /// </summary>
    public IDictionary<string, RectTransform> bindings => _bindings;
    public IDictionary<RectTransform, string> bindingsReverse => _bindingsReverse;

    public Action onClearAction;

    public RectTransform rectTransform => _rectTransform ??= this.GetComponent<RectTransform>();
    private RectTransform _rectTransform;

    private void Reset()
    {
      layout = this.GetComponent<UILayout>();
    }

    private void Awake()
    {
      layout = this.GetComponent<UILayout>();
      this.templateRT.gameObject.SetActive(false);
      _bindings.Clear();
    }

    public AddItemResult AddItem(
      string bindingId = null,
      UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None
      )
    {
      var rt = this.AddItem();

      if (bindingId != null)
      {
        _bindings.Add(bindingId, rt);
        _bindingsReverse.Add(rt, bindingId);
      }

      var task = this.Layout(new HashSet<RectTransform>() { rt }, null, animationType);

      return new()
      {
        rectTransform = rt,
        task = task
      };
    }

    public struct AddItemResult
    {
      public RectTransform rectTransform;
      public XUniTaskProgress task;
    }

    private RectTransform AddItem()
    {
      var go = XObjectPool.Get(this.templateRT.gameObject);
      go.transform.SetParent(this.layout.parentRT);
      go.transform.localScale = Vector3.one;

      var rt = go.GetComponent<RectTransform>();
      children.Add(rt);
      rt.localEulerAngles = templateRT.localEulerAngles;

      return rt;
    }

    public void RemoveItem(string id, UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None)
    {
      var rt = _bindings[id];
      _bindings.Remove(id);
      _bindingsReverse.Remove(rt);
      this.RemoveItem(rt, animationType);
    }

    public XUniTaskProgress RemoveItem(RectTransform rt, UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None)
    {
      children.Remove(rt);
      rt.SetParent(null);
      var task = this.Layout(null, new HashSet<RectTransform>() { rt }, animationType);
      task.AddToEnd(0f, s =>
      {
        XObjectPool.Release(rt.gameObject, this.templateRT.gameObject);
      });
      return task;
    }

    public XUniTaskProgress RemoveItems(IEnumerable<string> ids, UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None)
    {
      HashSet<RectTransform> rtSet = new();
      foreach (var id in ids)
      {
        var rt = _bindings[id];
        rtSet.Add(rt);
        _bindings.Remove(id);
        _bindingsReverse.Remove(rt);
      }

      if (rtSet.Count > 0)
      {
        var rtSize = rtSet.First().sizeDelta;

        var task = this.Layout(null, rtSet, animationType);
        task.AddToEnd(0f, s =>
        {
          foreach (var rt in rtSet)
          {
            rt.sizeDelta = rtSize;
            children.Remove(rt);
            XObjectPool.Release(rt.gameObject, this.templateRT.gameObject);
          }
          this.layout.Layout(new());
        });
        return task;
      }

      return new();
    }

    public XUniTaskProgress AddItems(
      int count,
      Action<int, RectTransform> action, 
      UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None,
      Func<int, string> getBingdingIdFunc = null)
    {
      HashSet<RectTransform> items = new();
      for (int i = 0; i < count; i++)
      {
        var r = this.AddItem(getBingdingIdFunc?.Invoke(i) ?? null);
        items.Add(r.rectTransform);
        action?.Invoke(i, r.rectTransform);
        ;
      }

      return this.Layout(items, null, animationType);
    }

    public void Distribution(IEnumerable<Vector2Int> coords, Action<Vector2Int, RectTransform> newItemAction)
    {
      this.Clear();
      Dictionary<Vector2Int, RectTransform> rts = new();
      foreach (Vector2Int coord in coords)
      {
        RectTransform rt = this.AddItem();
        newItemAction?.Invoke(coord, rt); // 同时传递坐标和实例
        rts.Add(coord, rt);
      }
      this.layout.Distribution(rts, null);
    }

    public XUniTaskProgress Layout(
      HashSet<RectTransform> newItems = null,
      HashSet<RectTransform> removeItems = null, 
      UILayout.LayoutAnimationType animationType = UILayout.LayoutAnimationType.None
      )
    {
      var ignores = new HashSet<Transform>() { this.templateRT };

      XUniTaskProgress task = new();

      if (newItems?.Count > 0)
      {
        var newItemLayoutTask = this.layout.Layout(new UILayout.LayoutSender()
        {
          ignores = ignores,
          animateChildren = newItems,
          animationType = animationType,
          animationStartScale = 0.1f,
          animationEndScale = 1f,
        });
        newItemLayoutTask.parent = task;
      }

      if (removeItems?.Count > 0)
      {
        var removeItemLayoutTask = this.layout.AnimateChildren(new UILayout.LayoutSender()
        {
          ignores = ignores,
          animateChildren = removeItems,
          animationType = animationType,
          animationStartScale = 1f,
          animationEndScale = 0f,
          animationSpeedFactor = 2f,
        });
        removeItemLayoutTask.parent = task;
      }

      if (animationType == UILayout.LayoutAnimationType.None)
      {
        task.StartAsync();
        return null;
      }

      return task;
    }

    public void Clear()
    {
      for (int i = 0; i < children.Count; i++)
      {
        var child = children[i];
        XObjectPool.Release(child.gameObject, this.templateRT.gameObject);
      }
      children.Clear();
      _bindings.Clear();
      _bindingsReverse.Clear();
    }

    public void Update()
    {

    }
  }
}