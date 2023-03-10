using UnityEngine;

namespace Player.Commands
{
	public class TakeItem : IPlayerAction
	{
		public PlayerController.PlayerState State => PlayerController.PlayerState.TakeItem;
		public bool Failed => false;
		private readonly ItemInteraction _item;
		private readonly VariableSystem _variableSystem;
		private readonly PlayerController _player;

		private float _timer;
		private bool _completed;
		private bool _started;

		public TakeItem(VariableSystem variableSystem, PlayerController player, ItemInteraction item)
		{
			_variableSystem = variableSystem;
			_player = player;
			_item = item;
		}
		
		public bool Completed()
		{
			return _completed;
		}

		public bool Update()
		{
			if (!_started)
			{
				_started = true;
                AudioManager.instance.PlayOneShot(AudioManager.instance.events.grabItem, _player.transform.position);
            }
            _timer += Time.deltaTime;
			if (_timer > 1f)
			{
				_item.Interact(_variableSystem);
				_completed = true;
			}

			return true;
		}
	}
}