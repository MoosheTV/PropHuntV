using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using PropHuntV.SharedModels;
using PropHuntV.SharedModels.Data;
using PropHuntV.Util;

namespace PropHuntV.Client
{
	public class LobbyHandler : ClientAccessor
	{
		protected PropHunt PropHunt { get; }

		private Camera _mainCam, _customCam, _currentCam;

		private DateTime _lastInteraction = DateTime.MinValue, _lastUpdate = DateTime.MinValue;

		public LobbyHandler( PropHunt ctx, Client client ) : base( client ) {
			PropHunt = ctx;
			Client.RegisterNuiEventHandler( "Lobby.Quit", OnQuit );
			Client.RegisterNuiEventHandler( "Lobby.Close", OnClose );
			Client.RegisterNuiEventHandler( "Lobby.SwitchCamera", SwitchCameras );
			Client.RegisterNuiEventHandler( "Lobby.Purchase", OnPurchase );
			Client.RegisterNuiEventHandler( "Lobby.SwitchModel", SwitchModel );
			Client.RegisterEventHandler( "Lobby.ShowNotification", new Action<string, int>( ShowNotification ) );
			Client.RegisterEventHandler( "Session.Loaded", new Action( async () => {
				API.DoScreenFadeOut( 50 );
				API.ShutdownLoadingScreen();
				await ToggleLobby( true );
			} ) );
			Client.RegisterEventHandler( "PropHunt.UserData", new Action<string>( OnDataSet ) );
			Client.RegisterEventHandler( "PropHunt.End", new Action<int>( OnRoundEnd ) );

			Client.RegisterTickHandler( OnTick );
		}

		private async Task OnTick() {
			try {
				if( (DateTime.UtcNow - _lastUpdate).TotalSeconds > 10 ) {
					UpdatePlayerCount();
					_lastUpdate = DateTime.UtcNow;
				}

				if( CitizenFX.Core.Game.IsControlJustPressed( 2, Control.FrontendSocialClub ) ) {
					UpdatePlayerCount();
					Client.TriggerNuiEvent( "Lobby.ShowPlayerCount" );
				}
				else if( CitizenFX.Core.Game.IsControlJustReleased( 2, Control.FrontendSocialClub ) ) {
					Client.TriggerNuiEvent( "Lobby.HidePlayerCount" );
				}

				// If player was in chat or a different NUI focus, this makes sure it puts them back in.
				if( _mainCam != null ) {
					API.SetNuiFocus( true, true );
					await BaseScript.Delay( 100 );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public void UpdatePlayerCount( int count = -1 ) {
			if( count == -1 )
				count = new PlayerList().Count();
			Client.TriggerNuiEvent( "Lobby.PlayerCount", new Dictionary<string, int> {
				{"PlayerCount", count}
			} );
		}

		public void ShowNotification( string message, int seconds ) {
			Client.TriggerNuiEvent( "Lobby.Notification", new NuiMessageModel {
				Message = message,
				Seconds = seconds
			} );
		}

		public void SetCountdown( string message, int seconds ) {
			Client.TriggerNuiEvent( "Lobby.Countdown", new NuiMessageModel {
				Message = message,
				Seconds = seconds
			} );
		}

		private async void OnRoundEnd( int team ) {
			try {
				var winners = (Team)team;
				PropHunt.Spectate.IsEnabled = false;
				foreach( var player in new PlayerList() ) {
					player.Character.IsVisible = true;
				}
				await ToggleLobby( true );
				PropHunt.PlaySound( winners == PropHunt.MapHandler.LastTeam ? "winner" : "loser" );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnClose( IDictionary<string, object> data ) {
			try {
				await ToggleLobby( false );

				if( !PropHunt.MapHandler.GameState.HasStarted ) {
					await PropHunt.SafeTeleport( PropHunt.WaitAreaPosition );
					CitizenFX.Core.Game.PlayerPed.Weapons.RemoveAll();
					PropHunt.GiveAllWeapons( PropHunt.Config.WaitWeapons );
				}
				else {
					PropHunt.Spectate.IsEnabled = true;
				}

				BaseScript.TriggerServerEvent( "PropHunt.Ready" );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void SwitchModel( IDictionary<string, object> data ) {
			try {
				if( (DateTime.UtcNow - _lastInteraction).TotalSeconds < 1f || !data.ContainsKey( "Model" ) ) {
					return;
				}

				var chara = PropHunt.Config.Characters.FirstOrDefault( c => c.ModelName.Equals( data["Model"].ToString(), StringComparison.InvariantCultureIgnoreCase ) );
				if( chara != null ) {
					_lastInteraction = DateTime.UtcNow;
					BaseScript.TriggerServerEvent( "PropHunt.SwitchModel", chara.ModelName );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnPurchase( IDictionary<string, object> data ) {
			try {
				if( (DateTime.UtcNow - _lastInteraction).TotalSeconds < 1f || !data.ContainsKey( "Item" ) ) {
					return;
				}

				var chara = PropHunt.Config.Characters.FirstOrDefault( c => c.ModelName.Equals( data["Item"].ToString(), StringComparison.InvariantCultureIgnoreCase ) );
				if( chara != null ) {
					_lastInteraction = DateTime.UtcNow;
					BaseScript.TriggerServerEvent( "PropHunt.Purchase", chara.ModelName );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal async Task SetPedModel( PedHash hash ) {
			if( Client.Player.PlayerPed.Model.Hash == (int)hash )
				return;

			var model = new Model( hash );
			var modelName = $"0x{(uint)hash:X} (PedHash.{Enum.GetName( typeof( PedHash ), hash ) ?? "Unknown"})";

			// Sometimes the model won't load correctly unless requested twice
			if( !await model.Request( 30000 ) || !await model.Request( 30000 ) || !await CitizenFX.Core.Game.Player.ChangeModel( model ) ) {
				Log.Warn( $"Failed to load ped model {modelName}" );
			}
			else {
				Log.Info( $"Loaded model {modelName}" );
				await BaseScript.Delay( 100 );
				Function.Call( Hash.TASK_START_SCENARIO_IN_PLACE, Client.Player.PlayerPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true );
			}
		}

		private async void OnDataSet( string data ) {
			try {
				var user = JsonConvert.DeserializeObject<UserDataModel>( data );

				Client.TriggerNuiEvent( "Lobby.UserData", user );
				Client.TriggerNuiEvent( "Lobby.Config", PropHunt.Config );

				var pedHash = Enum.GetValues( typeof( PedHash ) ).OfType<PedHash>()
					.FirstOrDefault( p => Enum.GetName( typeof( PedHash ), p )?.Equals( user.PedModel ) ?? false );
				await SetPedModel( pedHash );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnQuit( IDictionary<string, object> data ) {
			Log.Info( "Received lobby quit" );
			API.DoScreenFadeOut( 100 );
			await BaseScript.Delay( 100 );
			PropHunt.Teleport( new Vector3( -5000f, -5000f, 0f ) );
			await BaseScript.Delay( 50 );
			BaseScript.TriggerServerEvent( "Player.DropMe", "Disconnected" );
		}

		private void SwitchCameras( IDictionary<string, object> data ) {
			try {
				if( !data.ContainsKey( "camera" ) ) {
					return;
				}

				var camTo = _customCam;
				if( ((string)data["camera"]).Equals( "main" ) ) {
					camTo = _mainCam;
				}

				if( camTo == _currentCam )
					return;

				World.RenderingCamera.InterpTo( camTo, 750, 100, 100 );
				_currentCam = camTo;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal async Task ToggleLobby( bool enter ) {
			PropHunt.DrawCrosshair = Client.Game.ShowRadar = !enter;
			SetCountdown( "", 0 );
			CitizenFX.Core.Game.PlayerPed.Weapons.RemoveAll();
			if( enter ) {
				PropHunt.Spectate.IsEnabled = false;
				PropHunt.ClearModel();

				await PropHunt.SafeTeleport( new Vector3( PropHunt.SpawnLocation.X, PropHunt.SpawnLocation.Y, PropHunt.SpawnLocation.Z ), PropHunt.SpawnLocation.W );
				API.SetNuiFocus( true, true );

				// Main Camera
				_mainCam = World.CreateCamera( Client.Player.PlayerPed.GetOffsetPosition( new Vector3( 0f, 2.75f, 0.35f ) ), Vector3.Zero, GameplayCamera.FieldOfView );
				_mainCam.PointAt( Client.Player.PlayerPed.Bones[Bone.IK_Head], new Vector3( 0f, 0f, 0.2f ) );

				// Customize Camera
				_customCam = World.CreateCamera( _mainCam.GetOffsetPosition( new Vector3( -1.5f, -1.8f, 0f ) ), Vector3.Zero, GameplayCamera.FieldOfView );
				_customCam.PointAt( Client.Player.PlayerPed.Bones[Bone.IK_Head], new Vector3( -1.75f, 0f, 0.35f ) );

				_currentCam = World.RenderingCamera = _mainCam;
				Client.Game.HideAllComponents();

				Client.TriggerNuiEvent( "Lobby.Open" );
			}
			else {
				API.SetNuiFocus( false, false );

				Client.TriggerNuiEvent( "Lobby.Close" );
				World.RenderingCamera = _mainCam = _customCam = _currentCam = null;
				Client.Player.PlayerPed.Task.ClearAll();
				Client.Game.ShowAllComponents();
				Client.Game.HideComponent( HudComponent.AreaName );
				Client.Game.HideComponent( HudComponent.Cash );
				Client.Game.HideComponent( HudComponent.MpCash );
				Client.Game.HideComponent( HudComponent.CashChange );
				Client.Game.HideComponent( HudComponent.MpTagCashFromBank );
				Client.Game.HideComponent( HudComponent.VehicleName );
				Client.Game.HideComponent( HudComponent.StreetName );
			}
		}

		public class NuiMessageModel
		{
			public string Message { get; set; }
			public int Seconds { get; set; }
		}
	}
}
