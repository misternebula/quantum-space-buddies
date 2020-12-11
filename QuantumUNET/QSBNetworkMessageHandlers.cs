﻿using QuantumUNET.Messages;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumUNET
{
	internal class QSBNetworkMessageHandlers
	{
		private Dictionary<short, QSBNetworkMessageDelegate> _msgHandlers = new Dictionary<short, QSBNetworkMessageDelegate>();

		internal void RegisterHandlerSafe(short msgType, QSBNetworkMessageDelegate handler)
		{
			if (handler == null)
			{
				Debug.LogError("RegisterHandlerSafe id:" + msgType + " handler is null");
			}
			else
			{
				Debug.Log(string.Concat(new object[]
				{
					"RegisterHandlerSafe id:",
					msgType,
					" handler:",
					handler.GetMethodName()
				}));
				if (!_msgHandlers.ContainsKey(msgType))
				{
					_msgHandlers.Add(msgType, handler);
				}
			}
		}

		public void RegisterHandler(short msgType, QSBNetworkMessageDelegate handler)
		{
			if (handler == null)
			{
				Debug.LogError("RegisterHandler id:" + msgType + " handler is null");
			}
			else if (msgType <= 31)
			{
				Debug.LogError("RegisterHandler: Cannot replace system message handler " + msgType);
			}
			else
			{
				if (_msgHandlers.ContainsKey(msgType))
				{
					Debug.Log("RegisterHandler replacing " + msgType);
					_msgHandlers.Remove(msgType);
				}
				Debug.Log(string.Concat(new object[]
				{
					"RegisterHandler id:",
					msgType,
					" handler:",
					handler.GetMethodName()
				}));
				_msgHandlers.Add(msgType, handler);
			}
		}

		public void UnregisterHandler(short msgType) => _msgHandlers.Remove(msgType);

		internal QSBNetworkMessageDelegate GetHandler(short msgType)
		{
			QSBNetworkMessageDelegate result;
			if (_msgHandlers.ContainsKey(msgType))
			{
				result = _msgHandlers[msgType];
			}
			else
			{
				result = null;
			}
			return result;
		}

		internal Dictionary<short, QSBNetworkMessageDelegate> GetHandlers() => _msgHandlers;

		internal void ClearMessageHandlers() => _msgHandlers.Clear();
	}
}