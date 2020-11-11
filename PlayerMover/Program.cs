using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using CommandLine;
using fNbt;

namespace PlayerMover
{
	class Program
	{
		public class Options
		{
			[Option('d', "datadir", Required = true, HelpText = "Location of the playerdata directory")]
			public string DataDir { get; set; }

			[Option('v', "verbose", Default = false, HelpText = "Prints more.")]
			public bool Verbose { get; set; }

			[Option('f', "from", Required = true, HelpText = "UUID of the world to move players out of")]
			public Guid FromWorldUUID { get; set; }

			[Option('t', "to", Required = true, HelpText = "UUID of the world to move players to")]
			public Guid ToWorldUUID { get; set; }

			[Option('n', "name", Required = true, HelpText = "Name of the world to move players to")]
			public string ToWorldName { get; set; }

			[Option('p', "pos", Required = true, HelpText = "Position to put players in new world")]
			public string NewPosString { get; set; }

			public Position NewPos { get { var split = NewPosString.Split(','); return new Position() { X = double.Parse(split[0]), Y = double.Parse(split[1]), Z = double.Parse(split[2]) }; } }

			[Option('r', "rot", Required = true, HelpText = "Rotation to put players in new world")]
			public string NewRotString { get; set; }

			public Rotation NewRot { get { var split = NewRotString.Split(','); return new Rotation() { Yaw = float.Parse(split[0]), Roll = float.Parse(split[1]) }; } }
		}

		public struct Position
		{
			public double X { get; set; }
			public double Y { get; set; }
			public double Z { get; set; }

			public override string ToString()
			{
				return $"{X}, {Y}, {Z}";
			}
		}

		public struct Rotation
		{
			public float Yaw { get; set; }
			public float Roll { get; set; }

			public override string ToString()
			{
				return $"{Yaw}, {Roll}";
			}
		}


		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(RunOptions)
				.WithNotParsed(HandleParseError);
		}

		static void RunOptions(Options opts)
		{
			Console.WriteLine($"PlayerMover {Assembly.GetExecutingAssembly().GetName().Version}\n");

			// parse position
			Position pos;
			try
			{
				pos = opts.NewPos;
			}
			catch (Exception)
			{
				Console.WriteLine("Failed to parse position argument");
				return;
			}

			// parse rotation
			Rotation rot;
			try
			{
				rot = opts.NewRot;
			}
			catch (Exception)
			{
				Console.WriteLine("Failed to parse rotation argument");
				return;
			}

			// log settings
			Console.WriteLine($"Playerdata dir: {opts.DataDir}");
			Console.WriteLine($"Moving players in world: {opts.FromWorldUUID}");
			Console.WriteLine($"Moving players to world: {opts.ToWorldName}/{opts.ToWorldUUID} at ({opts.NewPos}) / ({opts.NewRot})");

			// confirm
			Console.WriteLine("\nPLEASE MAKE A BACKUP OF YOUR WORLD BEFORE RUNNING THIS SOFTWARE - PRESS Y TO CONTINUE, OR ANY OTHER KEY TO QUIT");
			if (!new char[] { 'y', 'Y' }.Contains(Console.ReadKey(true).KeyChar)) return;   // force user to enter y

			// load files
			string[] files = Directory.GetFiles(opts.DataDir, "*.dat");
			if (opts.Verbose) Console.WriteLine($"\nLoaded {files.Length} player files");

			foreach (var file in files)
			{
				HandleFile(file, opts);
			}
		}

		static void HandleParseError(IEnumerable<Error> errs)
		{
			// just exit i guess
		}

		static void HandleFile(string fileName, Options opts)
		{
			if (opts.Verbose) Console.WriteLine($"Loading file: {fileName}");
			
			var myFile = new NbtFile(fileName);

			var nbtLeast = myFile.RootTag.Get<NbtLong>("WorldUUIDLeast");
			var nbtMost = myFile.RootTag.Get<NbtLong>("WorldUUIDMost");

			var uuidBytes = new byte[sizeof(long) * 2];
			var mostBuf = BitConverter.GetBytes(nbtMost.Value);
			var leastBuf = BitConverter.GetBytes(nbtLeast.Value);
			Buffer.BlockCopy(mostBuf, 0, uuidBytes, 0, sizeof(long));
			Buffer.BlockCopy(leastBuf, 0, uuidBytes, sizeof(long), sizeof(long));

			var uuid = new Guid(uuidBytes);

			// verify world uuid
			if (nbtLeast == null || nbtMost == null)
			{
				throw new Exception($"NBT File {fileName} does not contain a valid world UUID");
			}

			if (opts.Verbose) Console.WriteLine($"Existing world UUID: {uuid}");
		}
	}
}
