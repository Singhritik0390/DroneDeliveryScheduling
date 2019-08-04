using UnityEngine;
using System.Collections;
using Assets.Scripts;

public class Drone : MonoBehaviour
{

    public Waypoint WaypointPrefab;

    public float WaitingTime { get; set; }
    public float StartOfWaiting { get; set; }

    public WaypointInfo WaypointInfo { get; set; }
    public Delivery Delivery { get; set; }
    public int FlightAltitude { get; set; }

    private int speed;
    private Controller controller;

    public void Init(Controller controller, int speed, int flightAltitude)
    {
        this.controller = controller;
        this.speed = speed;
        this.FlightAltitude = flightAltitude;
        this.WaitingTime = 0.0f;
        this.StartOfWaiting = 0.0f;
    }

    public void FlyToWaypoint()
    {
        if (WaypointInfo.WaypointType == WaypointType.END)
        {
            // Tell controller this drone has reached final destination and needs new waypoints.
            controller.TellWaiting(this);

        } else if (WaypointInfo.WaypointType == WaypointType.COORD)
        {
            Waypoint waypoint = Instantiate(WaypointPrefab, WaypointInfo.Coords, Quaternion.identity) as Waypoint;
            waypoint.ParentDrone = this;

            GetComponentInParent<Rigidbody>().velocity = Vector3.Normalize(WaypointInfo.Coords - transform.position) * speed;
            
        } else if (WaypointInfo.WaypointType == WaypointType.DELIVER)
        {
            Delivery.SetState(DeliveryState.COMPLETE);
            
            // Request new waypoint.
            WaypointInfo = controller.GetNextWaypointInfo(this);
            FlyToWaypoint();

        } else if (WaypointInfo.WaypointType == WaypointType.LAND)
        {
            GetComponentInParent<Rigidbody>().velocity = Vector3.down * speed;

        } else if (WaypointInfo.WaypointType == WaypointType.TAKEOFF)
        {
            Vector3 dest = transform.position;
            dest.y = FlightAltitude;
            Waypoint waypoint = Instantiate(WaypointPrefab, dest, Quaternion.identity) as Waypoint;
            waypoint.ParentDrone = this;

            GetComponentInParent<Rigidbody>().velocity = Vector3.up * speed;

        }
        

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Waypoint")
        {
            if (other.gameObject.GetComponent<Waypoint>().ParentDrone == this)
            {
                GetComponentInParent<Rigidbody>().velocity = Vector3.zero;
                Destroy(other.gameObject);

                // Request new waypoint.
                WaypointInfo = controller.GetNextWaypointInfo(this);

                FlyToWaypoint();
            }
        } else if (other.gameObject.tag == "Building")
        {
            // Only time a building will be hit is after a LAND waypoint.
            GetComponentInParent<Rigidbody>().velocity = Vector3.zero;
            WaypointInfo = controller.GetNextWaypointInfo(this);
            FlyToWaypoint();
        }
    }

    public Vector2 GetControllerPos()
    {
        return controller.GetControllerLocation();
    }

    public void Stop()
    {
        GetComponentInParent<Rigidbody>().velocity = Vector3.zero;
    }
}