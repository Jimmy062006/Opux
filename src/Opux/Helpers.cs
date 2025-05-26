using Discord.Commands;
using System;

namespace Opux
{
	class Helpers
	{
		internal static bool IsUserMention(ICommandContext context)
		{
			if (context.Message.MentionedUserIds.Count != 0)
			{
				return true;
			}
			return false;
		}

		internal static string GenerateUnicodePercentage(double percentage)
		{
			string styles = "░▒▓█";

			double d, full, middle, rest, x, min_delta = double.PositiveInfinity;
			char full_symbol = styles[^1], m;
			var n = styles.Length;
			var max_size = 20;
			var min_size = 19;

			var i = max_size;

			string String = "";
			if (percentage == 100)
			{
				return Repeat(full_symbol, 10);
			}
			else
			{
				percentage /= 100;

				while (i > 0 && i >= min_size)
				{

					x = percentage * i;
					full = Math.Floor(x);
					rest = x - full;
					middle = Math.Floor(rest * n);

					if (percentage != 0 && full == 0 && middle == 0) middle = 1;

					d = Math.Abs(percentage - (full + middle / n) / i) * 100;

					if (d < min_delta)
					{
						min_delta = d;

						m = styles[(int)middle];
						if (full == i) m = ' ';
						String = Repeat(full_symbol, full) + m + Repeat(styles[0], i - full - 1);
					}
					i--;
				}
			}

			return String;
		}

		static string Repeat(char s, double i)
		{
			var r = "";
			for (var j = 0; j < i; j++) r += s;
			return r;
		}
	}
}
