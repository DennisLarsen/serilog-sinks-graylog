using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Graylog.Helpers;
using Serilog.Sinks.Graylog.MessageBuilders;
using Serilog.Sinks.Graylog.Transport;
using Serilog.Sinks.Graylog.Transport.Http;
using Serilog.Sinks.Graylog.Transport.Udp;
using Serilog.Sinks.Graylog.Transport.Tcp;
using SerilogTransportType = Serilog.Sinks.Graylog.Transport.TransportType;


namespace Serilog.Sinks.Graylog
{
    public class GraylogSink : ILogEventSink
    {
        private readonly IGelfConverter _converter;
        private readonly ITransport _transport;

        public GraylogSink(GraylogSinkOptions options)
        {
            _transport = MakeTransport(options);

            string hostName = Dns.GetHostName();

            IDictionary<BuilderType, Lazy<IMessageBuilder>> builders = new Dictionary<BuilderType, Lazy<IMessageBuilder>>
            {
                [BuilderType.Exception] = new Lazy<IMessageBuilder>(() => new ExceptionMessageBuilder(hostName, options)),
                [BuilderType.Message] = new Lazy<IMessageBuilder>(() => new GelfMessageBuilder(hostName, options))
            };
              
            _converter = options.GelfConverter ?? new GelfConverter(builders);
        }

        private ITransport MakeTransport(GraylogSinkOptions options)
        {
            switch (options.TransportType)
            {
                case SerilogTransportType.Udp:

                    IDnsInfoProvider dns = new DnsWrapper();
                    IPAddress[] ipAddreses = Task.Run(() => dns.GetHostAddresses(options.HostnameOrAdress)).Result;
                    IPAddress ipAdress = ipAddreses.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);
                    var ipEndpoint = new IPEndPoint(ipAdress, options.Port);

                    Transport.Udp.IDataToChunkConverter chunkConverter = new Transport.Udp.DataToChunkConverter(new Transport.Udp.ChunkSettings
                    {
                        MessageIdGeneratorType = options.MessageGeneratorType
                    }, new MessageIdGeneratorResolver());

                    var udpClient = new UdpTransportClient(ipEndpoint);
                    var udpTransport = new UdpTransport(udpClient, chunkConverter);
                    return udpTransport;
                case SerilogTransportType.Tcp:

                    IDnsInfoProvider tcp_dns = new DnsWrapper();
                    IPAddress[] tcp_ipAddreses = Task.Run(() => tcp_dns.GetHostAddresses(options.HostnameOrAdress)).Result;
                    IPAddress tcp_ipAdress = tcp_ipAddreses.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork);
                    var tcp_ipEndpoint = new IPEndPoint(tcp_ipAdress, options.Port);

                    Transport.Tcp.IDataToChunkConverter tcp_chunkConverter = new Transport.Tcp.DataToChunkConverter(new Transport.Tcp.ChunkSettings
                    {
                        MessageIdGeneratorType = options.MessageGeneratorType
                    }, new MessageIdGeneratorResolver());

                    var tcpClient = new TcpTransportClient(tcp_ipEndpoint);
                    var tcpTransport = new TcpTransport(tcpClient, tcp_chunkConverter);
                    return tcpTransport;
                case SerilogTransportType.Http:
                    var httpClient = new HttpTransportClient($"{options.HostnameOrAdress}:{options.Port}/gelf");
                    var httpTransport = new HttpTransport(httpClient);
                    return httpTransport;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options), options.TransportType, null);
            }
            
        }

        public void Emit(LogEvent logEvent)
        {
            JObject json = _converter.GetGelfJson(logEvent);

            Task.Factory.StartNew(() => _transport.Send(json.ToString(Newtonsoft.Json.Formatting.None))).GetAwaiter().GetResult();
        }
    }
}