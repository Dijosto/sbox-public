using System;
using System.Collections.Generic;

namespace Facepunch.InteropGen;

public static class Log
{
	private static readonly object lockObj = new();

	// Store logs by group name
	private static readonly Dictionary<string, List<LogMessage>> groupLogs = [];

	[ThreadStatic]
	internal static int Indentation;

	[ThreadStatic]
	internal static string CurrentGroup;

	private record LogMessage
	{
		public ConsoleColor Color { get; set; }
		public string Text { get; set; }
		public int Indent { get; set; }
	}

	public static void WriteLine( string message )
	{
		WriteLine( ConsoleColor.White, message );
	}

	public static void Warning( string message )
	{
		WriteLine( ConsoleColor.Red, message );
	}

	public static void WriteLine( ConsoleColor color, string message )
	{
		lock ( lockObj )
		{
			if ( string.IsNullOrEmpty( CurrentGroup ) )
			{
				// No current group, just print directly
				PrintDirectMessage( color, message );
				return;
			}

			// Add to group logs
			if ( !groupLogs.ContainsKey( CurrentGroup ) )
			{
				groupLogs[CurrentGroup] = [];
			}

			groupLogs[CurrentGroup].Add( new LogMessage
			{
				Color = color,
				Text = message,
				Indent = Indentation
			} );
		}
	}

	private static void PrintDirectMessage( ConsoleColor color, string message )
	{
		ConsoleColor originalColor = Console.ForegroundColor;
		try
		{
			Console.ForegroundColor = color;
			Console.WriteLine( message );
		}
		finally
		{
			Console.ForegroundColor = originalColor;
		}
	}

	// log completion message in green or red and then all the logs of the current group
	public static void Completion( string message, bool success )
	{
		lock ( lockObj )
		{
			if ( string.IsNullOrEmpty( CurrentGroup ) )
			{
				// No current group, just print directly
				PrintDirectMessage( success ? ConsoleColor.Green : ConsoleColor.Red, message );
				return;
			}
			// Print all logs for the current group
			if ( groupLogs.TryGetValue( CurrentGroup, out List<LogMessage> logs ) )
			{
				foreach ( LogMessage log in logs )
				{
					PrintDirectMessage( log.Color, new string( ' ', log.Indent ) + log.Text );
				}
			}
			PrintDirectMessage( success ? ConsoleColor.Green : ConsoleColor.Red, $"{CurrentGroup}: {message}" );
			// Clear the current group
			_ = groupLogs.Remove( CurrentGroup );
			CurrentGroup = null;
			Indentation = 0;
		}
	}

	public class Indent : IDisposable
	{
		public Indent()
		{
			Indentation += 2;
		}

		public virtual void Dispose()
		{
			Indentation -= 2;
		}
	}

	public static IDisposable Group( ConsoleColor color, string groupName )
	{
		lock ( lockObj )
		{
			CurrentGroup = groupName;

			// Initialize group if needed
			if ( !groupLogs.ContainsKey( groupName ) )
			{
				groupLogs[groupName] = [];
			}

			// Add group header to logs
			groupLogs[groupName].Add( new LogMessage
			{
				Color = color,
				Text = groupName,
				Indent = 0
			} );

			// Print start message immediately
			PrintDirectMessage( color, $"Started: {groupName}" );

			Indentation = 2;
		}
		return new Indent();
	}
}
