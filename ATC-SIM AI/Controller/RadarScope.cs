﻿using AtcSimController.SiteReflection;
using AtcSimController.SiteReflection.Models;
using AtcSimController.SiteReflection.Resources;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;

namespace AtcSimController.Controller
{
    /// <summary>
    /// Scope Details
    /// </summary>
    public sealed class RadarScope
    {
        #region Internal accessors
        private int[] _activeRunwayIndices = new int[0];
        private List<Waypoint> _activeRunways = new List<Waypoint>();
        private List<AircraftSpecification> _aircraft = new List<AircraftSpecification>();
        private Airport _airport;
        private BrowserCapture _dataBroker;
        private Statistics _stats = new Statistics();
        private List<Waypoint> _waypoints = new List<Waypoint>();
        private Dictionary<string, Flight> _flightDict = new Dictionary<string, Flight>();
        private Dictionary<string, Waypoint> _waypointDict = new Dictionary<string, Waypoint>();
        private int _windHdg = 0;
        #endregion

        #region Public accessors
        /// <summary>
        /// Currently active runways
        /// </summary>
        public List<Waypoint> ActiveRunways
        {
            get
            {
                return this._activeRunways;
            }
        }
        /// <summary>
        /// Airport data
        /// </summary>
        public Airport Airport
        {
            get
            {
                return this._airport;
            }
        }
        /// <summary>
        /// All flights on the scope
        /// </summary>
        public Dictionary<string, Flight> Flights
        {
            get
            {
                return this._flightDict;
            }
        }
        /// <summary>
        /// Determines if the simulation is still active
        /// </summary>
        public bool IsActive
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Current score
        /// </summary>
        public Statistics Score
        {
            get
            {
                return this._stats;
            }
        }
        /// <summary>
        /// All waypoints on the radar scope
        /// </summary>
        public Dictionary<string, Waypoint> Waypoints
        {
            get
            {
                return this._waypointDict;
            }
        }
        /// <summary>
        /// Current Wind Direction
        /// </summary>
        public int WindHeading
        {
            get
            {
                return this._windHdg;
            }
        }
        #endregion

        /// <summary>
        /// Initializes Radar Scope data
        /// </summary>
        public RadarScope(IWebDriver driver)
        {
            // Open data broker connection
            this._dataBroker = new BrowserCapture(driver);
            // Get initial data sources
            this._fetchWaypoints();
            this._fetchAircraftModels();
            this._fetchAirportData();
        }

        /// <summary>
        /// Refreshes radar scope
        /// </summary>
        public void Refresh()
        {
            // Get environment data
            this._refreshWind();
            // Get flight data
            this._refreshFlights();
            // Refresh controller statistics
            this._refreshStats();
        }

        /// <summary>
        /// Fetches data about the airfield used for this simulation
        /// </summary>
        private void _fetchAirportData()
        {
            // Get airfield data
            int elevation = Convert.ToInt32(this._dataBroker.FetchRawJSVariable(JSVariables.AIRFIELD_ALTITUDE));
            // Get runways
            List<Waypoint> runways = new List<Waypoint>();
            runways = this._waypoints.FindAll(r => r.Type == WaypointType.RUNWAY);

            // Create Airport object
            this._airport = new Airport(elevation, runways);
        }

        /// <summary>
        /// Fetches all aircraft models used in the simulation
        /// </summary>
        private void _fetchAircraftModels()
        {
            // Get list of models
            JSArray rawModels = new JSArray(this._dataBroker.FetchRawJSVariable(JSVariables.AIRCRAFT));

            // Add models to local aircraft array
            for (int i = 0; i < rawModels.RawData.Count; i++)
            {
                // Parse with JSArray yet again...
                JSArray model = new JSArray(rawModels.RawData[i]);

                // Add to models array
                this._aircraft.Add(new AircraftSpecification(
                    Convert.ToInt32(model.RawData[0]),
                    Convert.ToInt32(model.RawData[1]),
                    Convert.ToInt32(model.RawData[2])
                    ));
            }
        }

        /// <summary>
        /// Fetch flight data from the simulator
        /// </summary>
        private void _refreshFlights()
        {
            // Get list of flights
            Dictionary<string, object> rawFlts = (Dictionary<string, object>)this._dataBroker.FetchRawJSVariable(JSVariables.FLIGHTS);

            // Cast into objects
            foreach (var flt in rawFlts)
            {
                // Get key (flight number)
                string flightNum = flt.Key;

                // Parse flight data to ReadOnlyCollection
                IDictionary<string, object> data = (IDictionary<string, object>)flt.Value;

                // Convert values to proper types and add new flight to the array
                string aircraft = Convert.ToString(data["0"]);
                int modelInd = Convert.ToInt32(data["1"]);
                int x = Convert.ToInt32(data["2"]);
                int y = Convert.ToInt32(data["3"]);
                int z = Convert.ToInt32(data["4"]);
                int hdg = Convert.ToInt32(data["5"]);
                int spd = Convert.ToInt32(data["6"]);
                int fltMode = Convert.ToInt32(data["7"]); // 1 when heading, 2 when nav
                int hdgClr = Convert.ToInt32(data["8"]);
                int altClr = Convert.ToInt32(data["9"]);
                int spdClr = Convert.ToInt32(data["10"]);
                string navClr = Convert.ToString(data["11"]);
                int navClrId = Convert.ToInt32(data["12"]);
                int dest = Convert.ToInt32(data["13"]);
                int lr = Convert.ToInt32(data["14"]);
                int timerSec = Convert.ToInt32(data["15"]);
                char timerMode = Convert.ToChar(data["16"]);
                bool expedite = Convert.ToBoolean(data["17"]);
                bool conflict = Convert.ToBoolean(data["18"]);
                string airline = Convert.ToString(data["19"]);

                Waypoint clearedDest = null;

                if(navClrId >= 0)
                {
                    clearedDest = this._waypoints[navClrId];
                }

                Flight newFlt = new Flight(
                    flightNum,
                    airline,
                    aircraft,
                    this._aircraft[modelInd],
                    (Status)timerMode,
                    this._waypoints[dest],
                    clearedDest,
                    z,
                    altClr,
                    spd,
                    spdClr,
                    hdg,
                    hdgClr,
                    x,
                    y
                );

                this._flightDict[flightNum] = newFlt;
            }
        }

        /// <summary>
        /// Loads waypoints visible on the radar scope
        /// </summary>
        private void _fetchWaypoints()
        {
            // Get list of waypoints
            JSArray rawWpts = new JSArray(this._dataBroker.FetchRawJSVariable(JSVariables.WAYPOINTS));

            // Parse into a list
            for (int i = 0; i < rawWpts.RawData.Count; i++)
            {
                // Parse with JSArray yet again...
                JSArray wpt = new JSArray(rawWpts.RawData[i]);

                // Grab values
                int heading = -1;

                if (wpt.RawData.Count == 5)
                {
                    // Object is a runway and has an extra index
                    heading = Convert.ToInt32(wpt.RawData[4]);
                }

                // Create Waypoint object
                Waypoint result = new Waypoint(
                    Convert.ToString(wpt.RawData[0]),
                    (WaypointType) Convert.ToInt32(wpt.RawData[1]),
                    Convert.ToInt32(wpt.RawData[2]),
                    Convert.ToInt32(wpt.RawData[3]),
                    heading
                );

                this._waypoints.Add(result);

                // Create dictionary entry for faster lookup later
                this._waypointDict[result.Name] = result;
            }
        }

        /// <summary>
        /// Updates score object
        /// </summary>
        private void _refreshStats()
        {
            JSArray rawStats = new JSArray(this._dataBroker.FetchRawJSVariable(JSVariables.SCORE));
            this._stats.UpdateStatistics(rawStats.ParseToInt());
        }

        /// <summary>
        /// Refreshes wind and active runway data
        /// </summary>
        private void _refreshWind()
        {
            // Provide wind and active runways
            this._windHdg = Convert.ToInt32(this._dataBroker.FetchRawJSVariable(JSVariables.WIND_HEADING));
            this._activeRunwayIndices = new JSArray(this._dataBroker.FetchRawJSVariable(JSVariables.ACTIVE_RUNWAYS)).ParseToInt();
        }

        public override string ToString()
        {
            string ret = "WindHdg: " + this._windHdg;
            ret += "; Active Runways:";
            foreach (Waypoint rw in this._activeRunways)
            {
                ret += " " + rw.Name;
            }

            return ret;
        }
    }
}