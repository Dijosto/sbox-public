
namespace Sandbox.UI;

public partial class Panel
{
	/// <summary>
	/// Set via <c>"value"</c> property from HTML.
	/// </summary>
	[Hide]
	public virtual string StringValue { get; set; }

	/// <summary>
	/// Call this when the value has changed due to user input etc. This updates any
	/// bindings, backwards. Also triggers $"{name}.changed" event, with value being the Value on the event.
	/// </summary>
	protected void CreateValueEvent( string name, object value )
	{
		CreateEvent( $"{name}.changed", value );
	}
}
