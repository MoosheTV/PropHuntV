using System.Collections.Generic;

namespace PropHuntV.SharedModels
{
	public class ServerConfigModel
	{
		public string DefaultPedModel { get; set; }
		public float PropHunterRatio { get; set; } = 0.75f;
		public int RoundTime { get; set; } = 600;
		public int HideTime { get; set; } = 30;
		public int WaitTime { get; set; } = 60;
		public int MinPlayers { get; set; } = 8;
		public float AliveReward { get; set; } = 10f;
		public float SurviveReward { get; set; } = 0.025f;
		public float KillReward { get; set; } = 5f;
		public float WinReward { get; set; } = 30f;
		public List<CharacterModel> Characters { get; set; } = new List<CharacterModel>();
		public List<MapModel> Maps { get; set; } = new List<MapModel>();
		public List<WeaponModel> WaitWeapons { get; set; } = new List<WeaponModel>();
		public List<WeaponModel> HuntWeapons { get; set; } = new List<WeaponModel>();
	}

	public class WeaponModel
	{
		public string Weapon { get; set; }
		public int Ammo { get; set; }
	}
}
