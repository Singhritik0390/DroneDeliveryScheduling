using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class DroneInfo
    {
        private int nextWaypointIndex = 0;
        private List<WaypointInfo> waypointInfos;

        public DroneInfo(List<WaypointInfo> waypointInfos)
        {
            this.waypointInfos = waypointInfos;
        }

        // Returns waypointInfo with type END if at final destination.
        public WaypointInfo getNextWaypointInfo()
        {
            if (nextWaypointIndex >= waypointInfos.Count)
            {
                // At final destination.
                return new WaypointInfo(WaypointType.END);
            }

            int i = nextWaypointIndex;
            nextWaypointIndex++;
            return waypointInfos[i];
        }

    }
}
