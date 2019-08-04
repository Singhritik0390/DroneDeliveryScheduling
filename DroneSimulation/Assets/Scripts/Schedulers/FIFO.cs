using System.Collections.Generic;

public class FIFO : DeliveryScheduler
{
    private List<Delivery> deliveries = new List<Delivery>();

    public override void AddDelivery(Delivery delivery)
    {
        deliveries.Add(delivery);
    }

    public override Delivery GetAndRemoveNextDelivery()
    {
        Delivery delivery = deliveries[0];
        deliveries.RemoveAt(0);
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
