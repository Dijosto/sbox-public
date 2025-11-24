using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sandbox.Twitch
{
	internal partial class TwitchAPI
	{
		public class UserResponse
		{
			[JsonPropertyName( "id" )]
			public string Id { get; set; }

			[JsonPropertyName( "login" )]
			public string Login { get; set; }

			[JsonPropertyName( "display_name" )]
			public string DisplayName { get; set; }

			[JsonPropertyName( "type" )]
			public string UserType { get; set; }

			[JsonPropertyName( "broadcaster_type" )]
			public string BroadcasterType { get; set; }

			[JsonPropertyName( "description" )]
			public string Description { get; set; }

			[JsonPropertyName( "profile_image_url" )]
			public string ProfileImageUrl { get; set; }

			[JsonPropertyName( "offline_image_url" )]
			public string OfflineImageUrl { get; set; }

			[JsonPropertyName( "view_count" )]
			public int ViewCount { get; set; }

			[JsonPropertyName( "email" )]
			public string Email { get; set; }

			[JsonPropertyName( "created_at" )]
			public string CreatedAt { get; set; }
		}

		public class UsersResponse
		{
			[JsonPropertyName( "data" )]
			public UserResponse[] Users { get; set; }

			public UserResponse FirstOrDefault() => Users != null && Users.Length > 0 ? Users.FirstOrDefault() : null;
		}

		public class UserFollowResponse
		{
			[JsonPropertyName( "from_id" )]
			public string FromId { get; set; }

			[JsonPropertyName( "from_login" )]
			public string FromLogin { get; set; }

			[JsonPropertyName( "from_name" )]
			public string FromName { get; set; }

			[JsonPropertyName( "to_id" )]
			public string ToId { get; set; }

			[JsonPropertyName( "to_login" )]
			public string ToLogin { get; set; }

			[JsonPropertyName( "to_name" )]
			public string ToName { get; set; }

			[JsonPropertyName( "followed_at" )]
			public string FollowedAt { get; set; }
		}

		public class UserFollowsResponse
		{
			[JsonPropertyName( "data" )]
			public UserFollowResponse[] UserFollows { get; set; }
		}

		public async Task<UserResponse> GetUser( string username = null )
		{
			var response = await Get<UsersResponse>( string.IsNullOrEmpty( username ) ?
				$"/users" :
				$"/users?login={username}" );

			return response.FirstOrDefault();
		}

		public Task<UserFollowsResponse> GetUserFollowing( string userId )
		{
			return Get<UserFollowsResponse>( $"/users/follows?from_id={userId}" );
		}

		public Task<UserFollowsResponse> GetUserFollowers( string userId )
		{
			return Get<UserFollowsResponse>( $"/users/follows?to_id={userId}" );
		}
	}
}

