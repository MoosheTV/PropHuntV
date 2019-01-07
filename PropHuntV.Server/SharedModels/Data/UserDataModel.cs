using System.Collections.Generic;

namespace PropHuntV.SharedModels.Data
{
	public class UserDataModel
	{
		public uint Experience { get; set; }
		public uint Points { get; set; }
		public string PedModel { get; set; }
		public List<string> Unlockables { get; set; } = new List<string>();
	}
}
