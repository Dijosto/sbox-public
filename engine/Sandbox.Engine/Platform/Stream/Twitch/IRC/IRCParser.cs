using System.Collections.Generic;

namespace Sandbox.Twitch
{
	internal static class IRCParser
	{
		private enum State
		{
			None,
			V3,
			Prefix,
			Command,
			Param,
			Trailing
		};

		/// <summary>
		/// Parses a raw IRC message into a IRCMessage.
		/// </summary>
		public static IRCMessage Parse( string raw )
		{
			Dictionary<string, string> tagDict = new Dictionary<string, string>();

			State state = State.None;
			int[] starts = new[] { 0, 0, 0, 0, 0, 0 };
			int[] lens = new[] { 0, 0, 0, 0, 0, 0 };
			for ( int i = 0; i < raw.Length; ++i )
			{
				lens[(int)state] = i - starts[(int)state] - 1;
				if ( state == State.None && raw[i] == '@' )
				{
					state = State.V3;
					starts[(int)state] = ++i;

					int start = i;
					string key = null;
					for ( ; i < raw.Length; ++i )
					{
						if ( raw[i] == '=' )
						{
							key = raw.Substring( start, i - start );
							start = i + 1;
						}
						else if ( raw[i] == ';' )
						{
							if ( key == null )
								tagDict[raw.Substring( start, i - start )] = "1";
							else
								tagDict[key] = raw.Substring( start, i - start );
							start = i + 1;
						}
						else if ( raw[i] == ' ' )
						{
							if ( key == null )
								tagDict[raw.Substring( start, i - start )] = "1";
							else
								tagDict[key] = raw.Substring( start, i - start );
							break;
						}
					}
				}
				else if ( state < State.Prefix && raw[i] == ':' )
				{
					state = State.Prefix;
					starts[(int)state] = ++i;
				}
				else if ( state < State.Command )
				{
					state = State.Command;
					starts[(int)state] = i;
				}
				else if ( state < State.Trailing && raw[i] == ':' )
				{
					state = State.Trailing;
					starts[(int)state] = ++i;
					break;
				}
				else if ( state < State.Trailing && raw[i] == '+' || state < State.Trailing && raw[i] == '-' )
				{
					state = State.Trailing;
					starts[(int)state] = i;
					break;
				}
				else if ( state == State.Command )
				{
					state = State.Param;
					starts[(int)state] = i;
				}

				while ( i < raw.Length && raw[i] != ' ' )
					++i;
			}

			lens[(int)state] = raw.Length - starts[(int)state];
			string cmd = raw.Substring( starts[(int)State.Command],
				lens[(int)State.Command] );

			IRCCommand command = IRCCommand.Unknown;
			switch ( cmd )
			{
				case "PRIVMSG":
					command = IRCCommand.PrivMsg;
					break;
				case "NOTICE":
					command = IRCCommand.Notice;
					break;
				case "PING":
					command = IRCCommand.Ping;
					break;
				case "PONG":
					command = IRCCommand.Pong;
					break;
				case "HOSTTARGET":
					command = IRCCommand.HostTarget;
					break;
				case "CLEARCHAT":
					command = IRCCommand.ClearChat;
					break;
				case "CLEARMSG":
					command = IRCCommand.ClearMsg;
					break;
				case "USERSTATE":
					command = IRCCommand.UserState;
					break;
				case "GLOBALUSERSTATE":
					command = IRCCommand.GlobalUserState;
					break;
				case "NICK":
					command = IRCCommand.Nick;
					break;
				case "JOIN":
					command = IRCCommand.Join;
					break;
				case "PART":
					command = IRCCommand.Part;
					break;
				case "PASS":
					command = IRCCommand.Pass;
					break;
				case "CAP":
					command = IRCCommand.Cap;
					break;
				case "001":
					command = IRCCommand.RPL_001;
					break;
				case "002":
					command = IRCCommand.RPL_002;
					break;
				case "003":
					command = IRCCommand.RPL_003;
					break;
				case "004":
					command = IRCCommand.RPL_004;
					break;
				case "353":
					command = IRCCommand.RPL_353;
					break;
				case "366":
					command = IRCCommand.RPL_366;
					break;
				case "372":
					command = IRCCommand.RPL_372;
					break;
				case "375":
					command = IRCCommand.RPL_375;
					break;
				case "376":
					command = IRCCommand.RPL_376;
					break;
				case "WHISPER":
					command = IRCCommand.Whisper;
					break;
				case "SERVERCHANGE":
					command = IRCCommand.ServerChange;
					break;
				case "RECONNECT":
					command = IRCCommand.Reconnect;
					break;
				case "ROOMSTATE":
					command = IRCCommand.RoomState;
					break;
				case "USERNOTICE":
					command = IRCCommand.UserNotice;
					break;
				case "MODE":
					command = IRCCommand.Mode;
					break;
			}

			string parameters = raw.Substring( starts[(int)State.Param],
				lens[(int)State.Param] );
			string message = raw.Substring( starts[(int)State.Trailing],
				lens[(int)State.Trailing] );
			string hostmask = raw.Substring( starts[(int)State.Prefix],
				lens[(int)State.Prefix] );
			return new IRCMessage( command, new[] { parameters, message }, hostmask, tagDict );
		}
	}
}
