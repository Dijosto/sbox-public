using System;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace AtlantisStrategy.API;

/// <summary>
/// HTTP client for communicating with Cloudflare Workers backend.
/// Handles authentication, retries, and error handling.
/// </summary>
public class CloudflareClient
{
	private string _jwtToken;
	private DateTime _lastRequest = DateTime.MinValue;
	private const float MIN_REQUEST_INTERVAL = 0.05f; // 50ms rate limit client-side

	/// <summary>
	/// Current authentication JWT token
	/// </summary>
	public string AuthToken
	{
		get => _jwtToken;
		set => _jwtToken = value;
	}

	/// <summary>
	/// Is the client authenticated?
	/// </summary>
	public bool IsAuthenticated => !string.IsNullOrEmpty( _jwtToken );

	/// <summary>
	/// Make a GET request to the API
	/// </summary>
	public async Task<T> GetAsync<T>( string endpoint )
	{
		var response = await SendRequestAsync( "GET", endpoint, null );
		return JsonSerializer.Deserialize<T>( response );
	}

	/// <summary>
	/// Make a POST request to the API
	/// </summary>
	public async Task<T> PostAsync<T>( string endpoint, object payload = null )
	{
		var response = await SendRequestAsync( "POST", endpoint, payload );
		return JsonSerializer.Deserialize<T>( response );
	}

	/// <summary>
	/// Make a PUT request to the API
	/// </summary>
	public async Task<T> PutAsync<T>( string endpoint, object payload = null )
	{
		var response = await SendRequestAsync( "PUT", endpoint, payload );
		return JsonSerializer.Deserialize<T>( response );
	}

	/// <summary>
	/// Make a DELETE request to the API
	/// </summary>
	public async Task<T> DeleteAsync<T>( string endpoint )
	{
		var response = await SendRequestAsync( "DELETE", endpoint, null );
		return JsonSerializer.Deserialize<T>( response );
	}

	/// <summary>
	/// Core request method with retries and error handling
	/// </summary>
	private async Task<string> SendRequestAsync( string method, string endpoint, object payload )
	{
		// Client-side rate limiting
		var timeSinceLastRequest = (float)(DateTime.Now - _lastRequest).TotalSeconds;
		if ( timeSinceLastRequest < MIN_REQUEST_INTERVAL )
		{
			await Task.Delay( (int)((MIN_REQUEST_INTERVAL - timeSinceLastRequest) * 1000) );
		}

		_lastRequest = DateTime.Now;

		var url = $"{GameConfig.ApiBaseUrl}{endpoint}";
		var attempt = 0;
		Exception lastException = null;

		while ( attempt < GameConfig.MaxRetries )
		{
			try
			{
				if ( GameConfig.ApiDebug )
				{
					Log.Info( $"[API] {method} {endpoint} (attempt {attempt + 1})" );
				}

				var request = Http.RequestAsync( url );
				request.Method = method;

				// Add authentication header
				if ( IsAuthenticated )
				{
					request.SetRequestHeader( "Authorization", $"Bearer {_jwtToken}" );
				}

				// Add timestamp for server-side validation
				request.SetRequestHeader( "X-Client-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() );

				// Serialize payload for POST/PUT
				if ( payload != null && (method == "POST" || method == "PUT") )
				{
					request.SetRequestHeader( "Content-Type", "application/json" );
					request.Body = JsonSerializer.Serialize( payload );
				}

				// Send request
				var response = await request;

				// Handle HTTP errors
				if ( !response.IsSuccessStatusCode )
				{
					var errorBody = response.Body;

					// Rate limiting - don't retry
					if ( response.StatusCode == 429 )
					{
						Log.Warning( $"[API] Rate limited by server: {errorBody}" );
						throw new ApiException( "Rate limit exceeded", 429, errorBody );
					}

					// Authentication errors - don't retry
					if ( response.StatusCode == 401 || response.StatusCode == 403 )
					{
						Log.Warning( $"[API] Authentication failed: {errorBody}" );
						_jwtToken = null; // Clear invalid token
						throw new ApiException( "Authentication failed", response.StatusCode, errorBody );
					}

					// Client errors (400-499) - don't retry
					if ( response.StatusCode >= 400 && response.StatusCode < 500 )
					{
						Log.Warning( $"[API] Client error {response.StatusCode}: {errorBody}" );
						throw new ApiException( $"Client error: {response.StatusDescription}", response.StatusCode, errorBody );
					}

					// Server errors (500+) - retry with backoff
					if ( response.StatusCode >= 500 )
					{
						Log.Warning( $"[API] Server error {response.StatusCode}, will retry: {errorBody}" );
						lastException = new ApiException( $"Server error: {response.StatusDescription}", response.StatusCode, errorBody );
						attempt++;
						await Task.Delay( (int)Math.Pow( 2, attempt ) * 1000 ); // Exponential backoff
						continue;
					}
				}

				if ( GameConfig.ApiDebug )
				{
					Log.Info( $"[API] {method} {endpoint} -> {response.StatusCode}" );
				}

				return response.Body;
			}
			catch ( ApiException )
			{
				// Re-throw API exceptions without retry
				throw;
			}
			catch ( Exception ex )
			{
				Log.Warning( $"[API] Request failed: {ex.Message}" );
				lastException = ex;
				attempt++;

				if ( attempt < GameConfig.MaxRetries )
				{
					// Exponential backoff: 1s, 2s, 4s
					await Task.Delay( (int)Math.Pow( 2, attempt ) * 1000 );
				}
			}
		}

		// All retries exhausted
		throw new ApiException( "Request failed after all retries", 0, lastException?.Message ?? "Unknown error" );
	}
}

/// <summary>
/// Exception thrown by API calls
/// </summary>
public class ApiException : Exception
{
	public int StatusCode { get; }
	public string ResponseBody { get; }

	public ApiException( string message, int statusCode, string responseBody )
		: base( message )
	{
		StatusCode = statusCode;
		ResponseBody = responseBody;
	}
}
