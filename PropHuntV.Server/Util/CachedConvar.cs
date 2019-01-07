using CitizenFX.Core.Native;

namespace PropHuntV.Util
{
	public class CachedConvar : CachedValue<string>
	{
		public string ConVar { get; }
		public string DefaultValue { get; }

		public CachedConvar( string convar, string defaultValue, long timeoutMs ) : base( timeoutMs ) {
			ConVar = convar;
			DefaultValue = defaultValue;
		}

		protected override string Update() {
			return API.GetConvar( ConVar, DefaultValue );
		}
	}
}
