using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PropHuntV.Client.Util;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Client
{
	internal class MapHandler : ClientAccessor
	{
		private IEnumerable<MapModel> Maps => PropHunt.Config?.Maps ?? new List<MapModel>();

		public MapModel CurrentMap => Maps.FirstOrDefault( m => m.Name.Equals( GameState?.Map ) );

		public GameDataModel GameState { get; private set; }

		public PlayerDataModel CurrentPlayer => GameState?.Players.FirstOrDefault( p => p.NetId == Client.Player.ServerId );

		private PropHunt PropHunt { get; }

		public Team LastTeam { get; private set; } = Team.None;

		private Area _currentBounds;

		protected internal MapHandler( PropHunt ctx, Client client ) : base( client ) {
			PropHunt = ctx;
			client.RegisterEventHandler( "PropHunt.GameState", new Action<string>( OnGameStateUpdate ) );
			client.RegisterEventHandler( "PropHunt.Died", new Action<int, string>( OnDeath ) );
		}

		private void OnDeath( int netId, string msg ) {
			try {
				var player = GameState?.Players.FirstOrDefault( p => p.NetId == netId );
				if( player != null )
					player.IsAlive = false;

				UiHelper.ShowNotification( msg );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public bool IsPlaying() {
			return GameState != null && GameState.HasStarted && GameState.Players.Any( p => p.NetId == Client.Player.ServerId && p.IsAlive );
		}

		private async void OnGameStateUpdate( string data ) {
			try {
				GameState = JsonConvert.DeserializeObject<GameDataModel>( data );

				var map = CurrentMap;
				if( map == null ) {
					Log.Warn( "Map is null when it shouldn't be" );
					return;
				}
				Client.Time.Hour = map.Hour;
				Client.Time.Freeze = true;
				Client.World.Weather = (WeatherType)map.Weather;

				_currentBounds = map.Bounds.Any() ? new Area( map.Bounds, map.MinZ, map.MaxZ ) : null;

				var player = GameState.Players.FirstOrDefault( p => p.NetId == Client.Player.ServerId );
				if( player == null ) return;
				LastTeam = player.Team;

				foreach( WeaponHash weapon in Enum.GetValues( typeof( WeaponHash ) ) ) {
					CitizenFX.Core.Game.PlayerPed.Weapons.Remove( weapon );
				}

				var spawnPoint = new Vector3( map.SpawnPoint );
				if( GameState.IsHiding ) {
					if( player.Team == Team.Hunter ) {
						spawnPoint += new Vector3( 0f, 0f, -5f );
						PropHunt.LobbyHandler.ShowNotification(
							@"You are the <span style=""color: #FF0000AD; font-weight: bold"">Hunter</span>.<br><br>
							When the screen fades in, <span style=""color: #FF0000AD; font-weight: bold"">Kill</span> the props which are hiding.", 8 );
					}
					else {
						PropHunt.LobbyHandler.ShowNotification(
							@"You are the <span style=""color: #52CEFFAD; font-weight: bold"">Prop</span>.<br><br>
							You can disguise yourself as a prop with <span style=""color: #52CEFFAD; font-weight: bold"">Right-Click</span>.", 8 );
					}

					PropHunt.LobbyHandler.SetCountdown( "Hunters Release", PropHunt.Config.HideTime );
					await PropHunt.SafeTeleport( spawnPoint, doFadeIn: player.Team != Team.Hunter );
					return;
				}

				PropHunt.LobbyHandler.SetCountdown( "Round Ends", GameState.TimeLeft - PropHunt.Config.HideTime );
				if( player.Team == Team.Hunter ) {
					await PropHunt.SafeTeleport( spawnPoint );
					PropHunt.GiveAllWeapons( PropHunt.Config.HuntWeapons );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public bool IsPlayerWithinBounds() {
			return _currentBounds == null || _currentBounds.Contains( Client.Player.Position );
		}
	}
}
