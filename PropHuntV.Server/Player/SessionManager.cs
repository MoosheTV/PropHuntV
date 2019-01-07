using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Newtonsoft.Json;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Server.Player
{
	public class SessionManager : ServerAccessor
	{
		private readonly List<Session> _sessions = new List<Session>();

		/// <summary>
		/// A thread-safe, readonly collection of the current active Sessions.
		/// </summary>
		public IReadOnlyList<Session> SessionList => new List<Session>( _sessions );

		public SessionManager( Server server ) : base( server ) {
			server.RegisterEventHandler( "playerConnecting", new Action<CitizenFX.Core.Player>( OnSessionPreLoad ) );
			server.RegisterEventHandler( "playerDropped", new Action<CitizenFX.Core.Player, string>( OnPlayerDropped ) );
			server.RegisterEventHandler( "Session.Load", new Action<CitizenFX.Core.Player>( OnSessionLoad ) );
		}

		/// <summary>
		/// Gets the session associated with this player.
		/// </summary>
		/// <param name="source">The player source.</param>
		/// <returns>The session associated with this player, or null, if there is no session associated.</returns>
		public Session FromPlayer( CitizenFX.Core.Player source ) {
			return _sessions.FirstOrDefault( s => s.Player.Handle == source.Handle );
		}

		public Session FromPlayer( int netId ) {
			return _sessions.FirstOrDefault( s => s.NetId == netId );
		}

		private Session FromIdentifiers( IdentifierCollection identifiers ) {
			return _sessions.FirstOrDefault( p => p.Player.Identifiers.All( identifiers.Contains ) );
		}

		private void OnPlayerDropped( [FromSource] CitizenFX.Core.Player player, string reason ) {
			try {
				var session = FromIdentifiers( player.Identifiers );
				if( session == null ) {
					Log.Warn( $"No player found with identifiers ({string.Join( ", ", player.Identifiers )}) from dropped session." );
					return;
				}

				_sessions.Remove( session );

				Log.Info( $"Player {session.Name} (net:{session.NetId}) has disconnected. ({reason})" );
				BaseScript.TriggerClientEvent( "Session.Drop", session.NetId );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionLoad( [FromSource] CitizenFX.Core.Player player ) {
			try {
				var session = FromIdentifiers( player.Identifiers );
				if( session == null ) {
					Log.Warn( $"Could not find pre-existing session from {player.Name} (net:{player.Handle}, {string.Join( ", ", player.Identifiers )}). Creating new." );
					session = new Session( player );
					_sessions.Add( session );
				}
				else {
					session.Player = player;
				}

				foreach( var p in new PlayerList() ) {
					var data = new SessionDataModel {
						Name = session.Name,
						NetId = session.NetId,
						SharedData = session.SharedData
					};
					if( p.Handle == player.Handle ) {
						data.ProtectedData = session.ProtectedData;
					}
					p.TriggerEvent( "Session.Join", session.NetId, JsonConvert.SerializeObject( data ) );
				}
				player.TriggerEvent( "Session.Populate", JsonConvert.SerializeObject( _sessions.Where( s => s.NetId != session.NetId ).Select( s => new SessionDataModel {
					Name = s.Name,
					NetId = s.NetId,
					SharedData = s.SharedData
				} ).ToList() ) );
				player.TriggerEvent( "Session.Loaded" );
				BaseScript.TriggerEvent( "Session.Loaded", session.NetId );

				Log.Verbose( $"Player {session.Name} (net:{session.NetId}) has loaded into the server." );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionPreLoad( [FromSource] CitizenFX.Core.Player player ) {
			try {
				var session = new Session( player );
				Log.Info( $"Player {player.Name} ({string.Join( ", ", player.Identifiers )}) is attempting to connect to the server." );
				if( Server.IsSteamRequired && session.SteamId64 == 0 ) {
					player.Drop( "Steam is required to play on this server." );
					return;
				}

				_sessions.Add( session );

				BaseScript.TriggerEvent( "Session.PreLoad", session.NetId );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
