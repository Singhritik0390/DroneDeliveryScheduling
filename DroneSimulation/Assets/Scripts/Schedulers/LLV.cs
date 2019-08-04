﻿using System.Collections.Generic;
using UnityEngine;

public class LLV : DeliveryScheduler
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
        updateNLVs();
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

    private void updateNLVs()
    {
        float currentTime = Time.time;
        int lost = 0;
        int won = 0;
        int netLostValue = 0;
        foreach (Delivery delivery in deliveries)
        {
            lost = PLV(delivery, currentTime);
            won = PGV(delivery, currentTime);
            netLostValue = lost - won;
            delivery.Priority = netLostValue;
        }
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
