using System.Text.Json.Nodes;

namespace Editor;

public static class SceneEditor
{
	/// <summary>
	/// Is there a <see cref="Component"/> type in the clipboard?
	/// </summary>
	public static bool HasComponentInClipboard()
	{
		var text = EditorUtility.Clipboard.Paste();

		try
		{
			if ( JsonNode.Parse( text ) is JsonObject jso )
			{
				var componentType = TypeLibrary.GetType<Component>( (string)jso["__type"] );
				return componentType is not null;
			}
		}
		catch
		{
			// Do nothing.
		}

		return false;
	}
}
