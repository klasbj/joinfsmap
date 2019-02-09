using System;
using System.Collections.Generic;

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
    }

    public class Atc : Entity {
        public IList<int> Frequency { get; set; }
    }

    public class Pilot : Entity {

        public double GroundSpeed { get; set; }

        public double Heading { get; set; }

        public string AircraftType { get; set; }

        public string AircraftTypeShort { get; set; }

        public IList<(DateTime TimeStamp, Position Position)> Trail { get; set; } = new List<(DateTime TimeStamp, Position Position)>();
    }
}