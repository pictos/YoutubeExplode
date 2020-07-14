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

        private string lastVideoSearchQuery = string.Empty;
        private string lastPlaylistSearchQuery = string.Empty;

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
            if (!searchQuery.Equals(lastVideoSearchQuery, StringComparison.InvariantCultureIgnoreCase))
            {
                lastVideoSearchQuery = searchQuery.ToLower();
                _encounteredVideoIds = new HashSet<string>();
            }

            for (; startPage <= endPage; startPage++)
            {
                var response = await PlaylistResponse.GetSearchResultsAsync(_httpClient, searchQuery, startPage);

                var countDelta = 0;
                foreach (var video in response.GetVideos())
                {
                    var videoId = video.GetId();
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

                    // Skip already encountered videos
                    if (!_encounteredVideoIds.Add(videoId))
                        continue;

                    countDelta++;
                }

                // Videos loop around, so break when we stop seeing new videos
                if (countDelta <= 0)
                    break;
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