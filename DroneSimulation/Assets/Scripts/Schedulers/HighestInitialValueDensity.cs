using System.Collections.Generic;

public class HighestInitialValueDensity : DeliveryScheduler
{
    public int QueueLength = -1;

    private SortedSet<Delivery> deliveries = new SortedSet<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        delivery.Priority = delivery.Value / delivery.FlightPath.GetPathLength();

        if (QueueLength > 0 && deliveries.Count >= QueueLength)
        {
            if (delivery.Priority > deliveries.Min.Priority)
            {
                deliveries.Remove(deliveries.Min);
                deliveries.Add(delivery);
            }
            else
            {
                return;
            }

        }
        else
        {
            deliveries.Add(delivery);
        }

    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        Delivery delivery = deliveries.Max;
        deliveries.Remove(delivery);
        return delivery;
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
