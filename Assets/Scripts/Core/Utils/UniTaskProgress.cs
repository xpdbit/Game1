using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game1
{
  public class UniTaskProgress
  {
    public static bool enableDelayDebug = true;

    public float delayTime;
    public event Action<ProgressSender> action;
    public XUniTaskLoopProgress loopProgress;
    public bool isProcessing { get; private set; }

    private System.Diagnostics.StackTrace trace;

    private UniTaskProgress _parent;
    public UniTaskProgress parent
    {
      get { return _parent; }
      set { this.SetParent(value); }
    }

    private UniTaskProgress _next;
    public UniTaskProgress next
    {
      get => _next;
      set
      {
        _next = value;
        _next?.SetParent(this);
      }
    }

    private void SetParent(UniTaskProgress parent)
    {
      if (this._parent != null && this._parent != parent)
        this._parent.next = null;
      this._parent = parent;
      if (parent != null)
        parent._next = this;
    }

    public UniTaskProgress(float delayTime = 0f)
    {
      if (delayTime > 99f)
        Debug.LogError("任务时间超过99s");

      this.delayTime = delayTime;
      action = null;
      loopProgress = default;
      _next = null;
      _parent = null;
      trace = enableDelayDebug ?
          new System.Diagnostics.StackTrace(3, true) : null;
    }

    public async UniTask StartAsync()
    {
      CancellationToken ct = default;
      this.isProcessing = true;
      try
      {
        if (enableDelayDebug)
        {
          await this.InvokeWithDebug(ct);
        }
        else
        {
          await this.InvokeCore(ct);
        }
      }
      catch (Exception e) when (!(e is OperationCanceledException))
      {
        Debug.LogException(e);
        if (enableDelayDebug)
        {
          this.LogExceptionDetails(e);
        }
      }
      finally
      {
        this.isProcessing = false;
      }
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
      this.isProcessing = true;
      try
      {
        if (enableDelayDebug)
        {
          await this.InvokeWithDebug(ct);
        }
        else
        {
          await this.InvokeCore(ct);
        }
      }
      catch (Exception e) when (!(e is OperationCanceledException))
      {
        Debug.LogException(e);
        if (enableDelayDebug)
        {
          this.LogExceptionDetails(e);
        }
      }
      finally
      {
        this.isProcessing = false;
      }
    }

    private async UniTask InvokeWithDebug(CancellationToken ct)
    {
      try
      {
        await this.InvokeCore(ct);
      }
      catch (Exception e)
      {
        this.LogExceptionDetails(e);
        throw;
      }
    }

    private async UniTask InvokeCore(CancellationToken ct)
    {
      if (delayTime > 0)
        await UniTask.Delay(TimeSpan.FromSeconds(delayTime), cancellationToken: ct);

      ct.ThrowIfCancellationRequested();

      var sender = new ProgressSender { Current = this };
      action?.Invoke(sender);

      if (loopProgress.isCreated)
        await loopProgress.InvokeAsync(ct);

      // 检查null而非HasValue
      if (this.next != null)
        await this.next.StartAsync(ct);
    }

    public async UniTask ExecuteImmediatelyAsync()
    {
      await this.ExecuteImmediatelyAsync(CancellationToken.None);
    }

    public async UniTask ExecuteImmediatelyAsync(CancellationToken ct)
    {
      this.isProcessing = true;
      try
      {
        if (enableDelayDebug)
        {
          await this.InvokeImmediateWithDebug(ct);
        }
        else
        {
          await this.InvokeImmediateCore(ct);
        }
      }
      catch (Exception e) when (!(e is OperationCanceledException))
      {
        Debug.LogException(e);
        if (enableDelayDebug)
        {
          this.LogExceptionDetails(e);
        }
      }
      finally
      {
        this.isProcessing = false;
      }
    }

    private async UniTask InvokeImmediateWithDebug(CancellationToken ct)
    {
      try
      {
        await this.InvokeImmediateCore(ct);
      }
      catch (Exception e)
      {
        this.LogExceptionDetails(e);
        throw;
      }
    }

    private async UniTask InvokeImmediateCore(CancellationToken ct)
    {
      ct.ThrowIfCancellationRequested();

      var sender = new ProgressSender { Current = this };
      this.action?.Invoke(sender);

      if (this.loopProgress.isCreated)
        await this.loopProgress.InvokeAsync(ct);

      if (this.next != null)
        await this.next.ExecuteImmediatelyAsync(ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogExceptionDetails(Exception ex)
    {
      if (!enableDelayDebug) return;

      StringBuilder sb = new StringBuilder(256);
      sb.AppendLine($"### UniTaskDelayProgress Error: {ex.Message}");
      sb.AppendLine("Task Chain:");

      var current = this;
      int index = 0;
      while (true)
      {
        sb.AppendLine($"{index}. (Delay: {current.delayTime}s)");

        if (current.trace != null)
        {
          var frame = current.trace.GetFrame(0);
          if (frame != null)
          {
            sb.AppendLine($"   at {frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}");
          }
        }

        // 检查null而非HasValue
        if (current.parent == null) break;
        current = current.parent;
        index++;
      }

      Debug.LogError(sb.ToString());
    }

    public UniTaskProgress GetFirst()
    {
      var curr = this;
      while (curr.parent != null)
        curr = curr.parent;
      return curr;
    }

    public UniTaskProgress GetLast()
    {
      var curr = this;
      while (curr.next != null)
        curr = curr.next;
      return curr;
    }

    public int GetDepth()
    {
      int depth = 0;
      var curr = this;
      while(curr.next != null)
      {
        depth++;
        curr = curr.next;
      }
      return depth;
    }

    public UniTaskProgress AddToEnd(float delayTime, Action<ProgressSender> action)
    {
      UniTaskProgress task = new(delayTime);
      task.parent = this.GetLast();
      task.action += action;
      return task;
    }

    // 内部类改为引用语义
    public class ProgressSender
    {
      public UniTaskProgress Current; // 直接使用类类型
    }
  }

  public struct XUniTaskLoopProgress
  {
    public Func<float, bool> stopFunc;
    public Func<float, float> delayTimeFunc;
    public Action<float> action;
    public float time;

    public bool isCreated => stopFunc != null;

    public async UniTask InvokeAsync(CancellationToken ct)
    {
      if (!this.isCreated) return;

      time = 0f;
      try
      {
        while (!stopFunc(time) && !ct.IsCancellationRequested)
        {
          action(time);
          float delay = delayTimeFunc?.Invoke(time) ?? 0f;
          time += delay;
          await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
        }
      }
      catch (OperationCanceledException) { /* 正常取消 */ }
    }

    #region 创建方法

    public static XUniTaskLoopProgress Create(
        Func<float, bool> stopFunc,
        Func<float, float> delayTimeFunc,
        Action<float> actionByTime)
    {
      return new XUniTaskLoopProgress
      {
        stopFunc = stopFunc,
        delayTimeFunc = delayTimeFunc,
        action = actionByTime,
        time = 0f
      };
    }

    public static XUniTaskLoopProgress Create(
        int number,
        Func<int, int> perNumberFunc,
        Func<int, float> delayTimeFunc,
        Action<int> action)
    {
      int index = 0;
      return new XUniTaskLoopProgress
      {
        stopFunc = (time) => index >= number,
        delayTimeFunc = (time) => delayTimeFunc(index),
        action = _ =>
        {
          int perNumber = perNumberFunc(number);
          for (int i = 0; i < perNumber && index < number; i++)
          {
            action(index++);
          }
        }
      };
    }

    public static XUniTaskLoopProgress Create(
        int number,
        Func<int, float> delayTimeFunc,
        Action<int> action)
    {
      return Create(number, _ => 1, delayTimeFunc, action);
    }

    public static XUniTaskLoopProgress Create(
        int number,
        float delayTime,
        Action<int> action)
    {
      return Create(number, _ => delayTime, action);
    }

    public static XUniTaskLoopProgress Create(
      int start,
      int end,
      float delayTime,
      Action<int> action
      )
    {
      return Create(Mathf.Abs(end - start) + 1, _ => delayTime, action);
    }

    #endregion
  }

  public class XUniTaskProcessQueue
  {
    private UniTaskProgress processChain;
    public bool isProcessing => this.processChain != null;
    public int depth => processChain.GetDepth();
    private CancellationTokenSource cancellationTokenSource = new();
    private readonly UniTaskProgress queueEndMarker;
    private readonly object syncRoot = new object();

    /// <summary>
    /// 结束事件，当队列完成时触发
    /// </summary>
    public event Action endAction;

    public XUniTaskProcessQueue()
    {
      this.queueEndMarker = new UniTaskProgress();
      this.queueEndMarker.action += s =>
      {
        this.queueEndMarker.parent = null;
        this.processChain = null;
        this.endAction?.Invoke();
      };
    }

    public void Enqueue(UniTaskProgress progress)
    {
      lock (syncRoot)
      {
        if (progress == null)
          return;

        if (this.processChain == null)
        {
          progress.GetLast().next = this.queueEndMarker;
          this.processChain = progress;
          this.processChain.StartAsync(this.cancellationTokenSource.Token).Forget();
        }
        else
        {
          var last = this.processChain.GetLast();
          if (last == this.queueEndMarker)
          {
            last = last.parent;
            this.queueEndMarker.parent = null;
          }
          last.next = progress;
          progress.GetLast().next = this.queueEndMarker;
        }
      }
    }

    public void Cancel()
    {
      if (!this.isProcessing) return;

      lock (syncRoot)
      {
        this.cancellationTokenSource?.Cancel();
        this.cancellationTokenSource?.Dispose();
        this.cancellationTokenSource = new CancellationTokenSource();
        if (this.processChain != null)
        {
          this.processChain.parent = null;
          this.processChain = null;
        }
      }
    }
  }

}