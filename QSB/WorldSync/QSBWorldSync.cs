using OWML.Common;
using QSB.OrbSync.TransformSync;
using QSB.OrbSync.WorldObjects;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace QSB.WorldSync
{
	public static class QSBWorldSync
	{
		public static List<NomaiInterfaceOrb> OldOrbList { get; set; } = new List<NomaiInterfaceOrb>();
		public static List<CharacterDialogueTree> OldDialogueTrees { get; set; } = new List<CharacterDialogueTree>();
		public static Dictionary<string, bool> DialogueConditions { get; } = new Dictionary<string, bool>();
		public static List<FactReveal> ShipLogFacts { get; } = new List<FactReveal>();

		private static readonly List<IWorldObject> WorldObjects = new List<IWorldObject>();
		private static readonly Dictionary<MonoBehaviour, IWorldObject> WorldObjectsToUnityObjects = new Dictionary<MonoBehaviour, IWorldObject>();

		public static IEnumerable<TWorldObject> GetWorldObjects<TWorldObject>()
			=> WorldObjects.OfType<TWorldObject>();

		public static TWorldObject GetWorldFromId<TWorldObject>(int id)
		{
			if (id < 0 || id >= GetWorldObjects<TWorldObject>().Count())
			{
				DebugLog.ToConsole($"Warning - Tried to find {typeof(TWorldObject).Name} id {id}. Count is {GetWorldObjects<TWorldObject>().Count()}.", MessageType.Warning);
				return default;
			}

			return GetWorldObjects<TWorldObject>().ToList()[id];
		}

		public static TWorldObject GetWorldFromUnity<TWorldObject, TUnityObject>(TUnityObject unityObject)
			where TWorldObject : WorldObject<TUnityObject>
			where TUnityObject : MonoBehaviour
		{
			if (unityObject == null)
			{
				DebugLog.ToConsole($"Error - Trying to run GetWorldFromUnity with a null unity object! TWorldObject:{typeof(TWorldObject).Name}, TUnityObject:{typeof(TUnityObject).Name}.", MessageType.Error);
				return default;
			}

			if (!QSBCore.IsInMultiplayer)
			{
				DebugLog.ToConsole($"Warning - Trying to run GetWorldFromUnity while not in multiplayer!");
				return default;
			}

			if (!WorldObjectsToUnityObjects.ContainsKey(unityObject))
			{
				DebugLog.ToConsole($"Error - WorldObjectsToUnityObjects does not contain \"{unityObject.name}\"! Called from {new StackTrace().GetFrame(1).GetMethod().Name}", MessageType.Error);
				return default;
			}

			return WorldObjectsToUnityObjects[unityObject] as TWorldObject;
		}

		public static int GetIdFromUnity<TWorldObject, TUnityObject>(TUnityObject unityObject)
			where TWorldObject : WorldObject<TUnityObject>
			where TUnityObject : MonoBehaviour
			=> GetWorldFromUnity<TWorldObject, TUnityObject>(unityObject).ObjectId;

		public static int GetIdFromTypeSubset<TTypeSubset>(TTypeSubset typeSubset)
		{
			var index = GetWorldObjects<TTypeSubset>().ToList().IndexOf(typeSubset);
			if (index == -1)
			{
				DebugLog.ToConsole($"Warning - {(typeSubset as IWorldObject).Name} doesn't exist in list of {typeof(TTypeSubset).Name} !", MessageType.Warning);
			}

			return index;
		}

		public static void RemoveWorldObjects<TWorldObject>()
		{
			var itemsToRemove = WorldObjects.Where(x => x is TWorldObject);
			foreach (var item in itemsToRemove)
			{
				WorldObjectsToUnityObjects.Remove(item.ReturnObject());
				try
				{
					item.OnRemoval();
				}
				catch (Exception e)
				{
					DebugLog.ToConsole($"Error - Exception in OnRemoval() for {item.GetType()}. Message : {e.InnerException.Message}, Stack trace : {e.InnerException.StackTrace}", MessageType.Error);
				}
			}

			WorldObjects.RemoveAll(x => x is TWorldObject);
		}

		public static List<TUnityObject> Init<TWorldObject, TUnityObject>()
			where TWorldObject : WorldObject<TUnityObject>
			where TUnityObject : MonoBehaviour
		{
			RemoveWorldObjects<TWorldObject>();
			var list = Resources.FindObjectsOfTypeAll<TUnityObject>().ToList();
			DebugLog.DebugWrite($"{typeof(TWorldObject).Name} init : {list.Count} instances.", MessageType.Info);
			for (var id = 0; id < list.Count; id++)
			{
				var obj = CreateWorldObject<TWorldObject>();
				obj.Init(list[id], id);
				WorldObjectsToUnityObjects.Add(list[id], obj);
			}

			return list;
		}

		private static TWorldObject CreateWorldObject<TWorldObject>()
			where TWorldObject : IWorldObject
		{
			var worldObject = (TWorldObject)Activator.CreateInstance(typeof(TWorldObject));
			WorldObjects.Add(worldObject);
			return worldObject;
		}

		public static void HandleSlotStateChange(NomaiInterfaceSlot slot, NomaiInterfaceOrb affectingOrb, bool state)
		{
			var slotList = GetWorldObjects<QSBOrbSlot>().ToList();
			if (!slotList.Any())
			{
				return;
			}

			var qsbSlot = slotList.FirstOrDefault(x => x.AttachedObject == slot);
			if (qsbSlot == null)
			{
				DebugLog.ToConsole($"Error - No QSBOrbSlot found for {slot.name}!", MessageType.Error);
				return;
			}

			var orbSync = NomaiOrbTransformSync.OrbTransformSyncs.FirstOrDefault(x => x.AttachedObject == affectingOrb.transform);
			if (orbSync == null)
			{
				DebugLog.ToConsole($"Error - No NomaiOrbTransformSync found for {affectingOrb.name} (For slot {slot.name})!", MessageType.Error);
				return;
			}

			if (orbSync.HasAuthority)
			{
				qsbSlot.HandleEvent(state, OldOrbList.IndexOf(affectingOrb));
			}
		}

		public static void SetDialogueCondition(string name, bool state)
		{
			if (!QSBCore.IsServer)
			{
				DebugLog.ToConsole("Warning - Cannot write to condition dict when not server!", MessageType.Warning);
				return;
			}

			DialogueConditions[name] = state;
		}

		public static void AddFactReveal(string id, bool saveGame, bool showNotification)
		{
			if (!QSBCore.IsServer)
			{
				DebugLog.ToConsole("Warning - Cannot write to fact list when not server!", MessageType.Warning);
				return;
			}

			if (ShipLogFacts.Any(x => x.Id == id))
			{
				return;
			}

			ShipLogFacts.Add(new FactReveal
			{
				Id = id,
				SaveGame = saveGame,
				ShowNotification = showNotification
			});
		}
	}
}