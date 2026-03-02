using System.IO;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Common.Utilities.FileHandlers
{
	/// <summary>
	/// Static helpers for writing mod data to world storage.  Three formats are
	/// supported: protobuf binary, XML, and raw text.
	///
	/// All three methods follow a delete-before-write pattern because the Space
	/// Engineers API does not expose an overwrite method — the file must be removed
	/// first to avoid appending to stale content.  A null-writer guard protects
	/// against the unlikely case where the API cannot open the file for writing.
	///
	/// Each method returns a non-empty string on failure (suitable for passing to
	/// <c>WriteGeneral</c> in the caller) and an empty string on success.
	/// </summary>
	public static class Save
	{
		/// <summary>
		/// Serialises <paramref name="data"/> to protobuf binary and writes it to
		/// world storage under <paramref name="fileName"/>.  Any existing file with
		/// the same name is deleted first.
		/// Returns an empty string on success, or a failure message on error.
		/// </summary>
		public static string WriteBinaryFileToWorldStorage<T>(string fileName, T data)
		{
			// Delete the existing file; the API has no overwrite mode.
			if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
				MyAPIGateway.Utilities.DeleteFileInWorldStorage(fileName, typeof(T));

			using (BinaryWriter binaryWriter = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(fileName, typeof(T)))
			{
				if (binaryWriter == null)
					return $"'{fileName}' could not be saved — BinaryWriter was null.";
				byte[] binary = MyAPIGateway.Utilities.SerializeToBinary(data);
				binaryWriter.Write(binary);
			}
			return string.Empty;
		}

		/// <summary>
		/// Serialises <paramref name="data"/> to XML and writes it to world storage
		/// under <paramref name="fileName"/>.  Any existing file is deleted first.
		/// Returns an empty string on success, or a failure message on error.
		/// </summary>
		public static string WriteXmlFileToWorldStorage<T>(string fileName, T data)
		{
			// Delete the existing file; the API has no overwrite mode.
			if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
				MyAPIGateway.Utilities.DeleteFileInWorldStorage(fileName, typeof(T));

			using (TextWriter textWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(T)))
			{
				if (textWriter == null)
					return $"'{fileName}' could not be saved — TextWriter was null.";
				string text = MyAPIGateway.Utilities.SerializeToXML(data);
				textWriter.Write(text);
			}
			return string.Empty;
		}

		/// <summary>
		/// Writes <paramref name="data"/> as a raw string to world storage under
		/// <paramref name="fileName"/>.  Any existing file is deleted first.
		/// Returns an empty string on success, or a failure message on error.
		/// </summary>
		public static string WriteFileToWorldStorage<T>(string fileName, T data)
		{
			// Delete the existing file; the API has no overwrite mode.
			if (MyAPIGateway.Utilities.FileExistsInWorldStorage(fileName, typeof(T)))
				MyAPIGateway.Utilities.DeleteFileInWorldStorage(fileName, typeof(T));

			using (TextWriter textWriter = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(T)))
			{
				if (textWriter == null)
					return $"'{fileName}' could not be saved — TextWriter was null.";
				textWriter.Write(data);
			}
			return string.Empty;
		}

		/// <summary>Stub — not yet implemented.</summary>
		public static void WriteToSandbox(System.Type T)
		{

		}
	}
}
