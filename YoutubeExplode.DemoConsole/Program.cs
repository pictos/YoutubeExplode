using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.DemoConsole.Internal;
using YoutubeExplode.Playlists;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeExplode.DemoConsole
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            Console.Title = "YoutubeExplode Demo";

            // This demo prompts for video ID and downloads one media stream
            // It's intended to be very simple and straight to the point
            // For a more complicated example - check out the WPF demo

            var youtube = new YoutubeClient();
			await GetVideosAsync(youtube);
			var playlist = (await youtube.Search.GetPlaylistAsync("cicero cosmo", 0, 1)).ToList();

			var videos = (await youtube.Search.GetPlaylistVideosAsync(playlist[0].Url)).ToList();

			var z = videos[0];

			//var p = (await GetPlaylist(youtube)).ToList();
			//await GetVideosAsync(youtube);
			//IEnumerable<Video>? videos = await youtube.Search.GetPlaylistVideosAsync(p[0].Url).ConfigureAwait(false);
			//// Read the video ID1
			//Console.Write("Enter YouTube video ID or URL: ");
			//var videoId = new VideoId(Console.ReadLine());

			//// Get media streams & choose the best muxed stream
			//var streams = await youtube.Videos.Streams.GetManifestAsync(videoId);
			//var streamInfo = streams.GetMuxed().WithHighestVideoQuality();
			//if (streamInfo == null)
			//{
			//    Console.Error.WriteLine("This videos has no streams");
			//    return -1;
			//}

			//// Compose file name, based on metadata
			//var fileName = $"{videoId}.{streamInfo.Container.Name}";

			//// Download video
			//Console.Write($"Downloading stream: {streamInfo.VideoQualityLabel} / {streamInfo.Container.Name}... ");
			//using (var progress = new InlineProgress())
			//    await youtube.Videos.Streams.DownloadAsync(streamInfo, fileName, progress);

			//Console.WriteLine($"Video saved to '{fileName}'");
			return 0;
        }

        private static async Task<IEnumerable<Video>> GetPlaylist(YoutubeClient youtube)
        {
            var z =  await youtube.Search.GetVideosByHTMLAsync("Cicero cosmo", 0, 1);

			foreach (var item in z)
			{
				Console.WriteLine($"Titulo:{item.Title}, Url:{item.Url}");
			}

            return z ;
        }

        static async Task GetVideosAsync(YoutubeClient youtube)
		{
            var p = (await youtube.Search.GetVideosAsync("Cicero cosmo", 0, 1)).ToList();
            foreach (var item in p)
            {
                Console.WriteLine($"Titulo:{item.Title}, Url:{item.Url}");
            }

            var z = await youtube.Videos.Streams.GetManifestAsync(p[0].Id);
        }
    }
}
