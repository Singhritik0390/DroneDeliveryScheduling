using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{

    public int simulationSeconds = 60;

    public uint NumberOfDrones = 5;
    public int DroneSpeed = 10;
    public int HeightClearance = 25;
    //public int DroneBatteryLevel = 10000;
    public Drone dronePrefab;

    public GameObject NFZPrefab;
    public uint NFZCount = 3;
    public uint MaxNFZSize = 6;
    public uint MinNFZSize = 4;

    public GameObject buildingPrefab;
    public GameObject RoadHorizontal;
    public GameObject RoadVertical;
    public GameObject RoadIntersection;
    public int MinBuildingHeight = 5;
    public int HeightVariation = 20;
    public int GridSize = 11;
    public int GridCellSize = 3;

    public Logger Logger;

    public DeliveryGenerator DeliveryGenerator;

    public DeliveryScheduler DeliveryScheduler;

    public Text SimulationCompleteText;

    public int Seed = 0;

    public List<Drone> Drones = new List<Drone>();
    private Dictionary<Drone, DroneInfo> droneInfoDict= new Dictionary<Drone, DroneInfo>();
    public List<Drone> WaitingDrones = new List<Drone>();

    private float cityBoundX;
    private float cityBoundZ;
    private float buildingSizeX;
    private float buildingSizeZ;
    private int[,] buildingHeights;

    private bool[,] isNFZ;
    private bool[,] isRoad;

    private Vector2 controllerLocation;
    private int controllerHeight;

    private bool simulationOver = false;
    private bool simulationAlreadyEnded = false;

    private void Start()
    {

        Debug.Log("METRICS: NEW");

        if (Seed != 0)
        {
            UnityEngine.Random.InitState(Seed);
        }

        buildingSizeX = buildingPrefab.transform.localScale.x;
        buildingSizeZ = buildingPrefab.transform.localScale.z;

        cityBoundX = buildingSizeX * (GridSize * (GridCellSize + 1) - 2);
        cityBoundZ = buildingSizeZ * (GridSize * (GridCellSize + 1) - 2);

        controllerLocation = new Vector2(cityBoundX / 2, cityBoundZ / 2);

        // Initialize the city.
        generateCity();
        generateNFZs();

        controllerHeight = buildingHeights[(GridSize * (GridCellSize + 1)) / 2 - 1, (GridSize * (GridCellSize + 1)) / 2 - 1];

        // Initialize the delivery generator.
        DeliveryGenerator.Init(cityBoundX, cityBoundZ, GridSize, GridCellSize, buildingSizeX, buildingSizeZ, buildingHeights, isNFZ, isRoad);

        // Spawn and initialise drones.
        for (int i = 0; i < NumberOfDrones; i++)
        {
            Drone drone = Instantiate(dronePrefab, new Vector3(controllerLocation.x, controllerHeight + 1, controllerLocation.y), Quaternion.identity) as Drone;
            drone.Init(this, DroneSpeed, MinBuildingHeight + HeightVariation + HeightClearance);
            Drones.Add(drone);
            drone.StartOfWaiting = Time.time;
            WaitingDrones.Add(drone);
            
        }

        // Start generating deliveries.
        DeliveryGenerator.StartGenerating();

        // Start logging metrics.
        Logger.StartLogging();

        // Run simulation for specified amount of time.
        if (simulationSeconds > 0)
        {
            StartCoroutine(runSimulationForSeconds(simulationSeconds));
        }


        
        
    }

    public void Update()
    {
        if (simulationAlreadyEnded)
        {
            foreach (Drone drone in Drones)
            {
                drone.Stop();
            }
            return;
        }

        if (simulationOver)
        {

            float endTime = Time.time;

            foreach (Drone drone in Drones)
            {
                drone.Stop();
            }

            DeliveryGenerator.StopGenerating();

            Logger.StopLogging();

            Logger.CalculateDroneUtilisation();

            simulationAlreadyEnded = true;
            return;
        }

        if (WaitingDrones.Count > 0 && DeliveryScheduler.IsReady())
        {
            Drone drone = WaitingDrones[0];
            WaitingDrones.RemoveAt(0);
        
            Delivery delivery = DeliveryScheduler.GetAndRemoveNextDelivery();
            drone.Delivery = delivery;
            delivery.SetState(DeliveryState.ACCEPTED);
            // Give drone waypoints to make delivery and return to controller, avoid NFZs.
            if (setFlightPath(drone, delivery))
            {
                drone.WaitingTime += (Time.time - drone.StartOfWaiting);

                drone.FlyToWaypoint();
            }
        }
       
    }

    public void TellWaiting(Drone drone)
    {
        droneInfoDict.Remove(drone);
        drone.StartOfWaiting = Time.time;
        WaitingDrones.Add(drone);
        
    }

    public void QueueDelivery(Delivery delivery)
    {
        DeliveryScheduler.AddDelivery(delivery);
        Logger.LogDeliveryRequest();
        
    }

    public WaypointInfo GetNextWaypointInfo(Drone drone)
    {
        return droneInfoDict[drone].getNextWaypointInfo();
    }

    public void LogCompleteDelivery(int profit, float timeTaken, float jobLength)
    {
        Logger.LogCompleteDelivery(profit, timeTaken, jobLength);
    }

    public void LogAcceptedDelivery(float timeTaken, Vector3 pos)
    {
        Logger.LogAcceptedDelivery(timeTaken, getGridCoords(pos));
    }

    public void RemoveDelivery(Delivery delivery)
    {
        DeliveryScheduler.RemoveDelivery(delivery);
    }

    public Vector2 GetControllerLocation() {
        return controllerLocation;
    }

    public void InitFlightPath(Delivery delivery)
    {
        int flightAltitude = MinBuildingHeight + HeightVariation + HeightClearance;
        int heightGain = flightAltitude - controllerHeight;
        int heightLoss = flightAltitude - delivery.BuildingHeight;

        delivery.FlightPath = new FlightPath(getGridCoords(controllerLocation), getGridCoords(delivery.GetDestination()), isNFZ, flightAltitude, buildingSizeZ, buildingSizeZ, heightGain, heightLoss);   
    }

    // Pre: drone is landed at controller.
    // Returns false if unreachable.
    private bool setFlightPath(Drone drone, Delivery delivery)
    {

        Vector3 destination = delivery.GetDestination();
        float flightAltitude = MinBuildingHeight + HeightVariation + HeightClearance;
        List<Vector3> path = delivery.FlightPath.GetPath();

        if (path.Count == 1)
        {
            return false;
        }

        List<WaypointInfo> waypointInfos = new List<WaypointInfo>();
        waypointInfos.Add(new WaypointInfo(WaypointType.TAKEOFF));

        foreach (var loc in path)
        {
            waypointInfos.Add(new WaypointInfo(loc));
        }


        waypointInfos.Add(new WaypointInfo(WaypointType.LAND));
        waypointInfos.Add(new WaypointInfo(WaypointType.DELIVER));
        waypointInfos.Add(new WaypointInfo(WaypointType.TAKEOFF));

        path.Reverse();

        foreach (var loc in path)
        {
            waypointInfos.Add(new WaypointInfo(loc));
        }

        waypointInfos.Add(new WaypointInfo(WaypointType.LAND));
        waypointInfos.Add(new WaypointInfo(WaypointType.END));

        droneInfoDict.Add(drone, new DroneInfo(waypointInfos));
        drone.WaypointInfo = droneInfoDict[drone].getNextWaypointInfo();
        return true;
    }

    private void generateCity()
    {
        buildingHeights = new int[GridSize * (GridCellSize + 1) - 1, GridSize * (GridCellSize + 1) - 1];
        isRoad = new bool[GridSize * (GridCellSize + 1) - 1, GridSize * (GridCellSize + 1) - 1];

        for (int i = 0; i < isRoad.GetLength(0); i++)
        {
            for (int j = 0; j < isRoad.GetLength(1); j++)
            {
                isRoad[i, j] = true;
            }
        }

        for (int i = 0; i < buildingHeights.GetLength(0); i++)
        {
            for (int j = 0; j < buildingHeights.GetLength(1); j++)
            {
                buildingHeights[i, j] = 0;
            }
        }

        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                generateGridCell(i, j);
            }
        }
    }

    private void generateGridCell(int x, int z)
    {
        
        float cellSizeX = (GridCellSize + 1) * buildingSizeX;
        float cellSizeZ = (GridCellSize + 1) * buildingSizeZ;

        for (int i = 0; i < GridCellSize; i++)
        {
            for (int j = 0; j < GridCellSize; j++)
            {
                int buildingHeight = MinBuildingHeight + UnityEngine.Random.Range(0, HeightVariation + 1);
                GameObject building = Instantiate(buildingPrefab, new Vector3(x * cellSizeX + i * buildingSizeX, 0, z * cellSizeZ + j * buildingSizeZ), Quaternion.identity);
                building.transform.localScale += new Vector3(0, buildingHeight, 0);
                building.transform.position += new Vector3(0, building.transform.localScale.y / 2, 0);

                buildingHeights[x * (GridCellSize + 1) + i, z * (GridCellSize + 1) + j] = buildingHeight;
                isRoad[x * (GridCellSize + 1) + i, z * (GridCellSize + 1) + j] = false;
            }
        }

        for (int i = 0; i < GridCellSize; i++)
        {
            if (z != GridSize - 1)
            {
                Instantiate(RoadHorizontal, new Vector3(x * cellSizeX + i * buildingSizeX, 0, (z + 1) * cellSizeZ - buildingSizeZ), Quaternion.identity);
            }
            if (x != GridSize - 1)
            {
                Instantiate(RoadVertical, new Vector3((x + 1) * cellSizeX - buildingSizeX, 0, z * cellSizeZ + i * buildingSizeZ), Quaternion.identity);
            }
        }

        if (x != GridSize - 1 && z != GridSize - 1)
        {
            Instantiate(RoadIntersection, new Vector3((x + 1) * cellSizeX - buildingSizeX, 0, (z + 1) * cellSizeZ - buildingSizeZ), Quaternion.identity);
        }
    }

    private void generateNFZs()
    {
        isNFZ = new bool[GridSize * (GridCellSize + 1) - 1, GridSize * (GridCellSize + 1) - 1];
        for (int i = 0; i < isNFZ.GetLength(0); i++)
        {
            for (int j = 0; j < isNFZ.GetLength(1); j++)
            {
                isNFZ[i, j] = false;
            }
        }

        float cellSizeX = (GridCellSize + 1) * buildingSizeX;
        float cellSizeZ = (GridCellSize + 1) * buildingSizeZ;

        for (int i = 0; i < NFZCount; i++)
        {
            int sizeX;
            int sizeZ;
            int gridCellX;
            int gridCellZ;

            do
            {
                sizeX = UnityEngine.Random.Range((int)MinNFZSize, (int)MaxNFZSize);
                sizeZ = UnityEngine.Random.Range((int)MinNFZSize, (int)MaxNFZSize);

                gridCellX = UnityEngine.Random.Range(0, GridSize * (GridCellSize + 1) - sizeX);
                gridCellZ = UnityEngine.Random.Range(0, GridSize * (GridCellSize + 1) - sizeZ);

            } while (doesCollideWithController(gridCellX, sizeX, gridCellZ, sizeZ));

            for (int j = 0; j < sizeX; j++)
            {
                for (int k = 0; k < sizeZ; k++)
                {
                    isNFZ[gridCellX + j, gridCellZ + k] = true;
                }
            }

            float actualSizeX = sizeX * buildingSizeX;
            float actualSizeZ = sizeZ * buildingSizeZ;

            float centerPosX = buildingSizeX * gridCellX + (actualSizeX - buildingSizeX) / 2;
            float centerPosZ = buildingSizeZ * gridCellZ + (actualSizeZ - buildingSizeZ) / 2;

            GameObject noFlyZone = Instantiate(NFZPrefab);
            
            noFlyZone.transform.position += new Vector3(centerPosX, 0, centerPosZ);
            noFlyZone.transform.localScale += new Vector3(actualSizeX, 0, actualSizeZ);
        }
    }

    private bool doesCollideWithController(int x1, int sizeX, int z1, int sizeZ)
    {
        int controllerX = (GridSize * (GridCellSize + 1)) / 2 - 1;
        int controllerZ = (GridSize * (GridCellSize + 1)) / 2 - 1;
        int x2 = x1 + sizeX - 1;
        int z2 = z1 + sizeZ - 1;
        return (controllerX >= x1 && controllerX <= x2 && controllerZ >= z1 && controllerZ <= z2);
    }

    private IEnumerator runSimulationForSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("STOP!");
        SimulationCompleteText.text = "SIMULATION COMPLETE";
        simulationOver = true;
    }

    public void StopSimulation()
    {
        Debug.Log("STOP CLICKED!");
        SimulationCompleteText.text = "SIMULATION COMPLETE";
        simulationOver = true;
    }

    private Vector2Int getGridCoords(Vector3 worldCoords)
    {
        return new Vector2Int(Mathf.FloorToInt((worldCoords.x + buildingSizeX / 2) / buildingSizeX), Mathf.FloorToInt((worldCoords.z + buildingSizeZ / 2) / buildingSizeZ));
    }

    private Vector2Int getGridCoords(Vector2 worldCoords)
    {
        return new Vector2Int(Mathf.FloorToInt((worldCoords.x + buildingSizeX / 2) / buildingSizeX), Mathf.FloorToInt((worldCoords.y + buildingSizeZ / 2) / buildingSizeZ));
    }
}
