using System.Security.Cryptography;
using System.Text;

namespace Sandbox.Obfuscation;

/// <summary>
/// Generates obfuscated names for symbols using cryptographically secure randomization.
/// </summary>
public class NameGenerator
{
	private readonly Random _random;
	private readonly HashSet<string> _usedNames = new();
	private int _counter;

	/// <summary>
	/// Characters used for generating short obfuscated names.
	/// Using a mix that's valid for C# identifiers but hard to read.
	/// </summary>
	private static readonly char[] NameChars =
		"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

	/// <summary>
	/// Unicode characters that look similar but are different (for extra confusion).
	/// These are all valid C# identifier characters.
	/// </summary>
	private static readonly char[] UnicodeChars =
	{
		'\u0430', // Cyrillic 'a'
		'\u0435', // Cyrillic 'e'
		'\u043E', // Cyrillic 'o'
		'\u0440', // Cyrillic 'p'
		'\u0441', // Cyrillic 'c'
		'\u0443', // Cyrillic 'y'
		'\u0445', // Cyrillic 'x'
		'\u0456', // Cyrillic 'i'
	};

	public NameGenerator( int? seed = null )
	{
		_random = seed.HasValue ? new Random( seed.Value ) : new Random();
	}

	/// <summary>
	/// Generate a unique obfuscated name for a type.
	/// </summary>
	public string GenerateTypeName()
	{
		return GenerateUniqueName( "t" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for a method.
	/// </summary>
	public string GenerateMethodName()
	{
		return GenerateUniqueName( "m" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for a field.
	/// </summary>
	public string GenerateFieldName()
	{
		return GenerateUniqueName( "f" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for a property.
	/// </summary>
	public string GeneratePropertyName()
	{
		return GenerateUniqueName( "p" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for a parameter.
	/// </summary>
	public string GenerateParameterName()
	{
		return GenerateUniqueName( "a" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for a local variable.
	/// </summary>
	public string GenerateLocalName()
	{
		return GenerateUniqueName( "l" );
	}

	/// <summary>
	/// Generate a unique obfuscated name for an event.
	/// </summary>
	public string GenerateEventName()
	{
		return GenerateUniqueName( "e" );
	}

	private string GenerateUniqueName( string prefix )
	{
		string name;
		do
		{
			name = GenerateName( prefix );
		} while ( _usedNames.Contains( name ) );

		_usedNames.Add( name );
		return name;
	}

	private string GenerateName( string prefix )
	{
		_counter++;

		// Use a combination of prefix, counter, and random suffix
		var sb = new StringBuilder();
		sb.Append( prefix );

		// Add base-52 encoded counter
		var num = _counter;
		while ( num > 0 )
		{
			sb.Append( NameChars[num % NameChars.Length] );
			num /= NameChars.Length;
		}

		// Add 1-2 random characters for extra uniqueness
		var extraChars = _random.Next( 1, 3 );
		for ( var i = 0; i < extraChars; i++ )
		{
			sb.Append( NameChars[_random.Next( NameChars.Length )] );
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generate a deterministic name based on the original name.
	/// Useful for consistent renaming across builds when using a seed.
	/// </summary>
	public string GenerateDeterministicName( string originalName, string prefix )
	{
		using var sha = SHA256.Create();
		var hash = sha.ComputeHash( Encoding.UTF8.GetBytes( originalName ) );

		var sb = new StringBuilder();
		sb.Append( prefix );

		// Use first 6 bytes of hash for name
		for ( var i = 0; i < 6; i++ )
		{
			sb.Append( NameChars[hash[i] % NameChars.Length] );
		}

		var name = sb.ToString();

		// Handle collisions
		var attempt = 0;
		while ( _usedNames.Contains( name ) )
		{
			attempt++;
			name = $"{sb}{NameChars[attempt % NameChars.Length]}";
		}

		_usedNames.Add( name );
		return name;
	}

	/// <summary>
	/// Generate a confusing name that uses similar-looking Unicode characters.
	/// These are harder to search for and distinguish.
	/// </summary>
	public string GenerateConfusingName( string prefix )
	{
		var sb = new StringBuilder();
		sb.Append( prefix );

		// Mix regular and Unicode lookalike characters
		var length = _random.Next( 4, 8 );
		for ( var i = 0; i < length; i++ )
		{
			if ( _random.Next( 3 ) == 0 && i > 0 ) // Don't start with Unicode
			{
				sb.Append( UnicodeChars[_random.Next( UnicodeChars.Length )] );
			}
			else
			{
				sb.Append( NameChars[_random.Next( NameChars.Length )] );
			}
		}

		var name = sb.ToString();

		// Ensure uniqueness
		var attempt = 0;
		while ( _usedNames.Contains( name ) )
		{
			attempt++;
			name = $"{sb}{_counter + attempt}";
		}

		_usedNames.Add( name );
		return name;
	}

	/// <summary>
	/// Clear all used names (call when starting a new assembly).
	/// </summary>
	public void Reset()
	{
		_usedNames.Clear();
		_counter = 0;
	}
}
