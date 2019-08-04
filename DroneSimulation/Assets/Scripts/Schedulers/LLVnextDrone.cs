using System.Collections.Generic;
using UnityEngine;

public class LLVnextDrone : DeliveryScheduler
{
    public Controller Controller;
    public int QueueLength = -1;

    private List<float> returnTimes = new List<float>();

    private List<Delivery> deliveries = new List<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        deliveries.Add(delivery);
    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        for (int i = returnTimes.Count - 1; i >= 0; i--)
        {
            if (returnTimes[i] <= Time.time)
            {
                returnTimes.RemoveAt(i);
            }
        }

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

        returnTimes.Add(Time.time + (delivery.FlightPath.GetPathLength() * 2) / Controller.DroneSpeed);

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

        float timeUntilNextDispatch = 0;

        if (Controller.WaitingDrones.Count == 0)
        {
            float soonestReturn = float.PositiveInfinity;
            foreach (float t in returnTimes)
            {
                soonestReturn = Mathf.Min(t, soonestReturn);
            }

            timeUntilNextDispatch = Mathf.Min(soonestReturn - Time.time, jobLength);
        }

        foreach (Delivery otherDelivery in deliveries)
        {
            if (otherDelivery != delivery)
            {
                float otherJobLength = otherDelivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
                lostValue += otherDelivery.GetValueAfterTime(otherJobLength) - otherDelivery.GetValueAfterTime(timeUntilNextDispatch + otherJobLength);
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


        float timeUntilNextDispatch = 0;

        if (Controller.WaitingDrones.Count == 0)
        {
            float soonestReturn = float.PositiveInfinity;
            foreach (float t in returnTimes)
            {
                soonestReturn = Mathf.Min(t, soonestReturn);
            }

            timeUntilNextDispatch = Mathf.Min(soonestReturn - Time.time, averageDurationOfOthers);
        }

        return delivery.GetValueAfterTime(jobLength) - delivery.GetValueAfterTime(jobLength + timeUntilNextDispatch);
    }

    public override void RemoveDelivery(Delivery delivery)
    {
        deliveries.Remove(delivery);
    }
}
