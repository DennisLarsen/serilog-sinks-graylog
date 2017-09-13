using System.Collections.Generic;

namespace Serilog.Sinks.Graylog.Transport
{
    /// <summary>
    /// Converts Data to TCP and UDP chunks
    /// </summary>
    public interface IDataToChunkConverter
    {
        /// <summary>
        /// Converts to chunks.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>array of chunks to save</returns>
        /// <exception cref="System.ArgumentException">message was too long</exception>
        /// <exception cref="ArgumentException">message was too long</exception>
        IList<byte[]> ConvertToChunks(byte[] message);
    }
}