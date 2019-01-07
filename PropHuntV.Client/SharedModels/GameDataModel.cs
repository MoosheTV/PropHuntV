using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PropHuntV.SharedModels
{
	public class GameDataModel
	{
		public bool HasStarted { get; set; }
		public bool IsHiding { get; set; } = true;
		public string Map { get; set; }
		public int TimeLeft { get; set; }

		[JsonIgnore]
		public DateTime GameEndsAt { get; set; }

		public List<PlayerDataModel> Players { get; set; } = new List<PlayerDataModel>();

		public GameDataModel() {

		}

		public GameDataModel( GameDataModel model ) {
			HasStarted = model.HasStarted;
			Map = model.Map;
			TimeLeft = model.TimeLeft;
			GameEndsAt = model.GameEndsAt;
			Players = model.Players;
		}
	}
}
