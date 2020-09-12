using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode.Channels;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Internal;
using YoutubeExplode.Internal.Extensions;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace YoutubeExplode.ReverseEngineering.Responses
{
	internal partial class PlaylistResponse
	{
		private readonly JsonElement _root;

		public PlaylistResponse(JsonElement root) => _root = root;

		public string GetTitle() => _root
			.GetProperty("title")
			.GetString();

		public string? TryGetAuthor() => _root
			.GetPropertyOrNull("author")?
			.GetString();

		public string? TryGetDescription() => _root
			.GetPropertyOrNull("description")?
			.GetString();

		public long? TryGetViewCount() => _root
			.GetPropertyOrNull("views")?
			.GetInt64();

		public long? TryGetLikeCount() => _root
			.GetPropertyOrNull("likes")?
			.GetInt64();

		public long? TryGetDislikeCount() => _root
			.GetPropertyOrNull("dislikes")?
			.GetInt64();

		public IEnumerable<Video> GetVideos() => Fallback.ToEmpty(
			_root
				.GetPropertyOrNull("video")?
				.EnumerateArray()
				.Select(j => new Video(j))
		);
	}

	internal partial class PlaylistResponse
	{
		public class Video
		{
			private readonly JsonElement _root;

			public Video(JsonElement root) => _root = root;

			public string GetId() => _root
				.GetProperty("encrypted_id")
				.GetString();

			public string GetAuthor() => _root
				.GetProperty("author")
				.GetString();

			public string GetChannelId() => _root
				.GetProperty("user_id")
				.GetString()
				.Pipe(id => "UC" + id);

			public DateTimeOffset GetUploadDate() => _root
				.GetProperty("time_created")
				.GetInt64()
				.Pipe(Epoch.ToDateTimeOffset);

			public string GetTitle() => _root
				.GetProperty("title")
				.GetString();

			public string GetDescription() => _root
				.GetProperty("description")
				.GetString();

			public TimeSpan GetDuration() => _root
				.GetProperty("length_seconds")
				.GetDouble()
				.Pipe(TimeSpan.FromSeconds);

			public long GetViewCount() => _root
				.GetProperty("views")
				.GetString()
				.StripNonDigit()
				.ParseLong();

			public long GetLikeCount() => _root
				.GetProperty("likes")
				.GetInt64();

			public long GetDislikeCount() => _root
				.GetProperty("dislikes")
				.GetInt64();

			public IReadOnlyList<string> GetKeywords() => _root
				.GetProperty("keywords")
				.GetString()
				.Pipe(s => Regex.Matches(s, "\"[^\"]+\"|\\S+"))
				.Cast<Match>()
				.Select(m => m.Value)
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.Trim('"'))
				.ToArray();
		}
	}

	internal partial class PlaylistResponse
	{
		public static PlaylistResponse Parse(string raw) => new PlaylistResponse(
			Json.TryParse(raw) ?? throw TransientFailureException.Generic("Playlist response is broken.")
		);

		public static async Task<PlaylistResponse> GetAsync(YoutubeHttpClient httpClient, string id, int index = 0) =>
			await Retry.WrapAsync(async () =>
			{
				var url = $"https://youtube.com/list_ajax?style=json&action_get_list=1&list={id}&index={index}&hl=en";
				var raw = await httpClient.GetStringAsync(url).ConfigureAwait(false);

				return Parse(raw);
			});

		public static async Task<PlaylistResponse> GetSearchResultsAsync(YoutubeHttpClient httpClient, string query, int page = 0) =>
            await Retry.WrapAsync(async () =>
            {
                var queryEncoded = Uri.EscapeUriString(query);

                var url = $"https://youtube.com/search_ajax?style=json&search_query={queryEncoded}&page={page}&hl=en";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // These headers are required for some reason
                request.Headers.Add("x-youtube-client-name", "56");
                request.Headers.Add("x-youtube-client-version", "20200911");

                // Don't ensure success here but rather return an empty list
                using var response = await httpClient.SendAsync(request);
                var raw = await response.Content.ReadAsStringAsync();

                return Parse(raw);
            });
            

		public static async Task<IEnumerable<PlaylistSearchModel>> GetPlaylistSearchResultsAsync(YoutubeHttpClient httpClient, string querystring, int page = 0)
		{
			var items = new List<PlaylistSearchModel>();
			var url = "https://www.youtube.com/results?search_query=" + querystring.Replace(" ", "+") + "&sp=EgIQAw%253D%253D&page=" + page;
			var content = await httpClient.GetStringAsync(url).ConfigureAwait(false);

			// Search string
			var pattern = "playlistRenderer\":\\{\"playlistId\":\"(?<ID>.*?)\",\"title\":\\{\"simpleText\":\"(?<TITLE>.*?)\"},\"thumbnails\":\\[\\{\"thumbnails\":\\[\\{\"url\":\"(?<THUMBNAIL>.*?)\".*?videoCount\":\"(?<VIDEOCOUNT>.*?)\".*?\\{\"webCommandMetadata\":\\{\"url\":\"(?<URL>.*?)\".*?\"shortBylineText\":\\{\"runs\":\\[\\{\"text\":\"(?<AUTHOR>.*?)\"";
			MatchCollection result = Regex.Matches(content, pattern, RegexOptions.Singleline);

			for (int ctr = 0; ctr <= result.Count - 1; ctr++)
			{
				// Id
				var id = result[ctr].Groups[1].Value;



				// Title
				var title = result[ctr].Groups[2].Value;



				// Author
				var author = result[ctr].Groups[6].Value;



				// VideoCount
				var videoCount = result[ctr].Groups[4].Value;



				// Thumbnail
				var thumbnail = result[ctr].Groups[3].Value;



				// Url
				var urlVideo = "http://youtube.com" + result[ctr].Groups[5].Value.Replace(@"\u0026", "&");



				// Add item to list
				items.Add(new PlaylistSearchModel(id, WebUtility.HtmlDecode(title), WebUtility.HtmlDecode(author), videoCount, thumbnail, urlVideo));
			}
			return items;
		}

		public static async Task<IEnumerable<Videos.Video>> GetPlaylistItems(YoutubeHttpClient httpClient, string playlisturl)
		{
			var items = new List<Videos.Video>();

			// Do search
			// Search address
			string content = await httpClient.GetStringAsync(playlisturl).ConfigureAwait(false);

			// Search string
			string pattern = "playlistPanelVideoRenderer\":\\{\"title\":\\{\"simpleText\":\"(?<TITLE>.*?)\".*?runs\":\\[\\{\"text\":\"(?<AUTHOR>.*?)\".*?\":\\{\"thumbnails\":\\[\\{\"url\":\"(?<THUMBNAIL>.*?)\".*?\"}},\"simpleText\":\"(?<DURATION>.*?)\".*?videoId\":\"(?<URL>.*?)\"";
			MatchCollection result = Regex.Matches(content, pattern, RegexOptions.Singleline);

			for (int ctr = 0; ctr <= result.Count - 1; ctr++)
			{
				// VideoId
				var id = result[ctr].Groups[5].Value;
				var videoId = new VideoId(id);

				// Title
				var title = result[ctr].Groups[1].Value.Replace(@"\u0026", "&");


				// Author
				var author = result[ctr].Groups[2].Value.Replace(@"\u0026", "&");


				// Duration
				var duration = result[ctr].Groups[4].Value;
				TimeSpan.TryParse(duration, out var videoDuration);
				// Add item to list
				try
				{
					var video = new Videos.Video(
								videoId,
								WebUtility.HtmlDecode(title),
								WebUtility.HtmlDecode(author),
								new ChannelId(id),
								default,
								string.Empty,
								videoDuration,
								new Common.ThumbnailSet(videoId),
								default,
								null);
					items.Add(video);
				}
				catch (Exception)
				{
				}
			}

			return items;
		}

		public static Task<List<Videos.Video>> GetSearchResulsHtmlAsync(YoutubeHttpClient httpclient, string querystring, int page = 1) =>
			Retry.WrapAsync(async () =>
			{
				var items = new List<Videos.Video>();
				var viewCount = string.Empty;
				string content = await httpclient.GetStringAsync("https://www.youtube.com/results?search_query=" + querystring + "&page=" + page);

				content = Helper.ExtractValue(content, "window[\"ytInitialData\"]", "window[\"ytInitialPlayerResponse\"]");

				// Search string
				string pattern = "videoRenderer.*?serviceEndpoint";
				MatchCollection result = Regex.Matches(content, pattern, RegexOptions.Singleline);


				for (int ctr = 0; ctr <= result.Count - 1; ctr++)
				{
					// Title
					var title = Helper.ExtractValue(result[ctr].Value, "\"title\":{\"runs\":[{\"text\":\"", "\"}]").Replace(@"\u0026", "&");


					// Author
					var author = Helper.ExtractValue(result[ctr].Value, "\"ownerText\":{\"runs\":[{\"text\":\"", "\",\"").Replace(@"\u0026", "&");


					// Description
					var description = Helper.ExtractValue(result[ctr].Value, "descriptionSnippet\":{\"runs\":[{\"text\":\"", "\"}]},").Replace(@"\u0026", "&");


					// Duration
					var duration = Helper.ExtractValue(result[ctr].Value, "lengthText\"", "viewCountText");
					duration = Helper.ExtractValue(duration, "simpleText\":\"", "\"");
					TimeSpan.TryParse(duration, out var videoDuration);

					// Url
					var url = string.Concat("http://www.youtube.com/watch?v=", Helper.ExtractValue(result[ctr].Value, "videoId\":\"", "\""));


					// Thumbnail
					var thumbnail = Helper.ExtractValue(result[ctr].Value, "\"thumbnail\":{\"thumbnails\":[{\"url\":\"", "\"").Replace(@"\u0026", "&");


					// Id
					//var index = url.IndexOf('=');
					var id = url.Split('=')[1];
					// View count

					string strView = Helper.ExtractValue(result[ctr].Value, "\"viewCountText\":{\"simpleText\":\"", "\"},\"");
					if (!string.IsNullOrEmpty(strView) && !string.IsNullOrWhiteSpace(strView))
					{
						string[] strParsedArr =
							strView.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

						string parsedText = strParsedArr[0];
						parsedText = parsedText.Trim().Replace(",", ".");

						viewCount = parsedText;
					}


					// Remove playlists
					if (title != "__title__" && title != " ")
					{
						if (duration != "" && duration != " ")
						{
							// Add item to list
							var video = new Videos.Video(
								id,
								WebUtility.HtmlDecode(title),
								WebUtility.HtmlDecode(author),
								new ChannelId(id),
								default,
								string.Empty,
								videoDuration,
								new Common.ThumbnailSet(id),
								default,
								null);
							items.Add(video);
						}
					}
				}

				return items;
			});
	}
}

