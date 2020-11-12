using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UUIDUtil
{
	public static class UUID
	{
		public static Guid Read(long most, long least)
		{
			// load the bytes into a new uuid
			var uuidBytes = new byte[sizeof(long) * 2];
			var mostBuf = BitConverter.GetBytes(most).ReverseIfLittleEndian();
			var leastBuf = BitConverter.GetBytes(least).ReverseIfLittleEndian();
			Buffer.BlockCopy(mostBuf, 0, uuidBytes, 0, sizeof(long));
			Buffer.BlockCopy(leastBuf, 0, uuidBytes, sizeof(long), sizeof(long));

			return Read(uuidBytes);
		}

		public static Guid Read(byte[] bytes)
		{
			// reverse first 3 parts because microsoft is fucking dumb
			Array.Reverse(bytes, 0, 4);
			Array.Reverse(bytes, 4, 2);
			Array.Reverse(bytes, 6, 2);

			return new Guid(bytes);
		}

		public static (long, long) GetLongs(Guid uuid)
		{
			var a = new byte[sizeof(long)];
			var b = new byte[sizeof(long)];
			var bytes = uuid.ToByteArray();

			// reverse first 3 parts of guid because microsoft is fucking dumb
			Array.Reverse(bytes, 0, 4);
			Array.Reverse(bytes, 4, 2);
			Array.Reverse(bytes, 6, 2);

			Buffer.BlockCopy(bytes, 0, a, 0, sizeof(long));
			Buffer.BlockCopy(bytes, sizeof(long), b, 0, sizeof(long));

			long c, d;

			c = BitConverter.ToInt64(a.ReverseIfLittleEndian());
			d = BitConverter.ToInt64(b.ReverseIfLittleEndian());

			return (c, d);
		}
	}

	public static class Extensions
	{
		public static byte[] ReverseIfLittleEndian(this byte[] source)
		{
			if (BitConverter.IsLittleEndian)
				return source.Reverse().ToArray();
			else
				return source;
		}
	}
}
