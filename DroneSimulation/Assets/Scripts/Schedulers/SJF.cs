using System.Collections.Generic;

public class SJF : DeliveryScheduler
{
    public int QueueLength = -1;

    private SortedSet<Delivery> deliveries = new SortedSet<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        delivery.Priority = delivery.FlightPath.GetPathLength();

        if (QueueLength > 0 && deliveries.Count >= QueueLength)
        {
            //Low priority is good...
            if (delivery.Priority < deliveries.Max.Priority)
            {
                deliveries.Remove(deliveries.Max);
                deliveries.Add(delivery);
            } else
            {
                return;
            }
            
        } else
        {
            deliveries.Add(delivery);
        }

    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        Delivery delivery = deliveries.Min;
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
