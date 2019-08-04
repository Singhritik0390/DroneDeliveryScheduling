using System.Collections.Generic;
using UnityEngine;

public class LLVD : DeliveryScheduler
{
    public Controller Controller;
    public int QueueLength = -1;

    private List<Delivery> deliveries = new List<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        deliveries.Add(delivery);
    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        updateNLVDs();
        deliveries.Sort();
        if (QueueLength > 0)
        {
            while (deliveries.Count > QueueLength)
            {
                deliveries.RemoveAt(deliveries.Count - 1);
            }
        }
        Delivery delivery = deliveries[0];
        deliveries.RemoveAt(0);
        
        return delivery;
    }

    public override bool IsReady()
    {
        return (deliveries.Count > 0);
    }

    private void updateNLVDs()
    {
        float currentTime = Time.time;
        float lost = 0;
        float won = 0;
        float netLostValueDensity = 0;
        foreach (Delivery delivery in deliveries)
        {
            lost = PLVD(delivery, currentTime);
            won = PGVD(delivery, currentTime);
            netLostValueDensity = lost - won;
            delivery.Priority = netLostValueDensity;
        }
    }

    private float PLVD(Delivery delivery, float currentTime)
    {
        float lostValueDensity = 0;
        float jobLength = (delivery.FlightPath.GetPathLength() * 2) / Controller.DroneSpeed;
        foreach (Delivery otherDelivery in deliveries)
        {
            if (otherDelivery != delivery)
            {
                float otherJobLength = otherDelivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
                lostValueDensity += (otherDelivery.GetValueAfterTime(otherJobLength) - otherDelivery.GetValueAfterTime(jobLength + otherJobLength)) / (otherJobLength * 2);
            }
        }
        return lostValueDensity;
    }

    private float PGVD(Delivery delivery, float currentTime)
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

        return (delivery.GetValueAfterTime(jobLength) - delivery.GetValueAfterTime(jobLength + averageDurationOfOthers)) / (jobLength * 2);
    }

    public override void RemoveDelivery(Delivery delivery)
    {
        deliveries.Remove(delivery);
    }
}
