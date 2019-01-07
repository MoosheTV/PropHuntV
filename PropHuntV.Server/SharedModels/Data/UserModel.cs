using System;

namespace PropHuntV.SharedModels.Data
{
	public class UserModel
	{
		public string DisplayName { get; set; }
		public string SteamId { get; set; }
		public string Address { get; set; }
		public string GtaLicense { get; set; }
		public DateTime LastLogin { get; set; }
		public DateTime DateCreated { get; set; }
		public UserDataModel Data { get; set; }
	}
}
