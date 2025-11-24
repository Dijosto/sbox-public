using System;

namespace Editor
{
	[AttributeUsage( AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true )]
	public class MenuAttribute : Attribute, Sandbox.IMemberAttribute
	{
		static Dictionary<string, MenuBar> Targets = new();
		static Action Unregister;

		public static void RegisterMenuBar( string name, MenuBar b )
		{
			Targets[name] = b;

			RegisterAll();
		}

		public string Target { get; set; }
		public string Path { get; set; }
		public string Icon { get; set; }
		public int Priority { get; set; }

		[Obsolete( "Use [Shortcut] attribute" )]
		public string Shortcut { get; set; }

		public Sandbox.MemberDescription MemberDescription { get; set; }

		public MenuAttribute( string target, string path, string icon = null )
		{
			Target = target;
			Path = path;
			Icon = icon;
		}

		[Event( "refresh" )]
		static void RegisterAll()
		{
			Unregister?.Invoke();

			foreach ( var target in Targets )
			{
				if ( !target.Value.IsValid ) continue;

				using var su = SuspendUpdates.For( target.Value );

				foreach ( var m in EditorTypeLibrary.GetMemberAttributes<MenuAttribute>( true ).Where( x => x.Target == target.Key ).OrderBy( x => x.Priority ) )
				{
					m.Register();
				}
			}
		}

		void Register()
		{
			if ( !Targets.TryGetValue( Target, out var menuBar ) )
				return;

			if ( MemberDescription is MethodDescription method )
			{
				var shortcut = method.GetCustomAttribute<ShortcutAttribute>();
				var o = menuBar.AddOption( Path, Icon, () => method.Invoke( null, null ), shortcut?.Identifier ?? null );

				Unregister += () => o.Destroy();
			}

			if ( MemberDescription is PropertyDescription property )
			{
				var option = new Option( menuBar, "..." );
				option.Checkable = true;

				option.FetchCheckedState = () => (bool)property.GetValue( null );
				option.Checked = option.FetchCheckedState();

				if ( !string.IsNullOrEmpty( Icon ) )
				{
					option.Icon = Icon;
				}

				option.Triggered += () =>
				{
					property.SetValue( null, option.Checked );
				};

				if ( property.HasAttribute<ShortcutAttribute>() )
					option.ShortcutName = property.GetCustomAttribute<ShortcutAttribute>().Identifier;

				menuBar.AddOption( Path, option );
				Unregister += () => option.Destroy();
			}

		}
	}
}
