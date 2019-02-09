using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JoinFsMap.Helpers;
using JoinFsMap.Models;

namespace JoinFsMap.Services {
    public interface IWhazzupReader {
        Task<WhazzupData> ReadAsync(TextReader fileData);
    }

    public class WhazzupData {
        public WhazzupData(DateTime timeStamp, IEnumerable<Entity> entities) {
            TimeStamp = timeStamp;
            Entities = entities;
        }

        public DateTime TimeStamp { get; }

        public IEnumerable<Entity> Entities { get; }
    }

    public class WhazzupReader : IWhazzupReader {
        private IDictionary<string, string> aircraftTypeMap;

        public WhazzupReader()
            : this(new Dictionary<string, string>()) {
        }

        public WhazzupReader(IDictionary<string, string> aircraftTypeMap) {
            this.aircraftTypeMap = aircraftTypeMap;
        }

        public async Task<WhazzupData> ReadAsync(TextReader fileData) {
            DateTime? timeStamp = null;
            List<Entity> entities = new List<Entity>();
            Section section = Section.Metadata;

            string line;
            while ((line = await fileData.ReadLineAsync()) != null) {
                section = ParseLine(line, section, ref timeStamp, entities);
            }

            if (!timeStamp.HasValue) {
                throw new InvalidOperationException("Whazzup data provided without a time stamp.");
            }

            return new WhazzupData(timeStamp.Value, entities);
        }

        private Section ParseLine(string line, Section section, ref DateTime? timeStamp, IList<Entity> entities) {
            if (line.StartsWith("!GENERAL")) {
                return Section.Metadata;
            }
            else if (line.StartsWith("!CLIENTS")) {
                return Section.Clients;
            }
            else if (line.StartsWith("!SERVERS")) {
                return Section.Servers;
            }

            switch (section) {
                case Section.Metadata:
                    if (line.StartsWith("UPDATE")) {
                        timeStamp = GetTimeStamp(line);
                    }
                    break;
                case Section.Clients:
                    entities.Add(ParseClient(line));
                    break;
                default:
                    break;
            }

            return section;
        }

        private DateTime? GetTimeStamp(string line) => DateTime.TryParseExact(
                line.Split('=')[1].Trim(),
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime r)
                    ? (DateTime?)r
                    : null;

        private Entity ParseClient(string line) {
            var elements = line.Split(':');
            Entity entity = null;

            switch (elements[3]) {
                case "ATC":
                    entity = ParseAtc(elements);
                    break;
                case "PILOT":
                    entity = ParsePilot(elements);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown entity type '{elements[3]}'");
            }

            entity.Callsign = elements[0];
            entity.UserName = elements[2];
            entity.Position = new Position {
                Latitude = ParseDouble(elements[5]),
                Longitude = ParseDouble(elements[6]),
                Altitude = ParseDouble(elements[7])
            };

            return entity;
        }

        private Entity ParseAtc(string[] elements) => new Atc {
            Frequency = elements[4].Split('&').Select(f => int.Parse(f.Replace(".", string.Empty))).ToList()
        };

        private Entity ParsePilot(string[] elements) => new Pilot {
            AircraftType = LookupAircraftType(elements[9]),
            AircraftTypeShort = elements[9].Split('/').GetOrDefault(1, "ZZZZ"),
            GroundSpeed = ParseDouble(elements[8]),
            Heading = ParseDouble(elements[43])
        };

        private string LookupAircraftType(string designation) {
            string type = designation.Split('/').GetOrDefault(1, null);
            if (type == null) {
                return "Unknown";
            }

            return aircraftTypeMap.TryGetValue(type, out string r) ? r : type;
        }

        double ParseDouble(string s) {
            var culture = CultureInfo.CurrentCulture;

            s = s.Replace("-", culture.NumberFormat.NegativeSign);
            s = s.Replace("—", culture.NumberFormat.NegativeSign);
            s = s.Replace(".", culture.NumberFormat.NumberDecimalSeparator);
            s = s.Replace(",", culture.NumberFormat.NumberDecimalSeparator);

            return double.Parse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
        }


        private enum Section {
            Metadata,
            Clients,
            Servers
        }
    }
}