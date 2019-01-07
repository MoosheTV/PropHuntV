namespace PropHuntV.SharedModels
{
	public enum Team
	{
		None = 0,
		Hunter,
		Prop
	}

	public class PlayerDataModel
	{
		public int NetId { get; set; }
		public Team Team { get; set; }
		public int Kills { get; set; }
		public int SurvivalTime { get; set; }
		public bool IsAlive { get; set; }
	}
}
