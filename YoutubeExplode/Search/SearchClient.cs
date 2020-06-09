using System;
using System.Collections.Generic;
using YoutubeExplode.Common;
using YoutubeExplode.ReverseEngineering;
using YoutubeExplode.ReverseEngineering.Responses;
using YoutubeExplode.Videos;

namespace YoutubeExplode.Search
{
	/// <summary>
	/// YouTube search queries.
	/// </summary>
	public class SearchClient
	{
		private readonly YoutubeHttpClient _httpClient;

		private HashSet<string> _encounteredVideoIds = new HashSet<string>();

		private string lastSearchQuery = string.Empty;

		/// <summary>
		/// Initializes an instance of <see cref="SearchClient"/>.
		/// </summary>
		internal SearchClient(YoutubeHttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		/// <summary>
		/// Enumerates videos returned by the specified search query.
		/// </summary>
		public async IAsyncEnumerable<Video> GetVideosAsync(string searchQuery, int startPage, int endPage)
		{
			if (!searchQuery.Equals(lastSearchQuery,StringComparison.InvariantCultureIgnoreCase))
			{
				lastSearchQuery = searchQuery.ToLower();
				_encounteredVideoIds = new HashSet<string>();
			}

			for (; startPage <= endPage; startPage++)
			{
				var response = await PlaylistResponse.GetSearchResultsAsync(_httpClient, searchQuery, startPage);

				var countDelta = 0;
				foreach (var video in response.GetVideos())
				{
					var videoId = video.GetId();

					// Skip already encountered videos
					if (!_encounteredVideoIds.Add(videoId))
						continue;

					yield return new Video(
						videoId,
						video.GetTitle(),
						video.GetAuthor(),
						video.GetUploadDate(),
						video.GetDescription(),
						video.GetDuration(),
						new ThumbnailSet(videoId),
						video.GetKeywords(),
						new Engagement(
							video.GetViewCount(),
							video.GetLikeCount(),
							video.GetDislikeCount()
						)
					);

					countDelta++;
				}

				// Videos loop around, so break when we stop seeing new videos
				if (countDelta <= 0)
					break;
			}
		}
	}
}