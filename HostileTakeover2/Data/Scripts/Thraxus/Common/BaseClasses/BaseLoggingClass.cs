﻿using System;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
	public abstract class BaseLoggingClass : ICommon
	{
		public event Action<string, string> OnWriteToLog;
		public event Action<IClose> OnClose;
        public event Action<IResetWithEvent> OnReset;

        private string _logPrefix;

        protected void OverrideLogPrefix(string prefix)
        {
            _logPrefix = "[" + prefix + "] ";
        }

        private void SetLogPrefix()
        {
            _logPrefix = "[" + GetType().Name + "] ";
        }

        public bool IsClosed { get; protected set; }

		public virtual void Close()
		{
			if (IsClosed) return;
			IsClosed = true;
			OnClose?.Invoke(this);
		}

		public virtual void Update(ulong tick) { }

		public virtual void WriteGeneral(string caller, string message)
		{
			if(string.IsNullOrEmpty(_logPrefix))
                SetLogPrefix();
            OnWriteToLog?.Invoke($"{_logPrefix}{caller}", message);
		}

        public virtual void Reset()
        {
            OnReset?.Invoke(this);
        }
    }
} 