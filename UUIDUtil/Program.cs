using System;
using System.IO;

namespace UUIDUtil
{
	class Program
	{
		static void Main(string[] args)
		{
			switch (args.Length)
			{
				case 0:
					{
						var stdin = Console.OpenStandardInput();

						// if data isn't being piped into the program we show help message and quit
						if (!InputRedirected())
						{
							HelpMsg();
							return;
						}

						// read in entire stream
						byte[] buffer = new byte[16];
						stdin.Read(buffer);

						// convert to uuid
						var uuid = UUID.Read(buffer);
						(var most, var least) = UUID.GetLongs(uuid);

						// output
						Console.WriteLine($"Most: {most}\nLeast: {least}");
						Console.WriteLine($"UUID: {uuid}");
						return;
					}
				case 1: // convert uuid to longs
					{
						if (!Guid.TryParse(args[0], out var uuid))
						{
							Console.WriteLine("Invalid UUID");
							return;
						}

						(var most, var least) = UUID.GetLongs(uuid);

						Console.WriteLine($"Most: {most}\nLeast: {least}");
						Console.WriteLine($"UUID: {uuid}");
						return;
					}
				case 2: // convert longs to guid
					{
						if (!long.TryParse(args[0], out var most) || !long.TryParse(args[1], out var least))
						{
							Console.WriteLine("You didn't specify two longs");
							return;
						}

						var uuid = UUID.Read(most, least);

						Console.WriteLine($"Most: {most}\nLeast: {least}");
						Console.WriteLine($"UUID: {uuid}");
						return;
					}
				default:
					{
						HelpMsg();
						return;
					}
			}

			static void HelpMsg()
			{
				Console.WriteLine("You need to run the program with a UUID or 2 longs in (most, least) order to convert OR pipe in a binary file with the UUID data");
			}

			static bool InputRedirected()
			{
				// https://stackoverflow.com/a/48237650/4023370
				try
				{
					if (Console.KeyAvailable) return false;
				}
				catch (InvalidOperationException)
				{
					return true;
				}

				return false;
			}
		}
	}
}
