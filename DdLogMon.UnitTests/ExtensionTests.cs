using System;
using DdLogMon.Helpers;
using DdLogMon.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace DdLogMon.UnitTests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void CanParseLogDateTimeFormat()
        {
            var dateAsString = "07/Mar/2004:16:05:49 -0800";

            var date = dateAsString.ToDateTimeFromC();

            date.Should().Be(new DateTime(2004, 3, 8, 0, 5, 49, DateTimeKind.Utc));
        }

        [Test]
        public void CanGetSegmentFromRequestCorrectly()
        {
            var requestSection = "GET /twiki/bin/edit/Main/Double_bounce_sender?topicparent=Main.ConfigurationVariables HTTP/1.1";

            var section = requestSection.GetStatsSectionFromRequest();

            section.Should().Be("twiki");
        }

        [Test]
        public void CanGetSegmentFromRequestWithoutMultipleSlashesCorrectly()
        {
            var requestSection = "GET /edit HTTP/1.1";

            var section = requestSection.GetStatsSectionFromRequest();

            section.Should().Be("edit");
        }

    }
}
