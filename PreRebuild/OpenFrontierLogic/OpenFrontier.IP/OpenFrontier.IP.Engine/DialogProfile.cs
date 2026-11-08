using System.Collections.Generic;
using OpenFrontier.IP.Engine.Dialog;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine
{
	public class DialogProfile : MonoBehaviour, IUnique
	{
		[FormerlySerializedAs("EventHandlers")]
		public List<DialogEventHandlerLegacy> LegacyEventHandlers = new List<DialogEventHandlerLegacy>();

		private Dictionary<int, CompiledDialogEventHandler> handlerMap = new Dictionary<int, CompiledDialogEventHandler>();

		[SerializeField]
		private int uniqueId;

		public int UniqueId
		{
			get
			{
				return uniqueId;
			}
			set
			{
				uniqueId = value;
			}
		}

		public void Init()
		{
			for (int i = 0; i < LegacyEventHandlers.Count; i++)
			{
				DialogEventHandlerLegacy dialogEventHandlerLegacy = LegacyEventHandlers[i];
				if (dialogEventHandlerLegacy != null && dialogEventHandlerLegacy.Event != null && dialogEventHandlerLegacy.Messages.Count > 0)
				{
					AddMessages(dialogEventHandlerLegacy.Event, dialogEventHandlerLegacy.Messages);
				}
			}
			DialogEventHandler[] componentsInChildren = GetComponentsInChildren<DialogEventHandler>();
			foreach (DialogEventHandler dialogEventHandler in componentsInChildren)
			{
				AddMessages(dialogEventHandler.Event, dialogEventHandler.Messages);
			}
		}

		public void AddMessages(DialogEvent dialogEvent, IEnumerable<string> messages)
		{
			if (!handlerMap.TryGetValue(dialogEvent.UniqueId, out var value))
			{
				CompiledDialogEventHandler compiledDialogEventHandler = (handlerMap[dialogEvent.UniqueId] = new CompiledDialogEventHandler());
				value = compiledDialogEventHandler;
				value.DialogEvent = dialogEvent;
			}
			value.Messages.AddRange(messages);
		}

		public CompiledDialogEventHandler GetDialogEventHandler(DialogEvent dialogEvent)
		{
			if (dialogEvent == null)
			{
				Debug.LogError("Dialog event is null");
				return null;
			}
			if (handlerMap.TryGetValue(dialogEvent.UniqueId, out var value))
			{
				return value;
			}
			return null;
		}
	}
}
