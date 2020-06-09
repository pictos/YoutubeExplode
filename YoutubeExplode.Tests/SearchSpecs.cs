using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace YoutubeExplode.Tests
{
	public class SearchSpecs
	{
		[Fact]
		public async Task I_can_search_for_YouTube_videos()
		{
			// Arrange
			var youtube = new YoutubeClient();

			// Act
			var videos = await youtube.Search.GetVideosAsync("undead corporation megalomania", 0, 5);

			// Assert
			videos.Should().NotBeEmpty();
		}

		[Fact]
		public async Task I_can_search_for_YouTube_videos_and_get_a_subset_of_results()
		{
			// Arrange
			const int maxVideoCount = 50;
			var youtube = new YoutubeClient();

			// Act
			var videos = await youtube.Search.GetVideosAsync("billie eilish", 0, 5).BufferAsync(maxVideoCount);

			// Assert
			videos.Should().NotBeEmpty();
			videos.Should().HaveCountLessOrEqualTo(maxVideoCount);
		}
	}
}