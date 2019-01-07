using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Server
{
	public class PropHunt : ServerAccessor
	{
		private Random Random { get; } = new Random();

		private readonly List<MapModel> _mapRotation = new List<MapModel>();

		public ServerConfigModel Config { get; private set; }
		public GameDataModel GameState { get; private set; }

		public HashSet<int> ReadyPlayers { get; } = new HashSet<int>();

		protected internal PropHunt( Server server ) : base( server ) {
			Log.Verbose( "Loading Configuration file" );

			if( !LoadConfig() ) {
				API.StopResource( API.GetCurrentResourceName() );
				return;
			}

			Server.RegisterEventHandler( "Session.Loaded", new Action<int>( OnSessionLoad ) );
			Server.RegisterEventHandler( "Player.DropMe", new Action<CitizenFX.Core.Player, string>( OnDropRequest ) );
			Server.RegisterEventHandler( "PropHunt.Purchase", new Action<CitizenFX.Core.Player, string>( OnPurchase ) );
			Server.RegisterEventHandler( "PropHunt.SwitchModel", new Action<CitizenFX.Core.Player, string>( OnModelSwitch ) );
			Server.RegisterEventHandler( "PropHunt.Ready", new Action<CitizenFX.Core.Player>( OnReady ) );
			Server.RegisterEventHandler( "PropHunt.Dead", new Action<CitizenFX.Core.Player, int>( OnDeath ) );
			Server.RegisterEventHandler( "playerDropped", new Action<CitizenFX.Core.Player, string>( OnDrop ) );
			Server.RegisterEventHandler( "PropHunt.Prop", new Action<CitizenFX.Core.Player, int, uint>( OnPropUpdate ) );

			Server.RegisterTickHandler( GameTick );
		}

		private void OnPropUpdate( [FromSource] CitizenFX.Core.Player source, int netId, uint hash ) {
			try {
				BaseScript.TriggerClientEvent( "PropHunt.Prop", source.Handle, netId, hash );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnDrop( [FromSource] CitizenFX.Core.Player source, string reason ) {
			try {
				var netId = int.Parse( source.Handle );
				if( GameState != null && GameState.Players.Any( p => p.NetId == netId && p.IsAlive ) )
					Kill( netId );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnDeath( [FromSource] CitizenFX.Core.Player source, int killerId ) {
			try {
				var session = Server.Sessions.FromPlayer( source );
				if( session == null ) return;

				Kill( session.NetId );

				var killer = Server.Sessions.FromPlayer( killerId );

				var killerData = GameState?.Players.FirstOrDefault( p => p.NetId == killerId );

				if( killerData != null )
					killerData.Kills++;

				var propCount = GameState?.Players.Count( p => p.Team == Team.Prop && p.IsAlive );
				BaseScript.TriggerClientEvent( "PropHunt.Died", session.NetId, $"~r~{session.Name}~s~ {(killerData != null ? $"was killed by ~r~{killer.Name}~s~!" : "has died.")}~n~~r~{propCount}~s~ prop{(propCount == 1 ? "" : "s")} left!" );
				Log.Info( $"Player {session.Name} (net:{session.NetId}) {(killerData == null ? "has died" : $"was killed by {killer.Name}")}" );

				session.TriggerEvent( "PropHunt.Sound", "death" );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void Kill( int netId ) {
			var playerData = GameState?.Players.FirstOrDefault( p => p.NetId == netId );

			if( playerData == null || !playerData.IsAlive ) return;

			playerData.IsAlive = false;
			playerData.SurvivalTime = (int)Math.Floor( MathUtil.Clamp( Config.RoundTime - (float)(GameState.GameEndsAt - DateTime.UtcNow).TotalSeconds, 0, Config.RoundTime ) );
		}

		private void OnReady( [FromSource] CitizenFX.Core.Player source ) {
			try {
				var session = Server.Sessions.FromPlayer( source );
				if( session == null || ReadyPlayers.Contains( session.NetId ) ) return;
				ReadyPlayers.Add( session.NetId );
				Log.Verbose( $"Player {session.Name} is ready." );
				source.TriggerEvent( "UI.ShowNotification", "You have been marked as ~g~Ready~s~.~n~Currently waiting for more ready players." );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async Task GameTick() {
			try {
				if( GameState == null ) {
					ChangeMap( GetNextMap() );
				}

				if( !GameState.HasStarted && DateTime.UtcNow >= GameState.GameEndsAt ) {
					var ratio = 1 - Config.PropHunterRatio;
					var playerCount = 0;
					var players = ReadyPlayers.Where( s => Server.Sessions.FromPlayer( s ) != null )
						.OrderBy( p => Random.Next( -1, 1 ) )
						.Select( s => new PlayerDataModel {
							NetId = s,
							IsAlive = true,
							Team = playerCount++ * ratio % 1 <= 0 ? Team.Hunter : Team.Prop
						} ).ToList();

					if( playerCount < Config.MinPlayers ) {
						GameState.GameEndsAt = DateTime.UtcNow.AddSeconds( Config.WaitTime );
						var min = Config.MinPlayers - playerCount;
						BaseScript.TriggerClientEvent( "Lobby.ShowNotification", $"Currently waiting for {min} more player{(min != 1 ? "s" : "")}.", 3 );
						return;
					}

					GameState.Players = players;
					GameState.GameEndsAt = DateTime.UtcNow.AddSeconds( Config.RoundTime );
					GameState.TimeLeft = Config.RoundTime;
					GameState.HasStarted = true;
					BaseScript.TriggerClientEvent( "PropHunt.GameState", JsonConvert.SerializeObject( GameState ) );
					Log.Info( $"Game has started with {players.Count( p => p.Team == Team.Prop )} props and {players.Count( p => p.Team == Team.Hunter )} hunters! Map: {GameState.Map}, Time Left: {GameState.TimeLeft} seconds" );
				}

				if( !GameState.HasStarted ) {
					return;
				}

				var alivePropCount = GameState.Players.Count( p => p.Team == Team.Prop && p.IsAlive );
				var aliveHunterCount = GameState.Players.Count( p => p.Team == Team.Hunter && p.IsAlive );
				if( alivePropCount <= 0 || aliveHunterCount <= 0 || DateTime.UtcNow >= GameState.GameEndsAt ) {
					await EndMap( alivePropCount > 0 ? Team.Prop : Team.Hunter );
					return;
				}

				if( GameState.IsHiding && (GameState.GameEndsAt - DateTime.UtcNow).TotalSeconds < Config.RoundTime - Config.HideTime ) {
					GameState.IsHiding = false;
					BaseScript.TriggerClientEvent( "PropHunt.GameState", JsonConvert.SerializeObject( GameState ) );
					Log.Info( "The hunters have been released!" );
					BaseScript.TriggerClientEvent( "Lobby.ShowNotification", @"The <span style=""color: #ff0000ad; font-weight: bold;"">Hunters</span> have been released!", 4 );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
			await BaseScript.Delay( 1000 );
		}

		private async Task EndMap( Team winningTeam ) {
			try {
				if( GameState == null ) return;
				Log.Info( $"Ending map - Team {Enum.GetName( typeof( Team ), winningTeam )} Wins!" );

				var points = 0u;
				var playerCount = 0;
				foreach( var player in GameState.Players ) {
					var session = Server.Sessions.FromPlayer( player.NetId );
					if( session == null ) continue;

					var reward = 0f;
					if( player.Team == Team.Prop ) {
						reward += MathUtil.Clamp( Config.SurviveReward * player.SurvivalTime, 0, Config.SurviveReward * Config.RoundTime );
						if( player.IsAlive ) {
							reward += Config.AliveReward;
						}
					}

					if( player.Team == Team.Hunter ) {
						reward += Config.KillReward * player.Kills;
					}

					if( player.Team == winningTeam ) {
						reward += Config.WinReward;
					}

					var amount = (uint)Math.Max( Math.Floor( reward ), 0 );
					session.User.Data.Points += amount;
					points += amount;
					playerCount++;
					await session.SaveData();
					session.TriggerEvent( "PropHunt.UserData", JsonConvert.SerializeObject( session.User.Data ) );
					session.TriggerEvent( "UI.ShowNotification", $"Your team {(winningTeam == player.Team ? "~g~Won" : "~r~Lost")}~s~! You've earned ~y~{amount:n0} points~s~." );
				}

				BaseScript.TriggerClientEvent( "PropHunt.End", (int)winningTeam );
				Log.Info( $"Rewarded a total of {points:n0} to {playerCount:n0} players." );
				ChangeMap( GetNextMap() );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnModelSwitch( [FromSource] CitizenFX.Core.Player source, string model ) {
			try {

				var session = Server.Sessions.FromPlayer( source );
				if( session == null ) return;

				var user = session.User.Data;
				var target = Config.Characters.FirstOrDefault( c => c.ModelName.Equals( model, StringComparison.InvariantCultureIgnoreCase ) );

				if( target == null ) {
					Log.Warn( $"Player {session.Name} (net:{session.NetId}) tried purchasing an invalid model: {model}" );
					return;
				}

				if( !target.Unlocked && !user.Unlockables.Contains( target.ModelName ) ) {
					Log.Warn( $"Player {session.Name} (net:{session.NetId}) tried switching to a model they don't own: {model}" );
					return;
				}

				user.PedModel = target.ModelName;
				if( await session.SaveData() ) {
					session.TriggerEvent( "PropHunt.UserData", JsonConvert.SerializeObject( user ) );
					Log.Verbose( $"Player {session.Name} switched to model {model}" );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnPurchase( [FromSource] CitizenFX.Core.Player source, string item ) {
			try {
				var session = Server.Sessions.FromPlayer( source );
				if( session == null ) return;

				var user = session.User.Data;
				var target = Config.Characters.FirstOrDefault( c => c.ModelName.Equals( item, StringComparison.InvariantCultureIgnoreCase ) );

				if( target == null ) {
					Log.Warn( $"Player {session.Name} (net:{session.NetId}) tried purchasing an invalid item: {item}" );
					return;
				}

				if( target.PointCost > user.Points ) {
					Log.Warn( $"Player {session.Name} (net:{session.NetId}) tried purchasing {item} ({target.PointCost}p) with only {user.Points} points." );
					return;
				}

				if( user.Unlockables.Contains( target.ModelName ) ) {
					return;
				}

				user.Points = Math.Max( 0, user.Points - target.PointCost );
				user.Unlockables.Add( target.ModelName );
				if( await session.SaveData() ) {
					session.TriggerEvent( "PropHunt.UserData", JsonConvert.SerializeObject( user ) );
					session.TriggerEvent( "PropHunt.Sound", "purchase" );
					Log.Verbose(
						$"Player {session.Name} purchased {target.ModelName} for {target.PointCost:n0}. Balance: {user.Points:n0}" );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private bool LoadConfig() {
			try {
				var file = API.LoadResourceFile( API.GetCurrentResourceName(), "config.json" );
				Config = JsonConvert.DeserializeObject<ServerConfigModel>( file );
				BaseScript.TriggerClientEvent( "PropHunt.Config", JsonConvert.SerializeObject( Config ) );
			}
			catch( Exception ) {
				Log.Error( $"Could not read resources/{API.GetCurrentResourceName()}/config.json -- Make sure the syntax is valid JSON." );
				return false;
			}
			return Config != null;
		}

		private void OnDropRequest( [FromSource] CitizenFX.Core.Player source, string reason ) {
			try {
				source.Drop( reason );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnSessionLoad( int netId ) {
			try {
				var session = Server.Sessions.FromPlayer( netId );
				if( session == null ) return;

				if( !await session.LoadData() ) {
					Log.Warn( $"Could not load {session.Player.Name}'s data" );
					session.Drop( "Error loading user data -- Contact Server Owner." );
					return;
				}

				Log.Info( $"Loaded player data from SteamID {session.SteamIdentifier}." );

				session.TriggerEvent( "PropHunt.Config", JsonConvert.SerializeObject( Config ) );
				session.TriggerEvent( "PropHunt.UserData", JsonConvert.SerializeObject( session.User.Data ) );

				session.TriggerEvent( "PropHunt.GameState", JsonConvert.SerializeObject( new GameDataModel( GameState ) {
					TimeLeft = GameState.HasStarted ? (int)Math.Floor( (GameState.GameEndsAt - DateTime.UtcNow).TotalSeconds ) : GameState.TimeLeft
				} ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public MapModel GetNextMap() {
			if( !_mapRotation.Any() ) {
				_mapRotation.AddRange( Config.Maps );
				_mapRotation.Sort( ( a, b ) => Random.Next( -1, 1 ) );
			}

			var first = _mapRotation.First();
			_mapRotation.Remove( first );
			return first;
		}

		public void ChangeMap( MapModel model ) {
			ReadyPlayers.Clear();
			GameState = new GameDataModel {
				HasStarted = false,
				Map = model.Name,
				TimeLeft = Config.RoundTime,
				GameEndsAt = DateTime.UtcNow.AddSeconds( Config.WaitTime ),
				Players = new List<PlayerDataModel>()
			};
			BaseScript.TriggerClientEvent( "PropHunt.GameState", JsonConvert.SerializeObject( GameState ) );
			Log.Verbose( $"Map changed to {model.Name}." );
		}
	}
}
