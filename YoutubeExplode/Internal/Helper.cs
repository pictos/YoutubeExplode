using System;

namespace YoutubeExplode.Internal
{
	internal sealed class Helper
	{
		public static string ExtractValue(string Source, string Start, string End)
		{
			int start, end;

			try
			{
				if (Source.Contains(Start) && Source.Contains(End))
				{
					start = Source.IndexOf(Start, 0) + Start.Length;
					end = Source.IndexOf(End, start);

					return Source.Substring(start, end - start);
				}
				else
					return PrintZero();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return PrintZero();
			}
		}

		public static string PrintZero()
		{
			return " ";
		}
	}
}
