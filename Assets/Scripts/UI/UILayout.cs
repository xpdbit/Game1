using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game1
{
  /// <summary>
  /// UI布局组件，支持多种布局方式（拉伸、平铺）和方向（水平、垂直），可配置行内数量限制和间距，并提供动画支持。
  /// </summary>
  public class UILayout : MonoBehaviour
  {
    public RectTransform parentRT;
    public bool lockParentSize = true;
    public LayoutHorizontalDirection hDirection = LayoutHorizontalDirection.LeftToRight;
    public LayoutVerticalDirection vDirection = LayoutVerticalDirection.TopToBottom;
    public LayoutRow layoutRow = LayoutRow.StretchHorizontal;
    public float validLineSize = 0f; // 行内数量
    public Vector2 spacing = Vector2.zero;
    public Func<RectTransform, string> layoutOrderFunc = rt =>
    {
      return rt.name.PadLeft(10, '0');
    };
    public Vector2 contentSize { private set; get; }

    public RectTransform rectTransform => _rectTransform ??= this.GetComponent<RectTransform>();
    private RectTransform _rectTransform;

    public enum LayoutHorizontalDirection { LeftToRight, RightToLeft }
    public enum LayoutVerticalDirection { TopToBottom, BottomToTop }
    public enum LayoutRow { StretchHorizontal, StretchVertical, TileHorizontal, TileVertical }

    private void OnValidate()
    {
      if (parentRT == null)
        parentRT = this.GetComponent<RectTransform>();
    }

    #region Layout

    public UniTaskProgress Layout(LayoutSender sender)
    {
      List<RectTransform> children = this.GetValidChildren(sender.ignores);
      List<RectTransform> sortedChildren;

      if (layoutOrderFunc != null && children.Count > 0)
      {
        sortedChildren = children.OrderBy(layoutOrderFunc).ToList();
      }
      else
      {
        sortedChildren = children.ToList();
      }

      UniTaskProgress task = new();

      switch (layoutRow)
      {
        case LayoutRow.StretchHorizontal:
        case LayoutRow.StretchVertical:
          task = this.LayoutStretch(sortedChildren, sender);
          break;
        case LayoutRow.TileHorizontal:
        case LayoutRow.TileVertical:
          task = this.LayoutTile(sortedChildren, sender);
          break;
      }

      if (!lockParentSize)
        parentRT.sizeDelta = this.contentSize;

      return task;
    }

    private UniTaskProgress LayoutStretch(List<RectTransform> children, LayoutSender sender)
    {
      Vector2 contentSize = Vector2.zero;
      Vector2 current = Vector2.zero;
      bool isHorizontal = this.layoutRow == LayoutRow.StretchHorizontal;

      for (int i = 0; i < children.Count; i++)
      {
        RectTransform rt = children[i];

        Vector2 size = rt.sizeDelta;
        Vector2 pivot = rt.pivot;

        if (isHorizontal)
        {
          // 垂直拉伸布局
          int vDirFactor = (this.vDirection == LayoutVerticalDirection.TopToBottom) ? -1 : 1;
          float yPos = this.vDirection == LayoutVerticalDirection.TopToBottom
              ? current.y - size.y * (1 - pivot.y)
              : current.y + size.y * pivot.y;

          rt.localPosition = new Vector2(this.GetHorizontalPosition(current.x, size.x, pivot.x), yPos);
          rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
          rt.offsetMax = new Vector2(0f, rt.offsetMax.y);

          contentSize.x = Mathf.Max(this.contentSize.x, size.x);
          contentSize.y += size.y + (this.contentSize.y > 0 ? this.spacing.y : 0);
          current.y += size.y * vDirFactor + this.spacing.y * vDirFactor;
        }
        else // StretchVertical
        {
          // 水平拉伸布局
          int hDirFactor = (this.hDirection == LayoutHorizontalDirection.LeftToRight) ? 1 : -1;
          float xPos = this.hDirection == LayoutHorizontalDirection.LeftToRight
              ? current.x + size.x * pivot.x
              : current.x - size.x * (1 - pivot.x);

          rt.localPosition = new Vector2(xPos, this.GetVerticalPosition(current.y, size.y, pivot.y));
          rt.offsetMin = new Vector2(rt.offsetMin.x, 0f);
          rt.offsetMax = new Vector2(rt.offsetMax.x, 0f);

          contentSize.y = Mathf.Max(this.contentSize.y, size.y);
          contentSize.x += size.x + (this.contentSize.x > 0 ? this.spacing.x : 0);
          current.x += size.x * hDirFactor + this.spacing.x * hDirFactor;
        }
      }

      this.contentSize = contentSize;

      return this.AnimateChildren(sender);
    }

    private float GetHorizontalPosition(float baseX, float width, float pivotX, bool ignoreSpacing = false)
    {
      float _spacing = ignoreSpacing ? 0f : spacing.x;
      return hDirection == LayoutHorizontalDirection.LeftToRight
          ? baseX + _spacing + width * pivotX
          : baseX - _spacing - width * (1 - pivotX);
    }

    private float GetVerticalPosition(float baseY, float height, float pivotY, bool ignoreSpacing = false)
    {
      float _spacing = ignoreSpacing ? 0f : spacing.y;
      return vDirection == LayoutVerticalDirection.TopToBottom
          ? baseY - _spacing - height * (1 - pivotY)
          : baseY + _spacing + height * pivotY;
    }

    private UniTaskProgress LayoutTile(List<RectTransform> children, LayoutSender sender)
    {
      Vector2 contentSize = Vector2.zero;
      Vector2 current = Vector2.zero;
      bool isHorizontal = layoutRow == LayoutRow.TileHorizontal;
      float lineOtherSideSize = 0f; // 行另一边的尺寸（横向则y，纵向则x）
      float rowSize = 0f; // 行宽度尺寸
      float pureRowSize = 0f; // 列高度尺寸
      int inlineCount = 0; // 行内数量
      int index = 0; // 子项指引
      int lineCount = 0; // 行数量

      foreach (RectTransform rt in children)
      {
        bool isFirst = index == 0;
        Vector2 size = rt.sizeDelta;
        Vector2 pivot = rt.pivot;
        float primarySize = isHorizontal ? size.x : size.y;
        float secondarySize = isHorizontal ? size.y : size.x;

        // 换行判断
        if (validLineSize > 0 && inlineCount > 0)
        {
          bool needWrap = isHorizontal
              ? (rowSize + primarySize) > validLineSize
              : (pureRowSize + primarySize) > validLineSize;

          if (needWrap)
          {
            UpdateContentSize(ref contentSize, ref current, isHorizontal, lineOtherSideSize, pureRowSize);

            rowSize = 0f;
            pureRowSize = 0f;
            lineOtherSideSize = 0f;
            inlineCount = 0;
            lineCount++;
          }
        }

        bool ignoreSpacingX = inlineCount == 0;
        bool ignoreSpacingY = lineCount == 0;

        // 计算位置
        Vector2 newPos = isHorizontal
            ? new Vector2(
                this.GetHorizontalPosition(current.x, size.x, pivot.x, ignoreSpacingX),
                this.GetVerticalPosition(current.y, size.y, pivot.y, ignoreSpacingY))
            : new Vector2(
                this.GetHorizontalPosition(current.x, size.x, pivot.x, ignoreSpacingX),
                this.GetVerticalPosition(current.y, size.y, pivot.y, ignoreSpacingY));

        rt.localPosition = newPos;

        // 更新累计值
        if (isHorizontal)
        {
          current.x += (hDirection == LayoutHorizontalDirection.LeftToRight ? 1 : -1) *
              (size.x + (ignoreSpacingX ? 0f : spacing.x));
          lineOtherSideSize = Mathf.Max(lineOtherSideSize, size.y);
          rowSize += size.x + spacing.x;
        }
        else
        {
          current.y += (vDirection == LayoutVerticalDirection.TopToBottom ? -1 : 1) *
              (size.y + (ignoreSpacingY ? 0f : spacing.y));
          lineOtherSideSize = Mathf.Max(lineOtherSideSize, size.x);
          pureRowSize += size.y + spacing.y;
        }

        inlineCount++;
        index++;
      }

      // 处理最后一行
      if (inlineCount > 0)
        UpdateContentSize(ref contentSize, ref current, isHorizontal, lineOtherSideSize, pureRowSize);

      this.contentSize = contentSize;

      return this.AnimateChildren(sender);

      void UpdateContentSize(ref Vector2 contentSize, ref Vector2 current, bool isHorizontal, float lineOtherSideSize, float pureRowSize)
      {
        if (isHorizontal)
        {
          contentSize.x = Mathf.Max(contentSize.x, current.x - (hDirection == LayoutHorizontalDirection.LeftToRight ? 0 : current.x));
          contentSize.y += lineOtherSideSize + (contentSize.y > 0 ? spacing.y : 0);
          current = new Vector2(0, current.y + lineOtherSideSize * (vDirection == LayoutVerticalDirection.TopToBottom ? -1 : 1) + spacing.y);
        }
        else
        {
          contentSize.y = Mathf.Max(contentSize.y, pureRowSize);
          contentSize.x += lineOtherSideSize + (contentSize.x > 0 ? spacing.x : 0);
          current = new Vector2(current.x + lineOtherSideSize * (hDirection == LayoutHorizontalDirection.LeftToRight ? 1 : -1) + spacing.x, 0);
        }
      }
    }

    private List<RectTransform> GetValidChildren(HashSet<Transform> ignores)
    {
      List<RectTransform> children = new List<RectTransform>();
      for (int i = 0; i < parentRT.childCount; i++)
      {
        Transform t = parentRT.GetChild(i);
        if (t.gameObject.activeSelf && (ignores == null || !ignores.Contains(t)))
          children.Add(t.GetComponent<RectTransform>());
      }
      if (layoutOrderFunc != null)
        children.Sort((a, b) => layoutOrderFunc(a).CompareTo(layoutOrderFunc(b)));
      return children;
    }

    public void Distribution(Dictionary<Vector2Int, RectTransform> items, Action<Vector2Int, RectTransform> layoutItemAction)
    {
      float minX = float.MaxValue;
      float maxX = float.MinValue;
      float minY = float.MaxValue;
      float maxY = float.MinValue;

      foreach (var kvp in items)
      {
        Vector2Int coord = kvp.Key;
        RectTransform child = kvp.Value;
        Vector2 size = child.sizeDelta;
        Vector2 pivot = child.pivot;
        float baseX = coord.x * (size.x + this.spacing.x);
        float baseY = coord.y * (size.y + this.spacing.y);
        float xPos = this.GetHorizontalPosition(baseX, size.x, child.pivot.x, false);
        float yPos = this.GetVerticalPosition(baseY, size.y, child.pivot.y, false);
        child.localPosition = new Vector3(xPos, yPos, 0f);

        // 计算包围框边界
        float left = xPos - size.x - spacing.x;
        float right = xPos + size.x + spacing.x;
        float bottom = yPos - size.y - spacing.y;
        float top = yPos + size.y + spacing.y;

        if (left < minX) minX = left;
        if (right > maxX) maxX = right;
        if (bottom < minY) minY = bottom;
        if (top > maxY) maxY = top;
        layoutItemAction?.Invoke(coord, child);
      }

      // 计算最终内容尺寸
      if (minX <= maxX && minY <= maxY)
      {
        this.contentSize = new Vector2(maxX - minX, maxY - minY);
      }
      else
      {
        this.contentSize = Vector2.zero;
      }
    }

    public UniTaskProgress AnimateChildren(LayoutSender sender)
    {
      if (sender.animationType == LayoutAnimationType.None
        || sender.animateChildren == null) 
        return new();

      UniTaskProgress task = new();

      foreach (var child in sender.animateChildren)
      {
        var rt = child;

        Vector2 startSize = rt.sizeDelta * sender.animationStartScale;
        Vector2 endSize = rt.sizeDelta * sender.animationEndScale;

        if (sender.animationType == LayoutAnimationType.WidthExpand)
        {
          startSize.y = rt.sizeDelta.y;
          endSize.y = startSize.y;
        }
        else if (sender.animationType == LayoutAnimationType.HeightExpand)
        {
          startSize.x = rt.sizeDelta.x;
          endSize.x = startSize.x;
        }
        rt.sizeDelta = startSize;

        float speed = Time.fixedDeltaTime * 6.5f * sender.animationSpeedFactor;
        UniTaskProgress animationTask = new();
        animationTask.loopProgress = XUniTaskLoopProgress.Create(
          t =>
          {
            const float epsilon = 0.01f;
            bool xDone = Mathf.Abs(rt.sizeDelta.x - endSize.x) < epsilon;
            bool yDone = Mathf.Abs(rt.sizeDelta.y - endSize.y) < epsilon;

            if (xDone && yDone)
            {
              rt.sizeDelta = endSize;
              return true;
            }
            return false;
          },
          t => Time.fixedDeltaTime,
          t =>
          {
            Vector2 size = startSize;
            size = Vector2.Lerp(rt.sizeDelta, endSize, speed);
            rt.sizeDelta = size;
          }
          );
        animationTask.parent = task.GetLast();
      }

      return task;
    }

    #endregion

    [ContextMenu("应用布局")]
    private void ContextMenu_ApplyLayout()
    {
      this.Layout(new LayoutSender()).ExecuteImmediatelyAsync();
    }

    public class LayoutSender
    {
      public HashSet<Transform> ignores = null;
      public HashSet<RectTransform> animateChildren = null;
      public LayoutAnimationType animationType = LayoutAnimationType.None;
      public float animationStartScale = 0f;
      public float animationEndScale = 1f;
      public float animationSpeedFactor = 1f;
    }

    public enum LayoutAnimationType
    {
      None = 0,
      WidthExpand,
      HeightExpand,
      SizeExpand,
    }
  }
}