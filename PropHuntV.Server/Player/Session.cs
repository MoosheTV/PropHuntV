using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PropHuntV.SharedModels.Data;
using PropHuntV.Util;

namespace PropHuntV.Server.Player
{
	public class Session
	{
		public int NetId => Convert.ToInt32( Player.Handle );
		public CitizenFX.Core.Player Player { get; protected internal set; }

		/// <summary>
		/// The HEX value of the Steam Identifier, or String.Empty if no steam identifier.
		/// </summary>
		public string SteamIdentifier { get; }

		/// <summary>
		/// The 64-bit identifier of the player, or 0 if no steam identifier.
		/// </summary>
		public ulong SteamId64 { get; }

		/// <summary>
		/// The license ID associated with their copy of Grand Theft Auto.
		/// </summary>
		public string LicenseId { get; }

		/// <summary>
		/// The IP Address this player is connecting from.
		/// </summary>
		public string Address { get; }

		private string _name;
		/// <summary>
		/// The display name of the user, which by default is the FiveM Player Name.
		/// This can be updated, unlike the FiveM Player Name. This is generally used
		/// internally for commands and messages.
		/// </summary>
		public string Name
		{
			get => _name;
			set {
				_name = value;
				BaseScript.TriggerClientEvent( "Session.UpdateName", NetId, value );
			}
		}

		public UserModel User { get; set; }

		public string UserFilePath => $"{API.GetResourcePath( API.GetCurrentResourceName() )}/data/players/{SteamIdentifier}.json";

		/// <summary>
		/// The UTC timestamp of when this session was created.
		/// </summary>
		public readonly DateTime TimeJoined = DateTime.UtcNow;

		protected internal readonly Dictionary<string, object> SharedData = new Dictionary<string, object>();
		protected internal readonly Dictionary<string, object> ProtectedData = new Dictionary<string, object>();

		public Session( CitizenFX.Core.Player player, Dictionary<string, object> sharedData = null, Dictionary<string, object> protectedData = null ) {
			Player = player;
			var hasSteam = player.Identifiers.Any( p => p.StartsWith( "steam:" ) );
			SteamIdentifier = hasSteam ? player.Identifiers["steam"].Replace( "steam:", "" ) : "";
			SteamId64 = hasSteam ? Convert.ToUInt64( SteamIdentifier, 16 ) : 0;

			LicenseId = player.Identifiers["license"].Replace( "license:", "" );

			Address = player.Identifiers["ip"].Replace( "ip:", "" );

			if( sharedData != null ) {
				foreach( var kvp in sharedData ) {
					SharedData.Add( kvp.Key, kvp.Value );
				}
			}
			if( protectedData != null ) {
				foreach( var kvp in protectedData ) {
					ProtectedData.Add( kvp.Key, kvp.Value );
				}
			}

			_name = Player.Name;
		}

		public async Task<bool> LoadData() {
			try {
				var exists = File.Exists( UserFilePath );
				if( !exists ) {
					User = new UserModel {
						Address = Address,
						DateCreated = DateTime.UtcNow,
						GtaLicense = LicenseId,
						LastLogin = DateTime.UtcNow,
						DisplayName = Name,
						SteamId = SteamIdentifier,
						Data = new UserDataModel {
							PedModel = Server.ActiveInstance.PropHunt.Config.DefaultPedModel,
							Experience = 0,
							Points = 0
						}
					};
					return await SaveData();
				}

				User = JsonConvert.DeserializeObject<UserModel>( File.ReadAllText( UserFilePath ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
				return false;
			}

			return true;
		}

		public async Task<bool> SaveData() {
			try {
				using( var file = File.CreateText( UserFilePath ) ) {
					await file.WriteAsync( JsonConvert.SerializeObject( User ) );
					await file.FlushAsync();
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
			return true;
		}


		public void SetProtectedData( string key, object value ) {
			SetProtectedData( new Dictionary<string, object> { { key, value } } );
		}

		public void SetProtectedData( Dictionary<string, object> pairs ) {
			foreach( var kvp in pairs ) {
				ProtectedData[kvp.Key] = kvp.Value;
			}

			TriggerEvent( "Session.UpdateProtectedData", NetId, JsonConvert.SerializeObject( pairs ) );
		}

		/// <summary>
		/// Gets the protected data value, or defaultValue if none exists. This data is
		/// only available between the server and this session's client.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public object GetProtectedData( string key, object defaultValue = null ) {
			if( ProtectedData.ContainsKey( key ) ) {
				return ProtectedData[key];
			}

			if( defaultValue != null ) {
				SetProtectedData( key, defaultValue );
			}

			return defaultValue;
		}

		/// <summary>
		/// Sets the value to the data which is shared between all clients. Keep in mind that
		/// these values should never contain sensitive details.
		/// </summary>
		/// <param name="key">The key value</param>
		/// <param name="value">The value to pair to the key. Intended only for native data types.</param>
		public void SetSharedData( string key, object value ) {
			SetSharedData( new Dictionary<string, object> { { key, value } } );
		}

		/// <summary>
		/// Adds the given values to the data dictionary which is shared between all clients.
		/// Keep in mind that these values should never contains sensitive details.
		/// </summary>
		/// <param name="pairs">The data dictionary to add</param>
		public void SetSharedData( Dictionary<string, object> pairs ) {
			foreach( var kvp in pairs ) {
				SharedData[kvp.Key] = kvp.Value;
			}

			BaseScript.TriggerClientEvent( "Session.UpdateSharedData", NetId, JsonConvert.SerializeObject( pairs ) );
		}

		/// <summary>
		/// Gets all shared values associated with this session, and shared between all clients.
		/// </summary>
		/// <returns>The dictionary of values stored.</returns>
		public IReadOnlyDictionary<string, object> GetSharedData() {
			return new Dictionary<string, object>( SharedData );
		}

		/// <summary>
		/// Gets the shared value associated with this session, and shared between all clients.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public object GetSharedData( string key, object defaultValue = null ) {
			if( SharedData.ContainsKey( key ) ) {
				return SharedData[key];
			}

			if( defaultValue != null ) {
				SetSharedData( key, defaultValue );
			}

			return defaultValue;
		}

		public void Drop( string reason ) {
			Player.Drop( reason );
		}

		public void TriggerEvent( string eventName, params object[] args ) {
			Player.TriggerEvent( eventName, args );
		}
	}
}
