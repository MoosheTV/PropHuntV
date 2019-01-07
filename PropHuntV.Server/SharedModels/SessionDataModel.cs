using System.Collections.Generic;

namespace PropHuntV.SharedModels
{
	public class SessionDataModel
	{
		public int NetId { get; set; }
		public string Name { get; set; }
		public Dictionary<string, object> SharedData { get; set; }
		public Dictionary<string, object> ProtectedData { get; set; }
	}
}
