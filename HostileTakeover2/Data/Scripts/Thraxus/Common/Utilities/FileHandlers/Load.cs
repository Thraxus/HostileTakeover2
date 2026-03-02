using System.IO;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Common.Utilities.FileHandlers
{
	/// <summary>
	/// Static helpers for reading mod data from world storage.  Three formats are
	/// supported to match the three write variants in <see cref="Save"/>: protobuf
	/// binary, XML, and raw text.
	///
	/// Each method first checks that the file exists before attempting to open it.
	/// A null-reader guard inside each <c>using</c> block defends against the
	/// unlikely case where the API returns a null reader even though the file exists.
	///
	/// Each method sets <paramref name="message"/> to a non-empty string when
	/// something noteworthy occurs (file not found, or null reader) and to
	/// <see cref="string.Empty"/> on a clean read.  The caller can pass this string
	/// to <c>WriteGeneral</c> to surface it through the mod's log chain.
	/// </summary>
	internal static class Load
	{
		/// <summary>
		/// Reads a protobuf-binary file from world storage and deserialises it to
		/// <typeparamref name="T"/>.  The binary format is length-prefixed: the first
		/// four bytes are an <c>int32</c> byte count, followed by that many bytes of
		/// protobuf data.
		/// Returns <c>default(T)</c> if the file does not exist or cannot be read.
		/// Sets <paramref name="message"/> to a non-empty description if anything
		/// other than a clean read occurred.
		/// </summary>
		public static T ReadBinaryFileInWorldStorage<T>(string fileName, out string message)
		{
			if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
			{
				message = $"'{fileName}' does not exist; returning default.";
				return default(T);
			}

			using (BinaryReader binaryReader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(fileName, typeof(T)))
			{
				if (binaryReader == null)
				{
					message = $"'{fileName}' could not be read — BinaryReader was null.";
					return default(T);
				}
				// Read the length prefix then the protobuf payload.
				message = string.Empty;
				return MyAPIGateway.Utilities.SerializeFromBinary<T>(binaryReader.ReadBytes(binaryReader.ReadInt32()));
			}
		}

		/// <summary>
		/// Reads an XML file from world storage and deserialises it to
		/// <typeparamref name="T"/>.
		/// Returns <c>default(T)</c> if the file does not exist or cannot be read.
		/// Sets <paramref name="message"/> to a non-empty description if anything
		/// other than a clean read occurred.
		/// </summary>
		public static T ReadXmlFileInWorldStorage<T>(string fileName, out string message)
		{
			if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
			{
				message = $"'{fileName}' does not exist; returning default.";
				return default(T);
			}

			using (TextReader textReader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(T)))
			{
				if (textReader == null)
				{
					message = $"'{fileName}' could not be read — TextReader was null.";
					return default(T);
				}
				message = string.Empty;
				return MyAPIGateway.Utilities.SerializeFromXML<T>(textReader.ReadToEnd());
			}
		}

		/// <summary>
		/// Reads the raw text content of a file from world storage.
		/// Returns <see cref="string.Empty"/> if the file does not exist or cannot be read.
		/// Sets <paramref name="message"/> to a non-empty description if anything
		/// other than a clean read occurred.
		/// </summary>
		public static string ReadFileInWorldStorage<T>(string fileName, out string message)
		{
			if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
			{
				message = $"'{fileName}' does not exist; returning empty.";
				return string.Empty;
			}

			using (TextReader textReader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(T)))
			{
				if (textReader == null)
				{
					message = $"'{fileName}' could not be read — TextReader was null.";
					return string.Empty;
				}
				message = string.Empty;
				return textReader.ReadToEnd();
			}
		}
	}
}
