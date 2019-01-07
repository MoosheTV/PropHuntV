using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PropHuntV.Util;

namespace PropHuntV.Client.Game
{
	public class TimeController : ClientAccessor
	{
		/// <summary>
		/// The Current Day in-game.
		/// </summary>
		public DateTime Day
		{
			get {
				var day = Function.Call<int>( Hash.GET_CLOCK_DAY_OF_MONTH );
				var month = Function.Call<int>( Hash.GET_CLOCK_MONTH );
				var year = Function.Call<int>( Hash.GET_CLOCK_YEAR );
				return new DateTime( year, month, day );
			}
			set => Function.Call( Hash.SET_CLOCK_DATE, value.Day, value.Month, value.Year );
		}

		public int Hour
		{
			get => Function.Call<int>( Hash.GET_CLOCK_HOURS );
			set {
				_freezeTime = new TimeSpan( value, _freezeTime.Minutes, _freezeTime.Seconds );
				Function.Call( Hash.NETWORK_OVERRIDE_CLOCK_TIME, value, Minute, Second );
			}
		}

		public int Minute
		{
			get => Function.Call<int>( Hash.GET_CLOCK_MINUTES );
			set {
				_freezeTime = new TimeSpan( _freezeTime.Hours, value, _freezeTime.Seconds );
				Function.Call( Hash.NETWORK_OVERRIDE_CLOCK_TIME, Hour, value, Second );
			}
		}

		public int Second
		{
			get => Function.Call<int>( Hash.GET_CLOCK_SECONDS );
			set {
				_freezeTime = new TimeSpan( _freezeTime.Hours, _freezeTime.Minutes, value );
				Function.Call( Hash.NETWORK_OVERRIDE_CLOCK_TIME, Hour, Minute, value );
			}
		}

		private TimeSpan _freezeTime = TimeSpan.Zero;

		public bool Freeze
		{
			get => _freezeTime != TimeSpan.Zero;
			set => _freezeTime = value ? new TimeSpan( Hour, Minute, Second ) : TimeSpan.Zero;
		}

		public TimeController( Client client ) : base( client ) {
			Client.RegisterTickHandler( OnTick );
		}

		public void Set( int hour, int minute, int second, bool freeze = false ) {
			Hour = hour;
			Minute = minute;
			Second = second;
			Freeze = freeze;
		}

		private async Task OnTick() {
			try {
				if( _freezeTime != TimeSpan.Zero ) {
					Function.Call( Hash.NETWORK_OVERRIDE_CLOCK_TIME, _freezeTime.Hours, _freezeTime.Minutes, _freezeTime.Seconds );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				await BaseScript.Delay( 1000 );
			}
		}
	}
}
