using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
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

		private string _lastVideoSearchQuery = string.Empty;
		private string _lastHtmlVideoSearchQuery = string.Empty;

		/// <summary>
		/// Initializes an instance of <see cref="SearchClient"/>.
		/// </summary>
		internal SearchClient(YoutubeHttpClient httpClient)
		{
			_httpClient = httpClient;
			_encounteredVideoIds = new HashSet<string>();
		}

		/// <summary>
		/// Enumerates videos returned by the specified search query.
		/// </summary>
		public async IAsyncEnumerable<Video> GetVideosAsync(string searchQuery, int startPage, int endPage)
		{
			if (!searchQuery.Equals(_lastVideoSearchQuery, StringComparison.InvariantCultureIgnoreCase))
			{
				_lastVideoSearchQuery = searchQuery.ToLower();
				_encounteredVideoIds.Clear();
			}

			for (; startPage <= endPage; startPage++)
			{
				var response = await PlaylistResponse.GetSearchResultsAsync(_httpClient, searchQuery, startPage).ConfigureAwait(false);

				var htmlResponse = await PlaylistResponse.GetSearchResulsHtmlAsync(_httpClient,searchQuery, startPage).ConfigureAwait(false);
				
				foreach (var item in htmlResponse)
				{
					if (!_encounteredVideoIds.Add(item.Id))
						continue;

					yield return item;
				}

				foreach (var video in response.GetVideos())
				{
					var videoId = video.GetId();

					if (!_encounteredVideoIds.Add(videoId))
						continue;

					yield return new Video(
						videoId,
						video.GetTitle(),
						video.GetAuthor(),
						video.GetChannelId(),
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
				}
			}
		}

		public async IAsyncEnumerable<Video> GetVideosByHTMLAsync(string searchQuery, int startPage, int endPage)
		{
			if (!searchQuery.Equals(_lastHtmlVideoSearchQuery, StringComparison.InvariantCultureIgnoreCase))
			{
				_lastHtmlVideoSearchQuery = searchQuery.ToLower();
				_encounteredVideoIds.Clear();
			}

			for (; startPage < endPage; startPage++)
			{
				var response = await PlaylistResponse.GetSearchResulsHtmlAsync(_httpClient, searchQuery, startPage).ConfigureAwait(false);

				var size = response.Count;

				for (int i = 0; i < size; i++)
					yield return response[i];
			}

		}

		public async Task<IEnumerable<PlaylistSearchModel>> GetPlaylistAsync(string searchQuery, int startPage, int endPage)
		{
			var playlist = new List<PlaylistSearchModel>();

			for (; startPage <= endPage; startPage++)
			{
				var result = await PlaylistResponse.GetPlaylistSearchResultsAsync(_httpClient, searchQuery, startPage).ConfigureAwait(false);
				playlist.AddRange(result);
			}

			return playlist.Distinct();
		}

		public Task<IEnumerable<Video>> GetPlaylistVideosAsync(string playlistUrl) =>
			PlaylistResponse.GetPlaylistItems(_httpClient, playlistUrl);
	}
}
