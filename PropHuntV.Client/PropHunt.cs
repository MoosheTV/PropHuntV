using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Client
{
	public class PropHunt : ClientAccessor
	{
		private const int DefaultHealthValue = 50;
		private const float IntegrityDistanceThreshold = 8f; // The max distance in meters a player can move in a second.

		internal static readonly Vector3 SpawnPosition = new Vector3( 232.377f, -986.48f, -100f );
		internal static readonly Vector3 WaitAreaPosition = new Vector3( 197.5932f, -935.3035f, 30.68681f );
		internal static readonly Vector4 SpawnLocation = new Vector4( SpawnPosition, 57.199f );

		private static readonly Dictionary<string, int> ModelSelectors = new Dictionary<string, int> {
			["_chr"] = 60,
			["chair"] = 60,
			["table"] = 100,
			["_mat"] = 30,
			["_ball"] = 10,
			["plant"] = 30,
			["bench"] = 80,
			["streetlight"] = 80
		};

		private static readonly Dictionary<string, Vector3> ModelOffsets = new Dictionary<string, Vector3> {
			{"hei_prop_hei_bust_01", new Vector3(0f, 0f, 0.45f)},
			{"prop_table_05", new Vector3(0f, 0f, 0.4f)},
			{"prop_table_02", new Vector3(0f, 0f, 0.4f)},
			{"v_res_filebox01", new Vector3(0f, 0f, 0.15f)},
			{"v_serv_tc_bin1_", new Vector3(0f, 0f, 0.4f)},
			{"v_serv_tc_bin2_", new Vector3(0f, 0f, 0.4f)},
			{"prop_fire_exting_2a", new Vector3(0f, 0f, 0.6f)},
			{"prop_fire_exting_3a", new Vector3(0f, 0f, 0.6f)},
			{"v_corp_bk_chair3", new Vector3(0f, 0f, 0.4f)},
			{"prop_dyn_pc_02", new Vector3(0f, 0f, 0.4f)},
			{"hei_prop_hei_lflts_02", new Vector3(0f, 0f, 0.4f)},
			{"hei_prop_hei_lflts_01", new Vector3(0f, 0f, 0.15f)},
			{"prop_table_08_chr", new Vector3(0f, 0f, 0.15f)},
			{"prop_mug_01", new Vector3(0f, 0f, 0.1f)},
			{"hei_prop_hei_bank_mon", new Vector3(0f, 0f, 0.15f)},
			{"prop_beach_parasol_02", new Vector3(0f, 0f, -0.3f)},
			{"prop_beach_parasol_01", new Vector3(0f, 0f, -0.3f)},
			{"prop_ball_box", new Vector3(0f, 0f, 0.2f)},
			{"prop_speaker_05", new Vector3(0f, 0f, 0.2f)},
			{"prop_patio_lounger1_table", new Vector3(0f, 0f, 0.15f)},
			{"prop_tv_flat_02", new Vector3(0f, 0f, 0.3f)},
			{"prop_arm_wrestle_01", new Vector3(0f, 0f, 0.3f) },
			{"prop_ld_toilet_01", new Vector3(0f, 0f, 0.3f) },
			{"prop_cratepile_07a", new Vector3(0f, 0f, 0.3f) },
			{"prop_rub_tyre_02", new Vector3(0f, 0f, 0.3f) },
		};

		internal MapHandler MapHandler { get; }
		internal LobbyHandler LobbyHandler { get; }
		internal SpectatorHandler Spectate { get; }

		private Entity _currentEntity;
		private Vector3 _lastPosition;

		internal ServerConfigModel Config { get; private set; }

		public bool DrawCrosshair { get; set; }

		private DateTime _lastValidWipe = DateTime.MinValue;
		private readonly Dictionary<int, uint> _propDict = new Dictionary<int, uint>();

		public PropHunt( Client client ) : base( client ) {
			MapHandler = new MapHandler( this, client );
			LobbyHandler = new LobbyHandler( this, client );
			Spectate = new SpectatorHandler( this, client );

			Client.RegisterEventHandler( "PropHunt.Config", new Action<string>( OnConfig ) );
			Client.RegisterEventHandler( "PropHunt.Prop", new Action<int, int, uint>( OnPropUpdate ) );
			Client.RegisterEventHandler( "PropHunt.Sound", new Action<string>( PlaySound ) );
			Client.RegisterEventHandler( "UI.ShowNotification", new Action<string>( UiHelper.ShowNotification ) );
			Client.RegisterTickHandler( EntitySelectTick );
			Client.RegisterTickHandler( IntegrityTick );
			Client.RegisterTickHandler( ValidatePropTick );
			Client.RegisterTickHandler( OnTick );

			Client.World.SpawnCops = false;
			Client.World.Weather = WeatherType.Clear;
			Client.Time.Hour = 12;
			Client.Time.Freeze = true;
			Client.World.PedDensity = Client.World.VehicleDensity = 0f;
			Client.Game.HideAllComponents();
			Client.Game.ShowRadar = false;
			Client.Player.PlayerSpeed = 1.5f;
			Client.Player.InfiniteStamina = true;
			Client.Player.DisablePistolWhipping = true;
		}

		private async Task ValidatePropTick() {
			try {
				if( (DateTime.UtcNow - _lastValidWipe).TotalMinutes > 1 ) {
					foreach( var sess in new List<int>( _propDict.Keys ) ) {
						if( Client.Sessions.FromPlayer( sess ) == null )
							_propDict.Remove( sess );
					}
					_lastValidWipe = DateTime.UtcNow;
				}

				foreach( var kvp in new Dictionary<int, uint>( _propDict ) ) {
					var player = new PlayerList().FirstOrDefault( p => p.ServerId == kvp.Key );
					if( player == null ) continue;

					var playerPos = player.Character.Position;
					var objId = API.GetClosestObjectOfType( playerPos.X, playerPos.Y, playerPos.Z, 2, kvp.Value, true, false, false );
					if( objId != 0 ) {
						var obj = Entity.FromHandle( objId );
						if( obj?.IsAttachedTo( player.Character ) ?? false )
							obj.IsVisible = true;
					}

					await BaseScript.Delay( 100 );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async void OnPropUpdate( int sender, int netId, uint hash ) {
			try {
				_propDict[sender] = hash;

				await BaseScript.Delay( 500 );
				var objHandle = API.NetToObj( netId );
				if( objHandle != 0 ) {
					var obj = Entity.FromHandle( objHandle );
					obj.IsVisible = true;
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public void GiveAllWeapons( IEnumerable<WeaponModel> list ) {
			foreach( WeaponHash weapon in Enum.GetValues( typeof( WeaponHash ) ) ) {
				if( Client.Player.PlayerPed.Weapons.HasWeapon( weapon ) ) {
					Client.Player.PlayerPed.Weapons[weapon].Ammo = 0;
					Client.Player.PlayerPed.Weapons[weapon].AmmoInClip = 0;
				}
				Client.Player.PlayerPed.Weapons.Remove( weapon );
			}
			foreach( var weapon in list ) {
				var hash = Enum.GetValues( typeof( WeaponHash ) ).OfType<WeaponHash>()
					.FirstOrDefault( w => Enum.GetName( typeof( WeaponHash ), w )?.Equals( weapon.Weapon, StringComparison.InvariantCultureIgnoreCase ) ?? false );
				if( hash != WeaponHash.Unarmed ) {
					CitizenFX.Core.Game.PlayerPed.Weapons.Give( hash, weapon.Ammo, true, true );
				}
			}
		}

		private void OnConfig( string data ) {
			try {
				Config = JsonConvert.DeserializeObject<ServerConfigModel>( data );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async Task OnTick() {
			try {
				if( DrawCrosshair )
					Screen.Hud.ShowComponentThisFrame( HudComponent.Reticle );

				if( Client.Player.PlayerPed.IsDead || Client.Player.PlayerPed.Health <= 0 ) {
					if( MapHandler.IsPlaying() ) {
						var killer = Client.Player.PlayerPed.GetKiller();
						BaseScript.TriggerServerEvent( "PropHunt.Dead", killer != null ? new PlayerList().FirstOrDefault( p => p.Character.Handle == killer.Handle )?.ServerId ?? -1 : -1 );
						Spectate.IsEnabled = true;
					}
					ClearModel();
					Client.Player.PlayerPed.Resurrect();
					await BaseScript.Delay( 250 );
					foreach( WeaponHash weapon in Enum.GetValues( typeof( WeaponHash ) ) ) {
						Client.Player.PlayerPed.Weapons.Remove( weapon );
					}
				}

				CitizenFX.Core.Game.Player.SetMayNotEnterAnyVehicleThisFrame();

				var playerList = new PlayerList();
				var netId = CitizenFX.Core.Game.Player.ServerId;
				foreach( var player in playerList ) {
					if( player.ServerId == netId ) continue;
					player.Character.SetNoCollision( Client.Player.PlayerPed, true );
				}

				if( MapHandler.CurrentPlayer == null || !MapHandler.GameState.IsHiding && !MapHandler.GameState.HasStarted ) {
					foreach( var session in new PlayerList() ) {
						if( session.ServerId == netId ) continue;
						if( session.Character.Position.DistanceToSquared( SpawnPosition ) > 100f ) {
							API.SetEntityLocallyVisible( session.Character.Handle );
						}
						else {
							API.SetEntityLocallyInvisible( session.Character.Handle );
						}
					}
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
		}

		private async Task IntegrityTick() {
			try {
				var isPlaying = MapHandler.CurrentPlayer?.IsAlive ?? false;
				CitizenFX.Core.Game.Player.IsInvincible = !isPlaying;

				if( isPlaying && Client.Player.PlayerPed.IsInWater ) {
					Damage( (int)Math.Ceiling( Client.Player.PlayerPed.MaxHealth * 0.02f ) );
				}

				if( isPlaying && !MapHandler.IsPlayerWithinBounds() ) {
					Damage( (int)Math.Ceiling( Client.Player.PlayerPed.MaxHealth * 0.05f ) );
					Client.Player.Position = _lastPosition;
				}

				if( Math.Sqrt( Client.Player.Position.DistanceToSquared( _lastPosition ) ) > IntegrityDistanceThreshold ) {
					Client.Player.Position = _lastPosition;
				}

				_lastPosition = Client.Player.Position;
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
			await BaseScript.Delay( 200 );
		}

		public void ClearModel() {
			_currentEntity?.Detach();
			_currentEntity?.Delete();
			_currentEntity = null;
			Client.Player.PlayerPed.IsVisible = true;
		}

		private async Task EntitySelectTick() {
			try {
				if( MapHandler.CurrentPlayer?.Team != Team.Prop ) {
					await BaseScript.Delay( 500 );
					return;
				}

				if( _currentEntity != null ) {
					_currentEntity.IsVisible = true;
				}

				var entity = GetEntityInCrosshair();
				if( CitizenFX.Core.Game.IsControlJustPressed( 2, Control.CursorCancel ) ) {
					_currentEntity?.Detach();
					_currentEntity?.Delete();
					_currentEntity = null;

					if( entity == null ) {
						Client.Player.PlayerPed.IsVisible = true;
						SetHealth( DefaultHealthValue );
						await BaseScript.Delay( 100 );
						return;
					}

					var objName = Enum.GetName( typeof( ObjectHash ), entity.Model.Hash ) ?? "Null";
					if( objName == "Null" || (MapHandler.CurrentMap?.BannedProps.Any( o => o.Equals( objName, StringComparison.InvariantCultureIgnoreCase ) ) ?? true) ) {
						Client.Player.PlayerPed.IsVisible = true;
						SetHealth( DefaultHealthValue );
						return;
					}

					Log.Info( $"Changing model as {objName} ({entity.Model.Hash:x2})" );

					_currentEntity = await World.CreateProp( new Model( entity.Model.Hash ), Client.Player.Position, false, false );
					_currentEntity.IsCollisionEnabled = false;
					_currentEntity.AttachTo( Client.Player.PlayerPed, new Vector3( 0f, 0f, -0.99f ) + (ModelOffsets.ContainsKey( objName ) ? ModelOffsets[objName] : Vector3.Zero) );

					Client.Player.PlayerPed.IsVisible = false;
					SetHealth( CalculateHealth( _currentEntity.Model ) );

					_currentEntity.IsVisible = true;
					BaseScript.TriggerServerEvent( "PropHunt.Prop", API.ObjToNet( _currentEntity.Handle ), (uint)_currentEntity.Model.Hash );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
		}

		private void SetHealth( int max ) {
			var pct = (float)Client.Player.PlayerPed.Health / Client.Player.PlayerPed.MaxHealth;
			Client.Player.PlayerPed.MaxHealth = max;
			Client.Player.PlayerPed.Health = (int)Math.Floor( max * pct );
			Log.Info( $"Set health to {Client.Player.PlayerPed.Health} / {Client.Player.PlayerPed.MaxHealth}" );
		}

		public async Task SafeTeleport( Vector3 position, float heading = -1f, bool doFadeIn = true ) {
			try {
				if( !API.IsScreenFadedOut() ) {
					API.DoScreenFadeOut( 200 );
					await BaseScript.Delay( 200 );
				}

				Client.Player.PlayerPed.Task.ClearAllImmediately();
				Teleport( position );
				Client.Player.PlayerPed.IsPositionFrozen = true;
				await BaseScript.Delay( 200 );
				Client.Player.PlayerPed.Task.ClearAllImmediately();
				API.RequestCollisionAtCoord( position.X, position.Y, position.Z );
				if( heading >= 0f && heading <= 360f ) {
					Client.Player.PlayerPed.Heading = heading % 360f;
				}
				await BaseScript.Delay( 500 );
				if( doFadeIn ) {
					Client.Player.PlayerPed.IsPositionFrozen = false;
					API.DoScreenFadeIn( 200 );
					await BaseScript.Delay( 200 );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		public void Teleport( Vector3 position ) {
			_lastPosition = position;
			Client.Player.PlayerPed.Task.ClearAllImmediately();
			Client.Player.PlayerPed.PositionNoOffset = position;
		}

		private void Damage( int amount ) {
			Client.Player.PlayerPed.Health -= amount;
		}

		private Entity GetEntityInCrosshair() {
			var raycast = World.Raycast( GameplayCamera.Position, CameraForwardVec(), 100f, IntersectOptions.Everything, Client.Player.PlayerPed );
			if( !raycast.DitHit || !raycast.DitHitEntity || raycast.HitPosition == default( Vector3 ) || !raycast.HitEntity.Model.IsProp ) {
				return null;
			}
			return raycast.HitEntity;
		}

		internal static void PlaySound( string key ) {
			try {
				switch( key.ToLower() ) {
				case "winner":
					API.PlaySoundFrontend( -1, "Mission_Pass_Notify", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS", true );
					break;
				case "loser":
					API.PlaySoundFrontend( -1, "Enemy_Pick_Up", "HUD_FRONTEND_MP_COLLECTABLE_SOUNDS", false );
					break;
				case "death":
					API.PlaySoundFrontend( -1, "CHARACTER_SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true );
					break;
				case "purchase":
					API.PlaySoundFrontend( -1, "LOCAL_PLYR_CASH_COUNTER_COMPLETE", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS", true );
					break;
				case "rankup":
					API.PlaySoundFrontend( -1, "MP_RANK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET", true );
					break;
				case "technofail":
					API.PlaySoundFrontend( -1, "Failure", "DLC_HEIST_HACKING_SNAKE_SOUNDS", false );
					break;
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private static int CalculateHealth( Model model ) {
			var modelName = Enum.GetName( typeof( ObjectHash ), model.Hash ) ?? "";
			var selection = ModelSelectors.FirstOrDefault( kvp => modelName.Contains( kvp.Key ) );
			return string.IsNullOrEmpty( selection.Key ) ? DefaultHealthValue : selection.Value;
		}

		private static Vector3 CameraForwardVec() {
			var rotation = (float)(Math.PI / 180.0) * Function.Call<Vector3>( Hash.GET_GAMEPLAY_CAM_ROT, 2 );
			return Vector3.Normalize( new Vector3( (float)-Math.Sin( rotation.Z ) * (float)Math.Abs( Math.Cos( rotation.X ) ), (float)Math.Cos( rotation.Z ) * (float)Math.Abs( Math.Cos( rotation.X ) ), (float)Math.Sin( rotation.X ) ) );
		}
	}
}
