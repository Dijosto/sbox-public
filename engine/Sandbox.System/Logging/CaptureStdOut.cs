using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox
{
	internal class CaptureStdOut : TextWriter
	{
		internal bool IsErrorOut { get; set; }

		private static Diagnostics.Logger log = Diagnostics.Logging.GetLogger( "Console" );

		public static void Init()
		{
			Console.SetOut( new CaptureStdOut() );
			Console.SetError( new CaptureStdOut { IsErrorOut = true } );
		}

		public override Encoding Encoding { get { return Encoding.UTF8; } }

		public override void Write( string value )
		{
			if ( IsErrorOut )
			{
				log.Error( value );
			}
			else
			{
				log.Info( value );
			}
		}

		public override void WriteLine( string value )
		{
			Write( value );
		}

		public override void Write( char value )
		{
			throw new NotImplementedException();
		}
	}
}
