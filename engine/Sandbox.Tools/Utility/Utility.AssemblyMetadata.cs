using Mono.Cecil;
using System.IO;
namespace Editor;

public static partial class EditorUtility
{

	public static partial class AssemblyMetadata
	{
		public struct Attribute
		{
			private CustomAttribute attr;

			public string AttributeType => attr.AttributeType.Name;
			public string AttributeFullName => attr.AttributeType.FullName;

			public object[] Arguments => attr.ConstructorArguments.Select( x => x.Value ).ToArray();

			public Attribute( CustomAttribute x ) : this()
			{
				this.attr = x;
			}
		}

		public static Attribute[] GetCustomAttributes( byte[] assemblyData )
		{
			using var ms = new MemoryStream( assemblyData );
			var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly( ms );

			return assembly.CustomAttributes.Select( x => new Attribute( x ) ).ToArray();
		}
	}
}
