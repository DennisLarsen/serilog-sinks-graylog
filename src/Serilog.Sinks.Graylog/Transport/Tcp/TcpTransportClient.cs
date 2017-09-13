using Serilog.Debugging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Serilog.Sinks.Graylog.Transport.Tcp
{
    /// <summary>
    /// Tcp transport client
    /// </summary>
    /// <seealso cref="byte" />
    public sealed class TcpTransportClient : ITransportClient<byte[]>
    {
        private readonly IPEndPoint _target;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransportClient"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public TcpTransportClient(IPEndPoint target)
        {
            _target = target;
        }

        /// <summary>
        /// Sends the specified payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public async Task Send(byte[] payload)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(_target.Address, _target.Port)
                        .ContinueWith(ct =>
                        {
                            tcpClient.Client.Send(payload);
                            if (ct.IsFaulted)
                                throw new LoggingFailedException("Unable send log message to graylog via Tcp transport");
                        });
            }
        }

    }
}

