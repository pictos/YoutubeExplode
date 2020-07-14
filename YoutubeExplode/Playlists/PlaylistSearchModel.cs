using System;
using System.Diagnostics;

namespace YoutubeExplode.Playlists
{
    [DebuggerDisplay("{Title}, {VideoCount}")]
    public readonly struct PlaylistSearchModel : IEquatable<PlaylistSearchModel>
    {
        public string Id { get; }
        public string Title { get; }
        public string Author { get; }
        public string VideoCount { get; }
        public string Thumbnail { get; }
        public string Url { get; }

        public PlaylistSearchModel(string id, string title, string author, string videoCount, string thumbnail, string url)
        {
            Id = id;
            Title = title;
            Author = author;
            VideoCount = videoCount;
            Thumbnail = thumbnail;
            Url = url;
        }

        public bool Equals(PlaylistSearchModel other) =>
            (Id, Title, Author, VideoCount, Thumbnail, Url) == (other.Id, other.Title, other.Author, other.VideoCount, other.Thumbnail, other.Url);

        public override bool Equals(object obj) =>
            (obj is PlaylistSearchModel playlist) && Equals(playlist);

        public static bool operator ==(PlaylistSearchModel left, PlaylistSearchModel right) =>
            left.Equals(right);

        public static bool operator !=(PlaylistSearchModel left, PlaylistSearchModel right) =>
            left.Equals(right);

        public override int GetHashCode() =>
            (Id, Title, Author, VideoCount, Thumbnail, Url).GetHashCode();
    }
}
