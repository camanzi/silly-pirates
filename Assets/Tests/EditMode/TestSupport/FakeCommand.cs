using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// An ICommand that completes synchronously: it returns an Awaitable from a completion source that is
    /// already signalled, so awaiting it resumes inline and CommandQueueSO can be driven from EditMode
    /// without any frame to pump.
    ///
    /// Optionally runs a callback (used to re-enter the queue from inside a command) or throws.
    /// </summary>
    internal sealed class FakeCommand : ICommand
    {
        private readonly string _id;
        private readonly List<string> _executionLog;
        private readonly Action _onExecute;
        private readonly Exception _throwOnExecute;

        internal int ExecuteCount { get; private set; }
        internal int UndoCount { get; private set; }

        internal FakeCommand(string id, List<string> executionLog = null, Action onExecute = null,
            Exception throwOnExecute = null)
        {
            _id = id;
            _executionLog = executionLog;
            _onExecute = onExecute;
            _throwOnExecute = throwOnExecute;
        }

        public Awaitable ExecuteAsync()
        {
            ExecuteCount++;
            _executionLog?.Add(_id);
            _onExecute?.Invoke();

            if (_throwOnExecute != null)
                throw _throwOnExecute;

            AwaitableCompletionSource source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        public void Undo() => UndoCount++;
    }
}
