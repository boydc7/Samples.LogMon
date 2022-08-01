using System;
using DdLogMon.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DdLogMon.UnitTests
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void CanParseSimpleLine()
        {
            var parser = new NaiveHttpAccessLogLineParser(NullLogger<NaiveHttpAccessLogLineParser>.Instance);

            var result = parser.Parse(@"64.242.88.10 - - [07/Mar/2004:16:05:49 -0800] ""GET /twiki/bin/edit/Main/Double_bounce_sender?topicparent=Main.ConfigurationVariables HTTP/1.1"" 401 12846");

            result.Should().NotBeNull();
            result.IpAddress.Should().Be("64.242.88.10");
            result.Ident.Should().Be("-");
            result.UserId.Should().Be("-");

            result.ReceivedOn.Should().Be(new DateTime(2004, 3, 8, 0, 5, 49, DateTimeKind.Utc));
            result.Request.Should().Be("GET /twiki/bin/edit/Main/Double_bounce_sender?topicparent=Main.ConfigurationVariables HTTP/1.1");
            result.StatusCode.Should().Be(401);
            result.ResponseSize.Should().Be(12846);
        }
    }
}
