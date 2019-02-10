using System;
using System.Collections.Generic;
using System.Linq;

namespace JoinFsMap.Models {

    public class Position {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Altitude { get; set; }
    }

    public class Entity {
        public string Callsign { get; set; }

        public string UserName { get; set; }

        public Position Position { get; set; }

        public virtual string Key => $"{Callsign}_{UserName}";
    }

    public class Atc : Entity {
        public IList<int> Frequency { get; set; }
    }

    public class Pilot : Entity {

        public double GroundSpeed { get; set; }

        public double Heading { get; set; }

        public string AircraftType { get; set; }

        public string AircraftTypeShort { get; set; }

        public IEnumerable<LoggedPosition> Trail { get; set; } = Enumerable.Empty<LoggedPosition>();

        public override string Key => $"{Callsign}_{UserName}_{AircraftTypeShort}";
    }

    public class LoggedPosition {
        public DateTime TimeStamp { get; set; }

        public Position Position { get; set; }
    }
}