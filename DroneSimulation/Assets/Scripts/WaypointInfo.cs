using UnityEngine;

namespace Assets.Scripts
{
    public class WaypointInfo
    {
        public WaypointType WaypointType { get; set; } = WaypointType.COORD;
        public Vector3 Coords { get; set; }

        public WaypointInfo(WaypointType waypointType)
        {
            this.WaypointType = waypointType;
            this.Coords = Vector3.zero;
        }

        public WaypointInfo(Vector3 coords)
        {
            this.Coords = coords;
        }
    }

    public enum WaypointType
    {
        COORD, LAND, TAKEOFF, END, DELIVER
    }
}