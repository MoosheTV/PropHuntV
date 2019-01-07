using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace PropHuntV.Util
{
	public static class Log
	{
		private static string ResourceName { get; }

		static Log() {
			ResourceName = API.GetCurrentResourceName().ToUpper();
		}

		public static void Info( string msg ) {
			WriteLine( "INFO", msg, ConsoleColor.White );
		}

		public static void Warn( string msg ) {
			WriteLine( "WARN", msg, ConsoleColor.Yellow );
		}

		public static void Error( string msg ) {
			WriteLine( "ERROR", msg, ConsoleColor.Red );
		}

		public static void Error( Exception ex, string msg = "" ) {
			WriteLine( "ERROR", $"{msg}\r\n{ex}", ConsoleColor.Red );
		}

		public static void Verbose( string msg ) {
			WriteLine( "VERBOSE", msg, ConsoleColor.DarkGray );
		}

		private static void WriteLine( string title, string msg, ConsoleColor color ) {
			try {
				var m = $"[{ResourceName}] [{title}] {msg}";
#if SERVER
				Console.ForegroundColor = color;
				Console.WriteLine( $"{DateTime.Now:G} {m}" );
				Console.ResetColor();
#else
				Debug.WriteLine( m );
#endif
			}
			catch( Exception ex ) {
				Console.WriteLine( ex );
			}
		}
	}
}
