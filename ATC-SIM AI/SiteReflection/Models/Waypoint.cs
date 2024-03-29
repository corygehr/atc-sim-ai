﻿using System;

namespace AtcSimController.SiteReflection.Models
{
    /// <summary>
    /// Waypoint object on the Radar Screen
    /// </summary>
    public sealed class Waypoint : ScopeObject, IEquatable<Waypoint>
    {
        private int _heading;
        private string _name;
        private WaypointType _type;

        /// <summary>
        /// Default constructor for a Waypoint object
        /// </summary>
        /// <param name="name">Waypoint name</param>
        /// <param name="type">Waypoint type</param>
        /// <param name="x">Waypoint x location</param>
        /// <param name="y">Waypoint y location</param>
        /// <param name="heading">Heading (runway)</param>
        public Waypoint(string name, WaypointType type, double x, double y, int heading = -1) : base(x, y)
        {
            this._heading = heading;
            this._name = name;
            this._type = type;
        }

        /// <summary>
        /// Constructor for a Waypoint object that allows Location object passing
        /// </summary>
        /// <param name="name">Waypoint name</param>
        /// <param name="type">Waypoint type</param>
        /// <param name="location">Waypoint Location object</param>
        /// <param name="heading">Heading (runway)</param>
        public Waypoint(string name, WaypointType type, Location location, int heading = -1) : base(location)
        {
            this._heading = heading;
            this._name = name;
            this._type = type;
        }

        /// <summary>
        /// Returns the heading of this waypoint (runways only, else heading is -1)
        /// </summary>
        public int Heading
        {
            get
            {
                return this._heading;
            }
        }

        /// <summary>
        /// Returns the name of the Navpoint
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <summary>
        /// Returns the numeric type identifier of this waypoint (see WaypointType)
        /// </summary>
        public WaypointType Type
        {
            get
            {
                return this._type;
            }
        }

        public override string ToString()
        {
            return this._name;
        }

        public bool Equals(Waypoint other)
        {
            return other.Name == this.Name && other.Type == this.Type;
        }
    }
}