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
            List<PlaylistSearchModel>? p = (await GetPlaylist(youtube)).ToList();

            IEnumerable<Video>? videos = await youtube.Search.GetPlaylistVideosAsync(p[0].Url).ConfigureAwait(false);
            // Read the video ID1
            Console.Write("Enter YouTube video ID or URL: ");
            var videoId = new VideoId(Console.ReadLine());

            // Get media streams & choose the best muxed stream
            var streams = await youtube.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streams.GetMuxed().WithHighestVideoQuality();
            if (streamInfo == null)
            {
                Console.Error.WriteLine("This videos has no streams");
                return -1;
            }

            // Compose file name, based on metadata
            var fileName = $"{videoId}.{streamInfo.Container.Name}";

            // Download video
            Console.Write($"Downloading stream: {streamInfo.VideoQualityLabel} / {streamInfo.Container.Name}... ");
            using (var progress = new InlineProgress())
                await youtube.Videos.Streams.DownloadAsync(streamInfo, fileName, progress);

            Console.WriteLine($"Video saved to '{fileName}'");
            return 0;
        }

        private static Task<IEnumerable<PlaylistSearchModel>> GetPlaylist(YoutubeClient youtube)
        {
            return youtube.Search.GetPlaylistAsync("Cicero cosmo", 0, 1);
        }
    }
}
