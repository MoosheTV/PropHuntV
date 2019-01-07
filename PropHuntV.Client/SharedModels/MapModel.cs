using System.Collections.Generic;
using CitizenFX.Core;

namespace PropHuntV.SharedModels
{
	public class MapModel
	{
		public string Name { get; set; }
		public LobbySize Size { get; set; }
		public int Weather { get; set; }
		public int Hour { get; set; } = 12;
		public List<string> BannedProps { get; set; } = new List<string>();
		public float[][] Bounds { get; set; } = { };
		public float[] SpawnPoint { get; set; }
		public float MaxZ { get; set; } = 1000f;
		public float MinZ { get; set; } = -300f;
	}

	public enum LobbySize
	{
		Small = 0,
		Medium = 16,
		Large = 24
	}
}
