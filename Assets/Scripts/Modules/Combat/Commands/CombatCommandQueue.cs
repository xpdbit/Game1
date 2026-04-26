using System;
using System.Collections.Generic;
using Game1.Modules.Combat.State;

namespace Game1.Modules.Combat.Commands
{
    /// <summary>
    /// 战斗命令队列
    /// 管理命令执行历史，支持撤销和重做
    /// </summary>
    public class CombatCommandQueue
    {
        private readonly List<ICombatCommand> _commandHistory;
        private readonly List<ICombatCommand> _undoneCommands;
        private CombatContext _context;
        private int _historyIndex;

        /// <summary>
        /// 当命令执行时触发
        /// </summary>
        public event Action<ICombatCommand> OnCommandExecuted;

        /// <summary>
        /// 当命令撤销时触发
        /// </summary>
        public event Action<ICombatCommand> OnCommandUndone;

        /// <summary>
        /// 当命令重做时触发
        /// </summary>
        public event Action<ICombatCommand> OnCommandRedone;

        /// <summary>
        /// 命令历史记录
        /// </summary>
        public IReadOnlyList<ICombatCommand> commandHistory => _commandHistory.AsReadOnly();

        /// <summary>
        /// 当前历史索引
        /// </summary>
        public int HistoryIndex => _historyIndex;

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => _historyIndex > 0;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => _historyIndex < _commandHistory.Count;

        /// <summary>
        /// 创建命令队列
        /// </summary>
        public CombatCommandQueue()
        {
            _commandHistory = new List<ICombatCommand>();
            _undoneCommands = new List<ICombatCommand>();
            _historyIndex = 0;
        }

        /// <summary>
        /// 初始化队列
        /// </summary>
        public void Initialize(CombatContext context)
        {
            _context = context;
            Clear();
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        public void Execute(ICombatCommand command)
        {
            if (_context == null)
            {
                throw new InvalidOperationException("CombatCommandQueue not initialized. Call Initialize() first.");
            }

            // 如果在历史中间执行新命令，丢弃重做栈
            if (_historyIndex < _commandHistory.Count)
            {
                _undoneCommands.Clear();
            }

            // 执行命令
            command.Execute(_context);

            // 记录到历史
            _commandHistory.Add(command);
            _historyIndex++;

            // 记录到上下文
            _context.RecordAction(command, command.IsPlayerAction);

            OnCommandExecuted?.Invoke(command);
        }

        /// <summary>
        /// 撤销上一个命令
        /// </summary>
        /// <returns>被撤销的命令，如果无法撤销则返回null</returns>
        public ICombatCommand Undo()
        {
            if (!CanUndo)
            {
                return null;
            }

            _historyIndex--;
            ICombatCommand command = _commandHistory[_historyIndex];
            command.Undo(_context);

            _undoneCommands.Add(command);

            OnCommandUndone?.Invoke(command);

            return command;
        }

        /// <summary>
        /// 重做上一个撤销的命令
        /// </summary>
        /// <returns>被重做的命令，如果无法重做则返回null</returns>
        public ICombatCommand Redo()
        {
            if (!CanRedo)
            {
                return null;
            }

            ICombatCommand command = _commandHistory[_historyIndex];
            command.Execute(_context);

            _historyIndex++;

            OnCommandRedone?.Invoke(command);

            return command;
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void Clear()
        {
            _commandHistory.Clear();
            _undoneCommands.Clear();
            _historyIndex = 0;
        }

        /// <summary>
        /// 获取最后一个执行的命令
        /// </summary>
        public ICombatCommand GetLastCommand()
        {
            if (_commandHistory.Count == 0)
            {
                return null;
            }

            return _commandHistory[_commandHistory.Count - 1];
        }

        /// <summary>
        /// 获取指定索引的命令
        /// </summary>
        public ICombatCommand GetCommandAt(int index)
        {
            if (index < 0 || index >= _commandHistory.Count)
            {
                return null;
            }

            return _commandHistory[index];
        }
    }
}