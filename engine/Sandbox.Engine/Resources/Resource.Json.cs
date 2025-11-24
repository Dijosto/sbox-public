using Sandbox.Resources;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sandbox;

public partial class Resource
{
	/// <summary>
	/// Embedded data for this resource
	/// </summary>
	[Hide, JsonIgnore]
	public EmbeddedResource? EmbeddedResource { get; set; }

	/// <summary>
	/// Read the resource from a JSON element. This is usually a string, describing the path to the resource
	/// </summary>
	static object IJsonConvert.JsonRead( ref Utf8JsonReader reader, Type typeToConvert )
	{
		return LoadJsonReference( typeToConvert, ref reader );
	}

	/// <summary>
	/// Write the resource reference to a json element. This is usually a string, describing the path to the resource
	/// </summary>
	static void IJsonConvert.JsonWrite( object value, Utf8JsonWriter writer )
	{
		if ( value is not Resource resource )
		{
			writer.WriteNullValue();
			return;
		}

		resource.WriteJsonReference( writer );
	}

	/// <summary>
	/// Load a resource from a path with improved deferred loading support
	/// </summary>
	internal static Resource LoadFromPath( Type typeToConvert, string path )
	{
		if ( typeToConvert.IsAssignableTo( typeof( GameResource ) ) )
		{
			//
			// GameResource: Fetch it from the cache, or setup a deferred load
			//

			if ( !path.EndsWith( "_c" ) ) path += "_c";

			// at this point the type may be a common base class
			// but we want to make sure we're loading this resource as the type it ACTUALLY is
			var extension = System.IO.Path.GetExtension( path );
			if ( Game.Resources.TryGetType( extension, out var resourceAttribute ) )
			{
				typeToConvert = resourceAttribute.TargetType;
			}

			return GameResource.GetPromise( typeToConvert, path );
		}

		// For native resource types, use direct loading
		return Load( typeToConvert, path );
	}

	/// <summary>
	/// Load a resource reference from JSON data.
	/// Handles both string paths and embedded resource objects for all resource types.
	/// </summary>
	internal static Resource LoadJsonReference( Type targetType, ref Utf8JsonReader reader )
	{
		// Just a path?
		if ( reader.TokenType == JsonTokenType.String )
		{
			return LoadFromPath( targetType, reader.GetString() );
		}

		// from an object (embedded resource)
		if ( reader.TokenType == JsonTokenType.StartObject )
		{
			EmbeddedResource serializedResource;

			try
			{
				serializedResource = JsonSerializer.Deserialize<EmbeddedResource>( ref reader );
			}
			catch ( System.Exception e )
			{
				Log.Warning( e, $"Couldn't deserialize resource data for {targetType.Name}" );
				return default;
			}

			//
			// If there's a compiled version then use it, but store the generation data too
			//
			if ( !string.IsNullOrWhiteSpace( serializedResource.CompiledPath ) )
			{
				var resource = LoadFromPath( targetType, serializedResource.CompiledPath );

				// Store embedded resource data if the resource supports it
				if ( resource is not null )
				{
					resource.EmbeddedResource = serializedResource;
				}

				return resource;
			}

			//
			// This is an embedded type, it's edited inline wherever it is
			// We could make this applicable for Resources too surely, the only thing stopping us is PushDeserializationScope
			//
			if ( targetType.IsAssignableTo( typeof( GameResource ) ) && serializedResource.ResourceCompiler == "embed" )
			{
				// 
				// Inherited resource
				//
				if ( !string.IsNullOrEmpty( serializedResource.TypeName ) )
				{
					var type = Game.TypeLibrary.GetType( serializedResource.TypeName );
					if ( type is not null )
					{
						targetType = type.TargetType;
					}
				}

				var resource = System.Activator.CreateInstance( targetType ) as GameResource;
				if ( resource is not null )
				{
					resource.Deserialize( serializedResource.Data );
					resource.EmbeddedResource = serializedResource;

					return resource;
				}
			}

			var options = ResourceGenerator.Options.Default;
			return ResourceGenerator.CreateResource( serializedResource, options, targetType );
		}

		// not found, null, empty, unhandled
		return default;
	}

	/// <summary>
	/// Allows a resource type to override how it reference entry is written. This is generally
	/// just going to be a path to the on disk resource, but we can use this to store metadata too.
	/// </summary>
	internal virtual void WriteJsonReference( Utf8JsonWriter writer )
	{
		// if we have an embedded resource, write that instead of the path
		if ( EmbeddedResource.HasValue )
		{
			//
			// This is an embedded resource, so we want to store all the data inline
			//
			if ( EmbeddedResource.Value is var resource && resource.ResourceCompiler == "embed" )
			{
				EmbeddedResource = resource with { Data = Json.SerializeAsObject( this ) };
			}

			JsonSerializer.Serialize( writer, EmbeddedResource );
			return;
		}

		// default write ResourcePath
		writer.WriteStringValue( ResourcePath );
	}
}
