using Sandbox.Internal;
using System.Text.Json.Nodes;

namespace Sandbox.Audio;

public partial class Mixer
{
	public JsonObject Serialize()
	{
		lock ( Lock )
		{
			var js = new JsonObject();

			js["Guid"] = Id;
			js["Name"] = Name;
			js["Volume"] = Volume;
			js["Mute"] = Mute;
			js["Solo"] = Solo;

			js["Spacializing"] = Spacializing;
			js["MaxVoices"] = MaxVoices;
			js["DistanceAttenuation"] = DistanceAttenuation;
			js["Occlusion"] = Occlusion;
			js["AirAbsorption"] = AirAbsorption;

			js["OverrideOcclusion"] = OverrideOcclusion;
			js["OcclusionTags"] = (OcclusionTags?.IsEmpty ?? true) ? null : Json.ToNode( OcclusionTags );


			if ( Mixer.Default == this )
			{
				js["IsDefault"] = true;
			}

			var array = new JsonArray();

			foreach ( var processor in GetProcessors() )
			{
				var p = processor.Serialize();
				if ( p is null ) continue;

				array.Add( p );
			}

			js["Processors"] = array;

			var children = new JsonArray();
			foreach ( var child in Children )
			{
				children.Add( child.Serialize() );
			}

			js["Children"] = children;

			return js;
		}
	}

	protected void SetMasterOcclusionDefaults()
	{
		OverrideOcclusion = true;
		OcclusionTags.RemoveAll();
		OcclusionTags.Add( "world" );
	}

	public void Deserialize( JsonObject js, TypeLibrary typeLibrary )
	{
		lock ( Lock )
		{
			Id = (Guid)(js["Guid"] ?? Id);
			Name = (string)(js["Name"] ?? "Unnammed Mixer");
			Volume = (float)(js["Volume"] ?? 1.0f);
			Mute = (bool)(js["Mute"] ?? false);
			Solo = (bool)(js["Solo"] ?? false);

			// for these tracks, turn all this fun spatial stuff off by default
			bool is2d = Name == "Music" || Name == "UI";

			Spacializing = (float)(js["Spacializing"] ?? (is2d ? 0.0f : 1.0f));
			DistanceAttenuation = (float)(js["DistanceAttenuation"] ?? (is2d ? 0.0f : 1.0f));
			Occlusion = (float)(js["Occlusion"] ?? (is2d ? 0.0f : 1.0f));
			AirAbsorption = (float)(js["AirAbsorption"] ?? (is2d ? 0.0f : 1.0f));
			MaxVoices = js.GetPropertyValue( "MaxVoices", 64 );

			OcclusionTags = js.GetPropertyValue<TagSet>( "OcclusionTags", null ) ?? new();
			OverrideOcclusion = js.GetPropertyValue( "OverrideOcclusion", false );

			// If we're the master and we don't have any occlusion tags
			// lets add world on there as the default.
			if ( Parent == null && !js.ContainsKey( "OcclusionTags" ) && !js.ContainsKey( "OverrideOcclusion" ) )
			{
				SetMasterOcclusionDefaults();
			}

			Clear();

			if ( (bool)(js["IsDefault"] ?? false) )
			{
				Mixer.Default = this;
			}

			if ( js["Children"] is JsonArray children )
			{
				foreach ( var child in children )
				{
					var mixer = AddChild();
					mixer.Deserialize( (JsonObject)child, typeLibrary );
				}
			}

			if ( js["Processors"] is JsonArray processors )
			{
				foreach ( var processor in processors )
				{
					var type = processor["__type"]?.ToString();
					if ( type is null ) continue;

					var p = typeLibrary.Create<AudioProcessor>( type );
					if ( p is null )
					{
						Log.Warning( $"Unknown processor type '{type}' when loading mixer '{Name}'" );
						continue;
					}

					p.Deserialize( processor as JsonObject );
					AddProcessor( p );
				}
			}
		}
	}

	void Clear()
	{
		ClearAllProcessors();

		foreach ( var child in GetChildren() )
		{
			child.Destroy();
		}
	}

	void ClearAllProcessors()
	{
		ClearProcessors();

		foreach ( var child in GetChildren() )
		{
			child.ClearAllProcessors();
		}
	}
}
