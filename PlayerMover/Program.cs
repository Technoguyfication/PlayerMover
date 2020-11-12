using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommandLine;
using fNbt;
using UUIDUtil;

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

			// validate position
			try
			{
				var _ = opts.NewPos;
			}
			catch (Exception)
			{
				Console.WriteLine("Failed to parse position argument");
				return;
			}

			// validate rotation
			try
			{
				var _ = opts.NewRot;
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
			Console.WriteLine("\nPLEASE MAKE A BACKUP OF YOUR WORLD BEFORE RUNNING THIS SOFTWARE - PRESS Y TO CONTINUE, OR ANY OTHER KEY TO QUIT\n");
			if (!new char[] { 'y', 'Y' }.Contains(Console.ReadKey(true).KeyChar)) return;   // force user to enter y

			// load files
			string[] files = Directory.GetFiles(opts.DataDir, "*.dat");
			if (opts.Verbose) Console.WriteLine($"\nLoaded {files.Length} player files");

			// open stream to the output file for putting uuids
			using StreamWriter sw = new StreamWriter(File.OpenWrite("MovedPlayers.txt"));

			foreach (var fileName in files)
			{
				if (opts.Verbose) Console.WriteLine($"\nLoading file: {fileName}");

				var myFile = new NbtFile(fileName);

				// read uuid bytes
				var worldLeast = myFile.RootTag.Get<NbtLong>("WorldUUIDLeast");
				var worldMost = myFile.RootTag.Get<NbtLong>("WorldUUIDMost");

				// verify uuid bytes
				if (worldLeast == null || worldMost == null)
				{
					throw new Exception($"NBT File {fileName} does not contain a valid world UUID");
				}

				var uuid = UUID.Read(worldMost.Value, worldLeast.Value);

				if (opts.Verbose) Console.WriteLine($"Existing world UUID: {uuid}");

				// are we moving the player?
				if (uuid.Equals(opts.FromWorldUUID))
				{
					//read player's uuid
					var uuidLeast = myFile.RootTag.Get<NbtLong>("UUIDLeast");
					var uuidMost = myFile.RootTag.Get<NbtLong>("UUIDMost");

					// verify uuid bytes
					if (worldLeast == null || worldMost == null)
					{
						throw new Exception($"NBT File {fileName} does not contain a valid player UUID");
					}

					var playerUuid = UUID.Read(uuidMost.Value, uuidLeast.Value);

					Console.WriteLine($"Moving {playerUuid}");

					// change world
					(var newWorldMost, var newWorldLeast) = UUID.GetLongs(opts.ToWorldUUID);
					worldMost.Value = newWorldMost;
					worldLeast.Value = newWorldLeast;

					// set spawn world
					var spawnWorld = myFile.RootTag.Get<NbtString>("SpawnWorld");
					if (spawnWorld != null) spawnWorld.Value = opts.ToWorldName;

					// set position
					var pos = myFile.RootTag.Get<NbtList>("Pos");
					pos[0] = new NbtDouble(opts.NewPos.X);
					pos[1] = new NbtDouble(opts.NewPos.Y);
					pos[2] = new NbtDouble(opts.NewPos.Z);

					// set rotation
					var rot = myFile.RootTag.Get<NbtList>("Rotation");
					rot[0] = new NbtFloat(opts.NewRot.Yaw);
					rot[1] = new NbtFloat(opts.NewRot.Roll);

					// set velocity to 0
					var motion = myFile.RootTag.Get<NbtList>("Motion");
					for (int i = 0; i < motion.Count; i++)
					{
						motion[i] = new NbtDouble(0d);
					}

					// set fall distance to 0
					myFile.RootTag.Get<NbtFloat>("FallDistance").Value = 0f;

					// set on ground to true
					myFile.RootTag.Get<NbtByte>("OnGround").Value = 0x01;

					// write back to file with existing compression
					myFile.SaveToFile(fileName, myFile.FileCompression);

					if (opts.Verbose) Console.WriteLine("Done updating NBT");
					sw.WriteLine(playerUuid.ToString());
				}
				else
				{
					if (opts.Verbose) Console.WriteLine("Move not required");
				}

			}

			Console.WriteLine($"\nLog of moved players' UUIDs stored in {Path.Combine(Environment.CurrentDirectory, "MovedPlayers.txt")}");
		}

		static void HandleParseError(IEnumerable<Error> errs)
		{
			// just exit i guess
		}
	}
}
