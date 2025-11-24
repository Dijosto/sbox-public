using Sandbox;
using System;

namespace Editor
{
	[AttributeUsage( AttributeTargets.Class )]
	public class DockAttribute : Attribute, ITypeAttribute
	{
		static Dictionary<string, DockWindow> Targets = new();
		static List<DockAttribute> All = new();

		Type ITypeAttribute.TargetType { get; set; }

		public static void RegisterWindow( string name, DockWindow b )
		{
			Targets[name] = b;

			foreach ( var m in All.Where( x => x.Target == name ) )
			{
				m.Register();
			}
		}

		void ITypeAttribute.TypeRegister()
		{
			All.Add( this );
			Register();
		}

		void Register()
		{
			if ( !Targets.TryGetValue( Target, out var window ) )
				return;

			var createAction = () =>
			{
				var widget = EditorTypeLibrary.Create<Widget>( (this as ITypeAttribute).TargetType, new object[] { window } );
				widget.WindowTitle = Name;
				widget.SetWindowIcon( Icon );
				widget.Name = Name;
				return widget;
			};

			window.DockManager.RegisterDockType( Name, Icon, createAction );
		}

		void ITypeAttribute.TypeUnregister()
		{
			All.Remove( this );

			if ( !Targets.TryGetValue( Target, out var window ) )
				return;

			window.DockManager.UnregisterDockType( Name );
		}

		public string Target { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }

		public DockAttribute( string target, string name, string icon = null )
		{
			Target = target;
			Name = name;
			Icon = icon;
		}
	}
}
