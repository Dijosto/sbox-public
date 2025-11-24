using Sandbox.Internal;

namespace Sandbox.UI;

public partial class Panel
{
	IPanel IPanel.Parent => Parent;
	IEnumerable<IPanel> IPanel.Children => Children;
	int IPanel.ChildrenCount => ChildrenCount;
	string IPanel.ElementName => ElementName;

	bool IPanel.IsMainMenu => Game.IsMenu;
	bool IPanel.IsGame => !Game.IsMenu;
	bool IPanel.IsVisible => IsVisible;
	bool IPanel.IsVisibleSelf => IsVisibleSelf;
	string IPanel.Classes => Classes;
	Rect IPanel.Rect => Box.Rect;
	Rect IPanel.OuterRect => Box.RectOuter;
	Rect IPanel.InnerRect => Box.RectInner;
	Matrix? IPanel.GlobalMatrix => GlobalMatrix;
	bool IPanel.HasTooltip => HasTooltip;
	bool IPanel.WantsPointerEvents => (ComputedStyle?.PointerEvents ?? PointerEvents.None) == PointerEvents.All;

	IPanel IPanel.GetPanelAt( Vector2 point, bool visibleOnly, bool needPointerEvents ) => GetPanelAt( point, visibleOnly, needPointerEvents );
	bool IPanel.IsAncestor( IPanel panel ) => IsAncestor( panel as Panel );

	Panel GetPanelAt( Vector2 point, bool visibleOnly, bool needPointerEvents = false )
	{
		if ( visibleOnly && !IsVisible ) return null;

		point = LocalMatrix?.Transform( point ) ?? point;

		if ( !IsInside( point ) ) return null;

		Panel bestSelection = this;

		foreach ( var child in Children.OrderByDescending( x => x.GetRenderOrderIndex() ).ThenByDescending( x => x.SiblingIndex ) )
		{
			var p = child.GetPanelAt( point, visibleOnly, needPointerEvents );

			if ( !p.IsValid() ) continue;

			bestSelection = p;
			break;
		}

		if ( bestSelection == this && needPointerEvents && !(this as IPanel).WantsPointerEvents )
			return null;

		return bestSelection;
	}

	int Depth => 1 + (Parent?.Depth ?? 0);

	IPanel IPanel.CreateTooltip() => CreateTooltipPanel();
	void IPanel.Delete( bool immediate ) => Delete( immediate );

	/// <summary>
	/// If the tooltip text changed, we'll update it here. I haven't exposed this to game code yet
	/// because I doubt the usefulness to people that are manually creating tooltip panels.
	/// </summary>
	void IPanel.UpdateTooltip( IPanel tooltipPanel )
	{
		if ( tooltipPanel is not Panel p ) return;
		if ( !p.HasChildren ) return;
		if ( p.ChildrenCount != 1 ) return;
		if ( p.Children.First() is not Sandbox.UI.Label textPanel ) return;

		textPanel.Text = Tooltip;
	}

	void IPanel.SetAbsolutePosition( TextFlag alignment, Vector2 position, float offset )
	{
		Style.Left = null;
		Style.Right = null;
		Style.Top = null;
		Style.Bottom = null;

		if ( (alignment & TextFlag.Left) != 0 )
		{
			Style.Right = ((Screen.Size.x - position.x) + offset) * ScaleFromScreen;
		}

		if ( (alignment & TextFlag.Right) != 0 )
		{
			Style.Left = (offset + position.x) * ScaleFromScreen;
		}

		if ( (alignment & TextFlag.Top) != 0 )
			Style.Bottom = ((Screen.Size.y - position.y) + offset) * ScaleFromScreen;

		if ( (alignment & TextFlag.Bottom) != 0 )
			Style.Top = (offset + position.y) * ScaleFromScreen;
	}
}
