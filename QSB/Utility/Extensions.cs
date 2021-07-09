using OWML.Common;
using OWML.Utils;
using QSB.Player.TransformSync;
using QuantumUNET;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace QSB.Utility
{
	public static class Extensions
	{
		// UNITY
		public static void Show(this GameObject gameObject) => SetVisibility(gameObject, true);

		public static void Hide(this GameObject gameObject) => SetVisibility(gameObject, false);

		private static void SetVisibility(GameObject gameObject, bool isVisible)
		{
			var renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (var renderer in renderers)
			{
				renderer.enabled = isVisible;
			}
		}

		public static Quaternion TransformRotation(this Transform transform, Quaternion localRotation)
			=> transform.rotation * localRotation;

		public static GameObject InstantiateInactive(this GameObject original)
		{
			original.SetActive(false);
			var copy = UnityEngine.Object.Instantiate(original);
			original.SetActive(true);
			return copy;
		}

		public static Transform InstantiateInactive(this Transform original) =>
			original.gameObject.InstantiateInactive().transform;

		// QNET
		public static uint GetPlayerId(this QNetworkConnection connection)
		{
			if (connection == null)
			{
				DebugLog.ToConsole($"Error - Trying to get player id of null QNetworkConnection.", MessageType.Error);
				return uint.MaxValue;
			}

			var playerController = connection.PlayerControllers[0];
			if (playerController == null)
			{
				DebugLog.ToConsole($"Error - Player Controller of {connection.address} is null.", MessageType.Error);
				return uint.MaxValue;
			}

			var go = playerController.Gameobject;
			if (go == null)
			{
				DebugLog.ToConsole($"Error - GameObject of {playerController.UnetView.NetId.Value} is null.", MessageType.Error);
				return uint.MaxValue;
			}

			var controller = go.GetComponent<PlayerTransformSync>();
			if (controller == null)
			{
				DebugLog.ToConsole($"Error - No PlayerTransformSync found on {go.name}", MessageType.Error);
				return uint.MaxValue;
			}

			return controller.NetId.Value;
		}

		// C#
		public static void SafeInvoke(this MulticastDelegate multicast, params object[] args)
		{
			foreach (var del in multicast.GetInvocationList())
			{
				try
				{
					del.DynamicInvoke(args);
				}
				catch (TargetInvocationException ex)
				{
					DebugLog.ToConsole($"Error invoking delegate! Message : {ex.InnerException.Message} Stack Trace : {ex.InnerException.StackTrace}", MessageType.Error);
				}
			}
		}

		public static float Map(this float value, float inputFrom, float inputTo, float outputFrom, float outputTo)
			=> ((value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom)) + outputFrom;

		public static void ForEach<T>(this System.Collections.Generic.IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (var item in enumerable)
			{
				action(item);
			}
		}

		private const BindingFlags Flags = BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.DeclaredOnly;

		public static void RaiseEvent<T>(this T instance, string eventName, params object[] args)
		{
			if (!(typeof(T)
				.GetField(eventName, Flags)?
				.GetValue(instance) is MulticastDelegate multiDelegate))
			{
				return;
			}

			multiDelegate.GetInvocationList().ToList().ForEach(dl => dl.DynamicInvoke(args));
		}

		public static void CallBase<ThisType, BaseType>(this ThisType obj, string methodName)
			where ThisType : BaseType
		{
			var method = typeof(BaseType).GetAnyMethod(methodName);
			if (method == null)
			{
				DebugLog.ToConsole($"Error - Couldn't find method {methodName} in {typeof(BaseType).FullName}!", MessageType.Error);
				return;
			}

			var functionPointer = method.MethodHandle.GetFunctionPointer();
			if (functionPointer == null)
			{
				DebugLog.ToConsole($"Error - Function pointer for {methodName} in {typeof(BaseType).FullName} is null!", MessageType.Error);
				return;
			}

			var methodAction = (Action)Activator.CreateInstance(typeof(Action), obj, functionPointer);
			methodAction();
		}

		// OW

		public static Vector3 GetRelativeAngularVelocity(this OWRigidbody baseBody, OWRigidbody relativeBody)
			=> baseBody.GetAngularVelocity() - relativeBody.GetAngularVelocity();
	}
}
