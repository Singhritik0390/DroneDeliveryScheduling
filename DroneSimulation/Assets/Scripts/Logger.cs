using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Logger : MonoBehaviour
{

    public Controller Controller;

    public float LogFrequency = 10.0f;

    public Text ProfitText;
    public Text CompleteCountText;
    public Text TotalCountText;
    public Text AcceptTimeText;
    public Text CompleteTimeText;
    public Text JobLengthText;
    public Text DroneUtilisationText;

    private int totalProfit = 0;
    private int completeCount = 0;
    private int totalCount = 0;
    private int acceptedCount = 0;
    private float totalTimeWaitingForAccept = 0.0f;
    private float avgAcceptTime = 0.0f;
    private float totalTimeWaitingForComplete = 0.0f;
    private float avgCompleteTime = 0.0f;
    private float totalJobLength = 0.0f;
    private float avgJobLength = 0.0f;
    private float droneUtilisation = 0.0f;

    private float startTime;

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    public void StartLogging()
    {
        InvokeRepeating("PrintMetrics", LogFrequency, LogFrequency);
    }

    public void StopLogging()
    {
        CancelInvoke("PrintMetrics");
    }

    public void LogDeliveryRequest()
    {
        totalCount++;
        TotalCountText.text = totalCount.ToString();
    }

    public void LogCompleteDelivery(int profit, float timeTaken, float jobLength)
    {
        totalProfit += profit;
        ProfitText.text = totalProfit.ToString();

        completeCount++;
        CompleteCountText.text = completeCount.ToString();

        totalTimeWaitingForComplete += timeTaken;
        avgCompleteTime = totalTimeWaitingForComplete / completeCount;
        CompleteTimeText.text = avgCompleteTime.ToString();

        totalJobLength += jobLength;
        avgJobLength = totalJobLength / completeCount;
        JobLengthText.text = avgJobLength.ToString();
    }

    public void LogAcceptedDelivery(float timeTaken, Vector2Int gridPos)
    {
        acceptedCount++;
        totalTimeWaitingForAccept += timeTaken;
        avgAcceptTime = totalTimeWaitingForAccept / acceptedCount;
        AcceptTimeText.text = avgAcceptTime.ToString();
        Debug.Log("ACCEPTED: " + gridPos);
    }

    public void CalculateDroneUtilisation()
    {
        float currentTime = Time.time;
        float totalWaitingTime = 0.0f;

        foreach (Drone drone in Controller.WaitingDrones)
        {
            totalWaitingTime += (currentTime - drone.StartOfWaiting);
        }
        foreach (Drone drone in Controller.Drones)
        {
            totalWaitingTime += drone.WaitingTime;
        }

        float totalDroneSeconds = (currentTime - startTime) * Controller.Drones.Count;
        droneUtilisation = 1 - (totalWaitingTime / totalDroneSeconds);

        DroneUtilisationText.text = droneUtilisation.ToString();
    }

    public void PrintMetrics()
    {
        CalculateDroneUtilisation();
        Debug.Log("METRICS: " + totalProfit + " " + completeCount + " " + totalCount + " " + avgAcceptTime + " " + avgCompleteTime + " " + avgJobLength + " " + droneUtilisation);
    }
}
