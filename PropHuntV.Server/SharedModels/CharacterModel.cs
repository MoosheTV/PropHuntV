namespace PropHuntV.SharedModels
{
	public class CharacterModel
	{
		public string ModelName { get; set; }
		public bool Unlocked { get; set; }
		public uint PointCost { get; set; } = 1000;
	}
}
