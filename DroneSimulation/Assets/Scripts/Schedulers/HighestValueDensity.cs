using System.Collections.Generic;

public class HighestValueDensity : DeliveryScheduler
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
            delivery.Priority = delivery.GetValueAfterTime(delivery.FlightPath.GetPathLength() / Controller.DroneSpeed) / delivery.FlightPath.GetPathLength();
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
