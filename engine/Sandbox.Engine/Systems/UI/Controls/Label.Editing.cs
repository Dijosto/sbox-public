using System.Globalization;

namespace Sandbox.UI;

public partial class Label
{
	/// <summary>
	/// Enables multi-line support for editing purposes.
	/// </summary>
	public bool Multiline { get; set; } = true;
	// Sol: TODO: unlink this from wrapping, add css text-wrap or something

	private Vector2 caretScroll;

	/// <summary>
	/// Replace the currently selected text with given text.
	/// </summary>
	public void ReplaceSelection( string str )
	{
		var s = Math.Min( SelectionStart, SelectionEnd );
		var e = Math.Max( SelectionStart, SelectionEnd );
		var len = e - s;

		if ( CaretPosition > e ) CaretPosition -= len;
		else if ( CaretPosition > s ) CaretPosition = s;

		CaretPosition += new StringInfo( str ).LengthInTextElements;
		InsertText( str, s, e );

		SelectionStart = 0;
		SelectionEnd = 0;
	}

	/// <summary>
	/// Sets the text selection.
	/// </summary>
	public void SetSelection( int start, int end )
	{
		var s = Math.Min( start, end ).Clamp( 0, TextLength );
		var e = Math.Max( start, end ).Clamp( 0, TextLength );

		if ( s == e )
		{
			s = 0;
			e = 0;
		}

		SelectionStart = s;
		SelectionEnd = e;
	}

	/// <summary>
	/// Set the text caret position to the given index.
	/// </summary>
	/// <param name="pos">Where to move the text caret to within the text.</param>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void SetCaretPosition( int pos, bool select = false )
	{
		if ( SelectionEnd == 0 && SelectionStart == 0 && select )
		{
			SelectionStart = CaretPosition.Clamp( 0, TextLength );
		}

		CaretPosition = pos.Clamp( 0, TextLength );

		if ( select )
		{
			SelectionEnd = CaretPosition;
		}
		else
		{
			SelectionEnd = 0;
			SelectionStart = 0;
		}

		ScrollToCaret();
	}

	/// <summary>
	/// Put the caret within the visible region.
	/// </summary>
	public void ScrollToCaret()
	{
		// Sol: not supported right now. think would work totally differently with "proper" scrollbars etc?
		if ( Multiline ) return;
		if ( _textBlock is null ) return;

		_textBlock.ScrollToCaret( CaretPosition, ref caretScroll, Box.RectInner.Size );
	}

	/// <summary>
	/// Move the text caret to the closest word start or end to the left of current position.<br/>
	/// This simulates holding Control key while pressing left arrow key.
	/// </summary>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveToWordBoundaryLeft( bool select )
	{
		var boundaries = GetWordBoundaryIndices();
		var left = boundaries.LastOrDefault( x => x < CaretPosition );

		MoveCaretPos( left - CaretPosition, select );
	}

	/// <summary>
	/// Move the text caret to the closest word start or end to the right of current position.<br/>
	/// This simulates holding Control key while pressing right arrow key.
	/// </summary>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveToWordBoundaryRight( bool select )
	{
		var boundaries = GetWordBoundaryIndices();
		var right = boundaries.FirstOrDefault( x => x >= CaretPosition );

		if ( right == 0 ) return;

		MoveCaretPos( right - CaretPosition, select );
	}

	/// <summary>
	/// Move the text caret by given amount.
	/// </summary>
	/// <param name="delta">How many characters to the right to move. Negative values move left.</param>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveCaretPos( int delta, bool select = false )
	{
		SetCaretPosition( CaretPosition + delta, select );
	}

	/// <summary>
	/// Insert given text at given position.
	/// </summary>
	/// <param name="text">Text to insert.</param>
	/// <param name="pos">Position to insert the text at.</param>
	/// <param name="endpos">If set, the end position in the current <see cref="Text"/>,
	/// which will be used to replace portion of the existing text with the given <paramref name="text"/>.</param>
	public void InsertText( string text, int pos, int? endpos = null )
	{
		CaretSantity();

		pos = Math.Clamp( pos, 0, TextLength );
		if ( endpos.HasValue ) endpos = Math.Clamp( endpos.Value, 0, TextLength );

		var a = pos > 0 ? StringInfo.SubstringByTextElements( 0, pos ) : "";
		var b = "";

		if ( endpos.HasValue )
		{
			if ( endpos < TextLength ) b = StringInfo.SubstringByTextElements( endpos.Value );
		}
		else
		{
			if ( pos < TextLength ) b = StringInfo.SubstringByTextElements( pos );
		}

		Text = $"{a}{text}{b}";
	}

	/// <summary>
	/// Remove given amount of characters from the label at given <paramref name="start"/> position.
	/// </summary>
	public virtual void RemoveText( int start, int count )
	{
		var a = start > 0 ? StringInfo.SubstringByTextElements( 0, start ) : "";
		var b = (start + count < TextLength) ? StringInfo.SubstringByTextElements( start + count ) : "";

		Text = a + b;
	}

	/// <summary>
	/// Move the text caret to the start of the current line.
	/// </summary>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveToLineStart( bool select = false )
	{
		if ( !Multiline )
		{
			SetCaretPosition( 0, select );
			return;
		}

		int iNewline = 0;
		var e = StringInfo.GetTextElementEnumerator( Text );
		while ( e.MoveNext() )
		{
			if ( e.ElementIndex >= CaretPosition )
				break;

			if ( IsNewline( e.GetTextElement() ) )
				iNewline = e.ElementIndex + 1;
		}

		SetCaretPosition( iNewline, select );
	}

	/// <summary>
	/// Move the text caret to the end of the current line.
	/// </summary>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveToLineEnd( bool select = false )
	{
		if ( !Multiline )
		{
			SetCaretPosition( TextLength, select );
			return;
		}

		var e = StringInfo.GetTextElementEnumerator( Text );
		while ( e.MoveNext() )
		{
			if ( e.ElementIndex < CaretPosition )
				continue;

			if ( IsNewline( e.GetTextElement() ) )
			{
				SetCaretPosition( e.ElementIndex, select );
				return;
			}
		}

		SetCaretPosition( TextLength, select );
	}

	/// <summary>
	/// Move the text caret to next or previous line.
	/// </summary>
	/// <param name="offset_line">How many lines to offset. Negative values move up.</param>
	/// <param name="select">Whether to also add the characters we passed by to the selection.</param>
	public void MoveCaretLine( int offset_line, bool select )
	{
		if ( !Multiline )
		{
			if ( offset_line < 0 ) SetCaretPosition( 0, select );
			if ( offset_line > 0 ) SetCaretPosition( TextLength, select );
			return;
		}

		var caret = GetCaretRect( CaretPosition );

		var height = caret.Size;
		height.x = 0;

		var click = caret.Position + caret.Size * 0.5f + height * offset_line * 1.2f;
		var pos = GetLetterAtScreenPosition( click );
		SetCaretPosition( pos, select );
	}

	/// <summary>
	/// Select a work at given word position.
	/// </summary>
	public void SelectWord( int wordPos )
	{
		if ( TextLength == 0 )
			return;

		var boundaries = GetWordBoundaryIndices();
		SelectionStart = boundaries.LastOrDefault( x => x < wordPos );
		SelectionEnd = boundaries.FirstOrDefault( x => x >= wordPos );

		CaretPosition = SelectionEnd;
	}

	/// <summary>
	/// Returns a list of positions in the text of each side of each word within the <see cref="Text"/>.<br/>
	/// This is used for Control + Arrow Key navigation.
	/// </summary>
	public List<int> GetWordBoundaryIndices()
	{
		var result = new List<int>() { 0, StringInfo.LengthInTextElements };
		var e = StringInfo.GetTextElementEnumerator( Text );
		var input = string.Empty;

		// make it work with graphemes by assuming everything is 1 char long
		while ( e.MoveNext() )
		{
			input += e.GetTextElement()[0];
		}

		var match = System.Text.RegularExpressions.Regex.Match( input, @"\b" );

		while ( match.Success )
		{
			result.Add( match.Index );
			match = match.NextMatch();
		}

		result = result.Distinct().ToList();
		result.Sort();

		return result;
	}

	/// <summary>
	/// Returns true if the input string is a 1 or 2 (\r\n) character newline symbol.
	/// </summary>
	private bool IsNewline( string str )
	{
		if ( str == "\n" ) return true;
		if ( str == "\r\n" ) return true;
		if ( str == "\r" ) return true;

		return false;
	}
}
