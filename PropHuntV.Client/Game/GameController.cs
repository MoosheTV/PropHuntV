using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using PropHuntV.Util;

namespace PropHuntV.Client.Game
{
	public class GameController : ClientAccessor
	{
		private readonly List<HudComponent> _hiddenComponents = new List<HudComponent>();

		public bool ShowRadar { get; set; } = true;

		public GameController( Client client ) : base( client ) {
			Client.RegisterTickHandler( OnTick );
		}

		/// <summary>
		/// Hides the component given from being rendered.
		/// </summary>
		/// <param name="comp">The component to hide.</param>
		public void HideComponent( HudComponent comp ) {
			Screen.Hud.HideComponentThisFrame( comp );
			if( !_hiddenComponents.Contains( comp ) ) {
				_hiddenComponents.Add( comp );
			}
		}

		/// <summary>
		/// Unhides the component, and will render it again.
		/// </summary>
		/// <param name="comp">The component to stop hiding.</param>
		public void ShowComponent( HudComponent comp ) {
			Screen.Hud.ShowComponentThisFrame( comp );
			if( _hiddenComponents.Contains( comp ) ) {
				_hiddenComponents.Remove( comp );
			}
		}

		public void HideAllComponents() {
			_hiddenComponents.AddRange( Enum.GetValues( typeof( HudComponent ) ).OfType<HudComponent>() );
			foreach( var comp in _hiddenComponents ) {
				Screen.Hud.HideComponentThisFrame( comp );
			}
		}

		public void ShowAllComponents() {
			_hiddenComponents.Clear();
			foreach( var comp in _hiddenComponents ) {
				Screen.Hud.ShowComponentThisFrame( comp );
			}
		}

		private async Task OnTick() {
			try {
				// Hide HUD components
				if( _hiddenComponents.Any() ) {
					foreach( var comp in new List<HudComponent>( _hiddenComponents ) ) {
						Screen.Hud.HideComponentThisFrame( comp );
					}
				}

				if( !ShowRadar ) {
					API.HideHudAndRadarThisFrame();
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
