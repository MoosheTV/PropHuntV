namespace PropHuntV.Server
{
	public class ServerAccessor
	{

		protected PropHuntV.Server.Server Server { get; }

		protected ServerAccessor( PropHuntV.Server.Server server ) {
			Server = server;
		}
	}
}
