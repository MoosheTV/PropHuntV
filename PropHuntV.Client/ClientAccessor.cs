namespace PropHuntV.Client
{
	public class ClientAccessor
	{
		protected Client Client { get; }

		protected ClientAccessor( Client client ) {
			Client = client;
		}
	}
}
