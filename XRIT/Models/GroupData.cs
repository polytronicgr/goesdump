﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenSatelliteProject.Tools;

namespace OpenSatelliteProject {
    public class GroupData {
        private const int GROUP_DATA_TIMEOUT = 3600; // 1h 
        private const int DATA_TIMEOUT = 15 * 60; // 15 minutes
        private const int MIN_DATA_MARK = 13 * 60; // 13 minutes

        public string SatelliteName { get; set; }
        public string RegionName { get; set; }
        public float SatelliteLongitude { get; set; }
        public DateTime FrameTime { get; set; }
        public OrganizerData Visible { get; set; }
        public OrganizerData Infrared { get; set; }
        public OrganizerData WaterVapour { get; set; }
        public Dictionary<string, OrganizerData> OtherData { get; set; }

        public bool IsProcessed { get; set; }
        public bool IsFalseColorProcessed { get; set; }
        public bool IsVisibleProcessed { get; set; }
        public bool IsInfraredProcessed { get; set; }
        public bool IsWaterVapourProcessed { get; set; }
        public bool CropImage { get; set; }
        public bool HasNavigationData { get; set; }
        
        public int FallBackLineOffset { get; set; }
        public int FallBackColumnOffset { get; set; }
        public float FallBackColumnScalingFactor { get; set; }
        public float FallBackLineScalingFactor { get; set; }

        public int RetryCount { get; set; }
        public bool Failed { get; set; }
        /// <summary>
        /// Code used for desambiguation
        /// </summary>
        /// <value>The code.</value>
        public string Code { get; set; }

        public bool IsComplete { get { 
                return Visible.IsComplete && 
                    Infrared.IsComplete && 
                    WaterVapour.IsComplete && 
                    IsOtherDataProcessed; 
            } 
        }

        public bool IsOtherDataProcessed { 
            get { 
                return OtherData.Count == 0 || (OtherData
                    .Select(x => x.Value.IsComplete)
                    .Aggregate((x, y) => x & y) && OtherData
                    .Select(x => x.Value.OK)
                    .Aggregate((x, y) => x & y)); 
            } 
        }

        public bool Timeout {
            get {
                return (LLTools.Timestamp() - Created) > DATA_TIMEOUT;
            }
        }

        public bool ReadyToMark {
            get {
                return (LLTools.Timestamp () - Created) > MIN_DATA_MARK;
            }
        }

        public bool GroupTimeout {
            get {
                return (LLTools.Timestamp () - Created) > GROUP_DATA_TIMEOUT;
            }
        }

        public void ForceComplete() {
            IsProcessed = true;
            IsFalseColorProcessed = true;
            IsVisibleProcessed= true;
            IsInfraredProcessed = true;
            IsWaterVapourProcessed = true;
            OtherData.Select (x => x.Value).ToList ().ForEach (k => { k.OK = true; });
        }

        private int Created;

        public GroupData() {
            SatelliteName = "Unknown";
            RegionName = "Unknown";
            SatelliteLongitude = 0f;
            FrameTime = DateTime.Now;
            Visible = new OrganizerData();
            Infrared = new OrganizerData();
            WaterVapour = new OrganizerData();
            OtherData = new Dictionary<string, OrganizerData>();
            IsFalseColorProcessed = false;
            IsVisibleProcessed = false;
            IsInfraredProcessed = false;
            IsWaterVapourProcessed = false;
            IsProcessed = false;
            Failed = false;
            RetryCount = 0;
            CropImage = false;
            Created = LLTools.Timestamp();
            Code = DateTime.UtcNow.ToString();
            HasNavigationData = false;
            FallBackColumnOffset = -1;
            FallBackLineOffset = -1;
            FallBackColumnScalingFactor = 0f;
            FallBackLineScalingFactor = 0f;
        }

        public override string ToString() {
            return string.Format(
                "Satellite Name: {0}\n" +
                "Region Name: {1}\n" +
                "Frame Time: {2}\n" +
                "Visible Segments: {3} ({4})\n" +
                "Infrared Segments: {5} ({6})\n" +
                "Water Vapour Segments: {7} ({8})\n" +
                "Other Data {9} ({10})\n" +
                "\n",
                SatelliteName,
                RegionName,
                FrameTime,
                Visible.Segments.Count,
                Visible.IsComplete ? "Complete" : "Incomplete",
                Infrared.Segments.Count,
                Infrared.IsComplete ? "Complete" : "Incomplete",
                WaterVapour.Segments.Count,
                WaterVapour.IsComplete ? "Complete" : "Incomplete",
                OtherData.Count,
                IsOtherDataProcessed ? "Complete" : "Incomplete"
            );
        }
    }
}

