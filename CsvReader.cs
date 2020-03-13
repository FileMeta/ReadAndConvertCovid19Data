/*
---
name: CsvReader.cs
description: CodeBit classes for reading data from .csv format according to rfc4180
url: https://raw.githubusercontent.com/FileMeta/CsvReader/master/CsvReader.cs
version: 1.0
keywords: CodeBit
dateModified: 2020=03-12
license: https://opensource.org/licenses/BSD-3-Clause
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
...
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause

Copyright 2020 Brandt Redd

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
this list of conditions and the following disclaimer in the documentation
and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
may be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FileMeta
{
    /// <summary>
    /// Read the contents of a .csv file according to rfc4180 specifications.
    /// </summary>
    class CsvReader : IDisposable
    {
        TextReader m_reader;
        bool m_autoCloseReader;

        /// <summary>
        /// Construct a .csv reader on a file in the file system
        /// </summary>
        /// <param name="filename">Name of the file</param>
        /// <param name="encoding">Expected text encoding in the file. Default: UTF8</param>
        /// <param name="detectEncodingFromByteOrderMarks">True if reader should look for
        /// byte-order marks that indicate encoding. Default: true</param>
        /// </remarks>
        public CsvReader(string filename, Encoding encoding = null, bool detectEncodingFromByteOrderMarks = true)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            m_reader = new StreamReader(filename, Encoding.UTF8, detectEncodingFromByteOrderMarks);
            m_autoCloseReader = true;
        }

        /// <summary>
        /// Construct a .csv reader from a <see cref="TextReader"/>
        /// </summary>
        /// <param name="reader">The reader containing the source .csv file.</param>
        /// <param name="autoCloseReader">True if the <see cref="TextReader"/> should be closed when the CsvReader is disposed.</param>
        public CsvReader(TextReader reader, bool autoCloseReader = true)
        {
            m_reader = reader;
            m_autoCloseReader = autoCloseReader;
        }

        /// <summary>
        /// Read one line from a CSV file
        /// </summary>
        /// <returns>An array of strings parsed from the line or <b>null</b> if at end-of-file.</returns>
        /// <remarks>A well-formed .csv file will have the same number of elements in each row (line).
        /// However, that's not guaranteed to be the case. In the event that a .csv file has a variable
        /// number of elements in each row - or if quoting and escaping is not consistent with rfc4180
        /// then lines may have different numbers of elements.
        /// </remarks>
        public string[] Read()
        {
            if (m_reader.Peek() < 0) return null;

            List<string> line = new List<string>();
            StringBuilder builder = new StringBuilder();

            for (; ; )
            {
                int c = m_reader.Read();
                char ch = (c >= 0) ? (char)c : '\n'; // Treat EOF like newline.

                // Reduce CRLF to LF
                if (ch == '\r')
                {
                    if (m_reader.Peek() == '\n') continue;
                    ch = '\n';
                }

                if (ch == '\n')
                {
                    line.Add(builder.ToString());
                    break;
                }
                else if (ch == ',')
                {
                    line.Add(builder.ToString());
                    builder.Clear();
                }
                else if (ch == '"')
                {
                    for (; ; )
                    {
                        c = m_reader.Read();
                        if (c < 0) break;
                        ch = (char)c;

                        if (ch == '"')
                        {
                            if (m_reader.Peek() == (int)'"')
                            {
                                // Double quote means embedded quote
                                m_reader.Read(); // read the second quote
                            }
                            else
                            {
                                break;
                            }
                        }
                        builder.Append(ch);
                    }
                } // if quote
                else
                {
                    builder.Append(ch);
                }
            } // Loop exits in the middle

            return line.ToArray();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (m_reader != null)
            {
                if (m_autoCloseReader)
                {
                    m_reader.Dispose();
                }
                m_reader = null;
#if DEBUG
                if (!disposing)
                {
                    System.Diagnostics.Debug.Fail("Failed to dispose CsvReader.");
                }
#endif
            }
        }

        ~CsvReader()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
