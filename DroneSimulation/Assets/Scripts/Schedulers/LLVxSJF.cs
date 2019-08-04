using System.Collections.Generic;
using UnityEngine;

public class LLVxSJF : DeliveryScheduler
{
    public Controller Controller;
    public int QueueLength = -1;
    public float LLVWeight = 1.0f;
    public float SJFWeight = 1.0f;

    private List<Delivery> deliveries = new List<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        deliveries.Add(delivery);
    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        updatePriorities();
        deliveries.Sort();
        if (QueueLength > 0)
        {
            while (deliveries.Count > QueueLength)
            {
                deliveries.RemoveAt(0);
            }
        }
        Delivery delivery = deliveries[deliveries.Count - 1];
        deliveries.RemoveAt(deliveries.Count - 1);
        
        return delivery;
    }

    public override bool IsReady()
    {
        return (deliveries.Count > 0);
    }

    private void updatePriorities()
    {
        float currentTime = Time.time;
        foreach (Delivery delivery in deliveries)
        {
            float priorityLLV = LLVWeight * getNLV(delivery, currentTime);
            float prioritySJF = (SJFWeight * delivery.FlightPath.GetPathLength() * 2) / Controller.DroneSpeed;
            delivery.Priority = -1 * (priorityLLV + prioritySJF);
        }
    }


    private int getNLV(Delivery delivery, float currentTime)
    {
        int lost = PLV(delivery, currentTime);
        int won = PGV(delivery, currentTime);
        int netLostValue = lost - won;

        return netLostValue;      
    }

    private int PLV(Delivery delivery, float currentTime)
    {
        int lostValue = 0;
        float jobLength = (delivery.FlightPath.GetPathLength() * 2) / Controller.DroneSpeed;
        foreach (Delivery otherDelivery in deliveries)
        {
            if (otherDelivery != delivery)
            {
                float otherJobLength = otherDelivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
                lostValue += otherDelivery.GetValueAfterTime(otherJobLength) - otherDelivery.GetValueAfterTime(jobLength + otherJobLength);
            }
        }
        return lostValue;
    }

    private int PGV(Delivery delivery, float currentTime)
    {
        float jobLength = delivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
        float totalDurationOfOthers = 0;
        foreach (Delivery otherDelivery in deliveries)
        {
            if (otherDelivery != delivery)
            {
                totalDurationOfOthers += (otherDelivery.FlightPath.GetPathLength() * 2) / Controller.DroneSpeed;
            }
        }
        float averageDurationOfOthers = totalDurationOfOthers / (deliveries.Count - 1);

        return delivery.GetValueAfterTime(jobLength) - delivery.GetValueAfterTime(jobLength + averageDurationOfOthers);
    }

    public override void RemoveDelivery(Delivery delivery)
    {
        deliveries.Remove(delivery);
    }
}
