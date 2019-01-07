using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Newtonsoft.Json;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Client.Player
{
	public class SessionManager : ClientAccessor
	{
		private readonly List<Session> _sessions = new List<Session>();

		/// <summary>
		/// A thread-safe, readonly collection of the current active Sessions.
		/// </summary>
		public IReadOnlyList<Session> SessionList => new List<Session>( _sessions );

		public SessionManager( Client client ) : base( client ) {
			client.RegisterEventHandler( "Session.Join", new Action<int, string>( OnSessionJoin ) );
			client.RegisterEventHandler( "Session.Drop", new Action<int>( OnSessionRemove ) );
			client.RegisterEventHandler( "Session.Populate", new Action<string>( OnSessionPopulate ) );
			client.RegisterEventHandler( "Session.UpdateName", new Action<int, string>( OnNameUpdate ) );
			client.RegisterEventHandler( "Session.UpdateSharedData", new Action<int, string>( OnSharedUpdate ) );
			client.RegisterEventHandler( "Session.UpdateProtectedData", new Action<int, string>( OnProtectedUpdate ) );

			BaseScript.TriggerServerEvent( "Session.Load" );
		}

		/// <summary>
		/// Gets the session from the given player server ID.
		/// </summary>
		/// <param name="serverId">The server ID associated with the session being looked for.</param>
		/// <returns>The session paired with that server ID, or null if it does not exist.</returns>
		public Session FromPlayer( int serverId ) {
			return _sessions.FirstOrDefault( p => p.NetId == serverId );
		}

		private void OnSharedUpdate( int netId, string data ) {
			try {
				var session = Client.Sessions.FromPlayer( netId );
				if( session == null ) return;

				session.UpdateSharedData( data );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnProtectedUpdate( int netId, string data ) {
			try {
				var session = Client.Sessions.FromPlayer( netId );
				if( session == null ) return;
				session.UpdateProtectedData( data );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionRemove( int serverId ) {
			try {
				var session = FromPlayer( serverId );
				if( session == null ) return;
				_sessions.Remove( session );

			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionPopulate( string data ) {
			try {
				var list = JsonConvert.DeserializeObject<List<SessionDataModel>>( data );
				foreach( var model in list ) {
					OnSessionJoinHandler( model );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionJoin( int netId, string data ) {
			try {
				var model = JsonConvert.DeserializeObject<SessionDataModel>( data );
				Log.Warn( $"Model(netId): {data}" );
				OnSessionJoinHandler( model );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSessionJoinHandler( SessionDataModel model ) {
			var player = new PlayerList().FirstOrDefault( p => p.ServerId == model.NetId );
			if( player == null ) {
				Log.Error( $"Received Session Data for {model.NetId}, but no player found." );
			}

			var session = new Session( model.NetId, model );
			Log.Warn( $"Model.NetId: {model.NetId} | {model.Name} | {model.SharedData} | {model.ProtectedData}" );
			Log.Warn( $"_sessions: {_sessions != null}" );
			_sessions.Add( session );
		}

		private void OnNameUpdate( int serverId, string name ) {
			try {
				var session = FromPlayer( serverId );
				if( session == null ) return;

				session.Name = name;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
