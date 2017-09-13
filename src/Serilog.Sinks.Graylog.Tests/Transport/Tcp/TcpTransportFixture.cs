using System.Collections.Generic;
using System.Linq;
using Moq;
using Ploeh.AutoFixture;
using Serilog.Sinks.Graylog.Extensions;
using Serilog.Sinks.Graylog.Transport;
using Serilog.Sinks.Graylog.Transport.Tcp;
using Xunit;

namespace Serilog.Sinks.Graylog.Tests.Transport.Tcp
{
    public class TcpTransportFixture
    {
        [Fact]
        public void WhenSend_ThenCallMethods()
        {
            var transportClient = new Mock<ITransportClient<byte[]>>();
            var dataToChunkConverter = new Mock<IDataToChunkConverter>();
            var fixture = new Fixture();

            var stringData = fixture.Create<string>();

            byte[] data = stringData.Compress();

            List<byte[]> chunks = fixture.CreateMany<byte[]>(3).ToList();

            dataToChunkConverter.Setup(c => c.ConvertToChunks(data)).Returns(chunks);

            TcpTransport target = new TcpTransport(transportClient.Object, dataToChunkConverter.Object);

            target.Send(stringData);

            dataToChunkConverter.Verify(c => c.ConvertToChunks(data), Times.Once);

            foreach (byte[] chunk in chunks)
            {
                transportClient.Verify(c => c.Send(chunk), Times.Once);
            }
            
        }
    }
}