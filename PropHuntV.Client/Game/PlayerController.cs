using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PropHuntV.Util;

namespace PropHuntV.Client.Game
{
	public class PlayerController : ClientAccessor
	{
		public Ped PlayerPed { get; private set; }

		private Vector3 _pos = Vector3.Zero;
		/// <summary>
		/// The position of the player character.
		/// </summary>
		public Vector3 Position
		{
			get => _pos;
			set {
				PlayerPed.Position = value;
				_pos = value;
			}
		}

		private int _voiceChannel;
		/// <summary>
		/// The voice channel the player is in.
		/// </summary>
		public int VoiceChannel
		{
			get => _voiceChannel;
			set {
				if( value <= 0 ) {
					Function.Call( Hash.NETWORK_CLEAR_VOICE_CHANNEL );
				}
				else {
					Function.Call( Hash.NETWORK_SET_VOICE_CHANNEL, _voiceChannel );
				}
				_voiceChannel = Math.Max( 0, value );
			}
		}

		private int _serverId;
		public int ServerId => _serverId;

		private float _speed = 1f;
		public float PlayerSpeed
		{
			get => _speed;
			set => _speed = MathUtil.Clamp( value, 1f, 1.5f );
		}

		public bool InfiniteStamina { get; set; }

		public bool DisablePistolWhipping { get; set; }

		public PlayerController( Client client ) : base( client ) {
			client.RegisterTickHandler( OnTick );
		}

		private async Task OnTick() {
			try {
				PlayerPed = CitizenFX.Core.Game.PlayerPed;
				_pos = PlayerPed.Position;
				_serverId = CitizenFX.Core.Game.Player.ServerId;

				API.SetRunSprintMultiplierForPlayer( PlayerPed.Handle, _speed );
				CitizenFX.Core.Game.Player.SetRunSpeedMultThisFrame( _speed );

				if( InfiniteStamina ) {
					API.RestorePlayerStamina( CitizenFX.Core.Game.Player.Handle, 1f );
				}

				if( DisablePistolWhipping && API.IsPedArmed( PlayerPed.Handle, 6 ) ) {
					Function.Call( Hash.DISABLE_CONTROL_ACTION, 1, 140, true );
					Function.Call( Hash.DISABLE_CONTROL_ACTION, 1, 141, true );
					Function.Call( Hash.DISABLE_CONTROL_ACTION, 1, 142, true );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
		}
	}
}
