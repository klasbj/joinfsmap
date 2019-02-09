using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JoinFsMap.Models;
using JoinFsMap.Services;
using NUnit.Framework;

namespace Tests {
    public class Tests {
        private WhazzupReader reader;

        [SetUp]
        public void Setup() {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var aircraftTypes = new[] { ("B738", "Boeing 737-800") }.ToDictionary(k => k.Item1, v => v.Item2);

            reader = new WhazzupReader(aircraftTypes);
        }

        [Test]
        public async Task Parse_OneAtcAndOnePilot_ShouldReturnBoth() {
            var data = new StringReader(@"!GENERAL
VERSION = 1
RELOAD = 1
UPDATE = 20190209111659
CONNECTED CLIENTS = 20
CONNECTED SERVERS = 26
!CLIENTS
EGKK_CTR:ATC-ALAM:ATC-ALAM:ATC:122.800:51.1480560302734:-0.190277993679047:0:0::::::::::6:88:::::::::::::::::::::::::::
HP1526CM:BLEW:BLEW:PILOT::9.36238174000879:-81.2680985695181:27816:404:/B738::::::::1200::::VFR:::::::::::::::JoinFS:::::::098:::
!SERVERS
95.141.35.71:95.141.35.71:Internet:ITALIANI VOLANTI:true:999");

            var actual = await reader.ReadAsync(data);

            Assert.Multiple(() => {
                Assert.That(actual.TimeStamp, Is.EqualTo(new DateTime(2019, 2, 9, 11, 16, 59)));
                Assert.That(actual.Entities.Select(x => new { x.Callsign, x.UserName }), Is.EquivalentTo(
                    new[] { new { Callsign = "EGKK_CTR", UserName = "ATC-ALAM" },
                    new { Callsign = "HP1526CM", UserName = "BLEW" } }));
                var atc = actual.Entities.Single(x => x is Atc) as Atc;
                Assert.That(atc.Frequency, Is.EquivalentTo(new[] { 122800 }));

                var pilot = actual.Entities.Single(x => x is Pilot) as Pilot;
                Assert.That(pilot.AircraftTypeShort, Is.EqualTo("B738"));
                Assert.That(pilot.AircraftType, Is.EqualTo("Boeing 737-800"));
                Assert.That(pilot.GroundSpeed, Is.EqualTo(404.0).Within(1.0e-6));
                Assert.That(pilot.Heading, Is.EqualTo(98.0).Within(1.0e-6));
                Assert.That(pilot.Trail, Is.Empty);
            });
        }

        [Test]
        public async Task Parse_OnePilotInCultureWithCommaAsDecimalSeparator_LongitudeAndLatitudeParsedCorrectly() {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sv-SE");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");

            var data = new StringReader(@"!GENERAL
UPDATE = 20190209111659
!CLIENTS
HP1526CM:BLEW:BLEW:PILOT::9,36238:-81,26809:27816:404:/B738::::::::1200::::VFR:::::::::::::::JoinFS:::::::098:::");

            var actual = (await reader.ReadAsync(data)).Entities.Single();

            Assert.Multiple(() => {
                Assert.That(actual.Position.Latitude, Is.EqualTo(9.36238).Within(1e-6));
                Assert.That(actual.Position.Longitude, Is.EqualTo(-81.26809).Within(1e-6));
            });
        }
    }
}