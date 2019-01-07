using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PropHuntV.Util;

namespace PropHuntV.Client.Game
{
	public class IplController : ClientAccessor
	{
		public Dictionary<string, Vector4> ProximityIpls = new Dictionary<string, Vector4> {
			{"hei_carrier", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int1", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int2", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int3", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int4", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int5", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"hei_carrier_int6", new Vector4(3092.765f, -4704.072f, 11.81955f, 1000f)},
			{"bkr_bi_hw1_13_int", new Vector4(984.1553f, -95.36626f, 75.9326f, 500f)}
		};

		private List<string> _currentLoadedIpls = new List<string>();

		public IplController( Client client ) : base( client ) {
			Client.RegisterTickHandler( LoadProximityTick );
		}

		private async Task LoadProximityTick() {
			try {
				foreach( var ipl in ProximityIpls ) {
					var pos = new Vector3( ipl.Value.X, ipl.Value.Y, ipl.Value.Z );
					if( Math.Sqrt( pos.DistanceToSquared2D( Client.Player.Position ) ) > ipl.Value.W ) {
						if( _currentLoadedIpls.Contains( ipl.Key ) ) {
							_currentLoadedIpls.Remove( ipl.Key );
							UnloadIpl( ipl.Key );
						}
						continue;
					}

					if( _currentLoadedIpls.Contains( ipl.Key ) ) continue;
					_currentLoadedIpls.Add( ipl.Key );
					LoadIpl( ipl.Key );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
			await BaseScript.Delay( 50 );
		}

		public void LoadIpl( string ipl ) {
			Log.Info( $"Load ipl {ipl}" );
			API.RequestIpl( ipl );
		}

		public void UnloadIpl( string ipl ) {
			Log.Info( $"Unload ipl {ipl}" );
			API.RemoveIpl( ipl );
		}
	}
}
