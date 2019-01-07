using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Client
{
	public class SpectatorHandler : ClientAccessor
	{
		protected PropHunt PropHunt { get; }

		public bool IsEnabled
		{
			get => _currentPlayer == null;
			set {
				Log.Info( $"Spectate mode set to {value}" );
				if( !value ) {
					_currentPlayer = null;
					Client.Player.PlayerPed.Detach();
					Client.Player.PlayerPed.IsCollisionEnabled = true;
					API.NetworkSetInSpectatorMode( false, Client.Player.PlayerPed.Handle );
					API.NetworkSetOverrideSpectatorMode( false );
					return;
				}

				API.NetworkSetOverrideSpectatorMode( true );
				_currentPlayer = GetNextPlayer();
				if( _currentPlayer != null )
					API.NetworkSetInSpectatorMode( true, _currentPlayer.Character.Handle );
			}
		}

		private CitizenFX.Core.Player _currentPlayer;

		public SpectatorHandler( PropHunt ctx, Client client ) : base( client ) {
			PropHunt = ctx;
			Client.RegisterTickHandler( OnTick );
			Client.RegisterEventHandler( "PropHunt.Died", new Action<int, string>( ( id, str ) => {
				if( _currentPlayer?.ServerId == id ) {
					_currentPlayer = GetNextPlayer();
				}
			} ) );
		}

		private async Task OnTick() {
			try {
				if( !IsEnabled || (PropHunt.MapHandler.CurrentPlayer?.IsAlive ?? false) ) {
					return;
				}

				if( _currentPlayer != null && (_currentPlayer.Character.IsDead || _currentPlayer.Character.Health <= 0 ||
						PropHunt.MapHandler.GameState.Players.Any( p => p.NetId == _currentPlayer.ServerId && !p.IsAlive )) ) {
					_currentPlayer = GetNextPlayer();
				}

				if( CitizenFX.Core.Game.IsControlJustPressed( 2, Control.Attack ) ) {
					if( _currentPlayer != null ) {
						API.NetworkSetInSpectatorMode( false, Client.Player.PlayerPed.Handle );
					}
					_currentPlayer = GetNextPlayer();
					if( _currentPlayer != null ) {
						API.NetworkSetInSpectatorMode( true, _currentPlayer.Character.Handle );
					}
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
		}

		private CitizenFX.Core.Player GetNextPlayer() {
			var team = PropHunt.MapHandler.CurrentPlayer?.Team ?? Team.None;
			var players = PropHunt.MapHandler.GameState?.Players;
			if( players == null ) return null;

			var order = players.OrderBy( p => p.NetId ).Where( p => p.NetId != Client.Player.ServerId && p.IsAlive && (team == Team.None || p.Team == team) ).ToList();
			if( !order.Any() ) {
				return null;
			}

			var next = order.FirstOrDefault( p => p.NetId > (_currentPlayer?.ServerId ?? -1) ) ?? order.FirstOrDefault();

			var player = new PlayerList().FirstOrDefault( p => p.ServerId == (next?.NetId ?? -1024) );
			if( player != null ) {
				Client.Player.PlayerPed.Detach();
				Client.Player.PlayerPed.AttachTo( player.Character, new Vector3( 0f, 0f, -10f ) );
				Client.Player.PlayerPed.IsVisible = false;
			}

			Log.Info( $"Spectating {player?.Name ?? "Null"}" );
			return player;
		}
	}
}
