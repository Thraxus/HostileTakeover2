using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Utility.Research
{
    internal class DataLog
    {
        private readonly string _filename;
        private readonly StringBuilder _buffer = new StringBuilder();
        private string[] _headers;
        private readonly List<string[]> _rows = new List<string[]>();

        public DataLog(string name)
        {
            _filename = name + ".md";
        }

        /// <summary>Flush any pending table then write a raw Markdown line (section header, blank line, etc.).</summary>
        public void Write(string rawMarkdown)
        {
            FlushTable();
            _buffer.AppendLine(rawMarkdown);
        }

        /// <summary>Flush any pending table then start a new one with these column headers.</summary>
        public void WriteHeader(params string[] columns)
        {
            FlushTable();
            _headers = columns;
        }

        public void WriteRow(params string[] values)
        {
            _rows.Add(values);
        }

        public void Close()
        {
            FlushTable();
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_filename, typeof(DataLog)))
                    writer.Write(_buffer.ToString());
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"DataLog.Close() [{_filename}] exception: {e}");
            }
            finally
            {
                _buffer.Clear();
            }
        }

        private void FlushTable()
        {
            if (_headers == null) return;

            var widths = new int[_headers.Length];
            for (int i = 0; i < _headers.Length; i++)
                widths[i] = _headers[i].Length;
            foreach (var row in _rows)
                for (int i = 0; i < row.Length && i < widths.Length; i++)
                    widths[i] = Math.Max(widths[i], EscapeMd(row[i]).Length);

            _buffer.AppendLine(FormatRow(_headers, widths));
            var sep = new string[_headers.Length];
            for (int i = 0; i < _headers.Length; i++) sep[i] = new string('-', widths[i]);
            _buffer.AppendLine(FormatRow(sep, widths, escape: false));
            foreach (var row in _rows)
                _buffer.AppendLine(FormatRow(row, widths));

            _headers = null;
            _rows.Clear();
        }

        private string FormatRow(string[] values, int[] widths, bool escape = true)
        {
            var sb = new StringBuilder("|");
            for (int i = 0; i < widths.Length; i++)
            {
                var v = escape ? EscapeMd(i < values.Length ? values[i] : "") : (i < values.Length ? values[i] : "");
                sb.Append(" ").Append(v.PadRight(widths[i])).Append(" |");
            }
            return sb.ToString();
        }

        private static string EscapeMd(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }
    }
}
