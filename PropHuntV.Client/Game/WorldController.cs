using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using PropHuntV.SharedModels;
using PropHuntV.Util;

namespace PropHuntV.Client.Game
{
	public class WorldController : ClientAccessor
	{

		private static readonly Dictionary<CloudHatType, string> CloudHats = new Dictionary<CloudHatType, string> {
			[CloudHatType.Altostratus] = "altostratus",
			[CloudHatType.Clear01] = "Clear 01",
			[CloudHatType.Cloudy01] = "Cloudy 01",
			[CloudHatType.HorizonBand1] = "horizonband1",
			[CloudHatType.HorizonBand2] = "horizonband2",
			[CloudHatType.HorizonBand3] = "horizonband3",
			[CloudHatType.Horsey] = "horsey",
			[CloudHatType.Rain] = "RAIN",
			[CloudHatType.Shower] = "shower"
		};

		private CloudHatType _cloudHat;
		/// <summary>
		/// Sets the cloud hat type to whatever you define it to.
		/// </summary>
		public CloudHatType CloudHat
		{
			get => _cloudHat;
			set {
				try {
					_cloudHat = value;
					var val = CloudHats.ContainsKey( _cloudHat ) ? CloudHats[_cloudHat] : Enum.GetName( typeof( CloudHatType ), _cloudHat ) ?? "";
					Function.Call( Hash._SET_CLOUD_HAT_TRANSITION, val, 1f );
				}
				catch( Exception ex ) {
					Log.Error( ex );
				}
			}
		}

		public float VehicleDensity { get; set; } = 0f;
		public float PedDensity { get; set; } = 0f;

		private WeatherType _weather;
		/// <summary>
		/// Sets the weather type to whatever you define it to.
		/// </summary>
		public WeatherType Weather
		{
			get => _weather;
			set {
				try {
					var newVal = (Enum.GetName( typeof( WeatherType ), value ) ?? "").ToUpper();
					Function.Call( Hash._SET_WEATHER_TYPE_OVER_TIME, newVal, 3f );
					_weather = value;
				}
				catch( Exception ex ) {
					Log.Error( ex );
				}
			}
		}

		private bool _isBlackout;
		/// <summary>
		/// Blacks out light emissions, headlights, and interior lights.
		/// </summary>
		public bool Blackout
		{
			get => _isBlackout;
			set {
				_isBlackout = value;
				Function.Call( Hash._SET_BLACKOUT, value );
			}
		}

		private bool _allowCops = true;
		/// <summary>
		/// Whether or not cops/military can spawn and interact with the player.
		/// </summary>
		public bool SpawnCops
		{
			get => _allowCops;
			set {
				_allowCops = value;
				Function.Call( Hash.SET_CREATE_RANDOM_COPS, value );
				Function.Call( Hash.SET_CREATE_RANDOM_COPS_NOT_ON_SCENARIOS, value );
				Function.Call( Hash.SET_CREATE_RANDOM_COPS_ON_SCENARIOS, value );
				Function.Call( Hash.SET_DISPATCH_COPS_FOR_PLAYER, value );
				Function.Call( Hash.SET_POLICE_IGNORE_PLAYER, !value );
			}
		}

		public WorldController( Client client ) : base( client ) {
			Client.RegisterTickHandler( OnTick );

			Function.Call( Hash.SET_CAN_ATTACK_FRIENDLY, true );
			Function.Call( Hash.NETWORK_SET_FRIENDLY_FIRE_OPTION, true );
		}

		private async Task OnTick() {
			try {
				var player = CitizenFX.Core.Game.Player;

				// Disable wanted level if cops are disabled
				if( !_allowCops && player.WantedLevel > 0 ) {
					player.WantedLevel = 0;
					Function.Call( Hash.SET_PLAYER_CAN_BE_HASSLED_BY_GANGS, player.Handle, false );
				}

				Function.Call( Hash.SET_PARKED_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, VehicleDensity );
				Function.Call( Hash.SET_RANDOM_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, VehicleDensity );
				Function.Call( Hash.SET_VEHICLE_DENSITY_MULTIPLIER_THIS_FRAME, VehicleDensity );
				Function.Call( Hash.SET_PED_DENSITY_MULTIPLIER_THIS_FRAME, PedDensity );
				Function.Call( Hash.SET_SCENARIO_PED_DENSITY_MULTIPLIER_THIS_FRAME, PedDensity );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
