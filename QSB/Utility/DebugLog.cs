﻿using OWML.Common;
using System.Diagnostics;
using System.Linq;

namespace QSB.Utility
{
	public static class DebugLog
	{
		public static void ToConsole(string message, MessageType type = MessageType.Message)
			=> QSBCore.Helper.Console.WriteLine(message, type, GetCallingType(new StackTrace()));

		public static void ToHud(string message)
		{
			if (Locator.GetPlayerBody() == null)
			{
				return;
			}
			var data = new NotificationData(NotificationTarget.Player, message.ToUpper());
			NotificationManager.SharedInstance.PostNotification(data);
		}

		public static void ToAll(string message, MessageType type = MessageType.Message)
		{
			ToConsole(message, type);
			ToHud(message);
		}

		public static void DebugWrite(string message, MessageType type = MessageType.Message)
		{
			if (QSBCore.DebugMode)
			{
				ToConsole(message, type);
			}
		}

		private static string GetCallingType(StackTrace frame)
		{
			var stackFrame = frame.GetFrames().First(x => x.GetMethod().DeclaringType.Name != "DebugLog");
			return stackFrame.GetMethod().DeclaringType.Name;
		}
	}
}