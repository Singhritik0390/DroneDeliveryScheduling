using System.Collections.Generic;

public class HVDxSJF : DeliveryScheduler
{
    public Controller Controller;
    public int QueueLength = -1;
    public float HVDWeight = 30.0f;
    public float SJFWeight = 1.0f;


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
            float priorityHVD =  HVDWeight * delivery.GetValueAfterTime(jobTime) / (jobTime * 2);
            float prioritySJF = SJFWeight * jobTime * 2;
            delivery.Priority = priorityHVD - prioritySJF;
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
