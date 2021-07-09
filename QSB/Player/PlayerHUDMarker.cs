using QSB.Utility;
using UnityEngine;

namespace QSB.Player
{
	public class PlayerHUDMarker : HUDDistanceMarker
	{
		private PlayerInfo _player;
		private bool _needsInitializing;
		private bool _isReady;

		protected override void InitCanvasMarker()
		{
			_markerRadius = 2f;

			_markerTarget = new GameObject().transform;
			_markerTarget.parent = transform;

			_markerTarget.localPosition = Vector3.up * 2;
		}

		public void Init(PlayerInfo player)
		{
			DebugLog.DebugWrite($"Init {player.PlayerId} name:{player.Name}");
			_player = player;
			_player.HudMarker = this;
			_needsInitializing = true;
		}

		private void Update()
		{
			if (_needsInitializing)
			{
				Initialize();
			}

			if (!_isReady || !_player.PlayerStates.IsReady)
			{
				return;
			}

			if (_canvasMarker != null)
			{
				var isVisible = _canvasMarker.IsVisible();

				if (_player.Visible != isVisible)
				{
					_canvasMarker.SetVisibility(_player.Visible);
				}
			}
			else
			{
				DebugLog.DebugWrite($"Warning - _canvasMarker for {_player.PlayerId} is null!", OWML.Common.MessageType.Warning);
			}
		}

		private void Initialize()
		{
			if (_player.Name == null)
			{
				DebugLog.ToConsole($"Error - {_player.PlayerId} has a null name!", OWML.Common.MessageType.Error);
				_player.Name = "NULL";
			}

			_markerLabel = _player.Name.ToUpper();
			_needsInitializing = false;
			_isReady = true;

			base.InitCanvasMarker();
		}

		public void Remove()
		{
			_isReady = false;
			// do N O T destroy the parent - it completely breaks the ENTIRE GAME
			if (_canvasMarker != null)
			{
				_canvasMarker.DestroyMarker();
			}

			if (_markerTarget != null)
			{
				Destroy(_markerTarget.gameObject);
			}

			Destroy(this);
		}
	}
}