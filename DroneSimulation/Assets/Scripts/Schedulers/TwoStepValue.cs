using System.Collections.Generic;
using UnityEngine;

public class TwoStepValue : DeliveryScheduler
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
            int thisValue = delivery.GetValueAfterTime(jobTime);
            int nextValue = 0;
            foreach(Delivery nextDelivery in deliveries)
            {
                if (nextDelivery.ID == delivery.ID)
                {
                    continue;
                }
                float nextJobTime = nextDelivery.FlightPath.GetPathLength() / Controller.DroneSpeed;
                nextValue = Mathf.Max(nextValue, nextDelivery.GetValueAfterTime(jobTime * 2 + nextJobTime));
            }
            delivery.Priority = thisValue + nextValue;
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
