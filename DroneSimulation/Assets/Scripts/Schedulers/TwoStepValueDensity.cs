using System.Collections.Generic;
using UnityEngine;

public class TwoStepValueDensity : DeliveryScheduler
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
        foreach (Delivery delivery in deliveries)
        {
            float jobTime = delivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
            float thisValueDensity = delivery.GetValueAfterTime(jobTime) / (jobTime * 2);
            float nextValueDensity = 0.0f;
            foreach(Delivery nextDelivery in deliveries)
            {
                if (nextDelivery.ID == delivery.ID)
                {
                    continue;
                }
                float nextJobTime = nextDelivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
                nextValueDensity = Mathf.Max(nextValueDensity, nextDelivery.GetValueAfterTime(jobTime * 2 + nextJobTime) / (nextJobTime * 2));
            }
            delivery.Priority = thisValueDensity + nextValueDensity;
        }

        deliveries.Sort();

        if (QueueLength > 0)
        {
            while (deliveries.Count > QueueLength)
            {
                deliveries.RemoveAt(0);
            }
        }

        Delivery result = deliveries[deliveries.Count - 1];
        deliveries.RemoveAt(deliveries.Count - 1);
        return result;
    }

    public override bool IsReady()
    {
        return (deliveries.Count > 0);
    }

    public override void RemoveDelivery(Delivery delivery)
    {
        deliveries.Remove(delivery);
    }
}
