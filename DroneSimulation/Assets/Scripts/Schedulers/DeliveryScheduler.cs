using UnityEngine;

public abstract class DeliveryScheduler : MonoBehaviour
{
    
    abstract public void AddDelivery(Delivery delivery);
    abstract public Delivery GetAndRemoveNextDelivery();
    abstract public bool IsReady();
    abstract public void RemoveDelivery(Delivery delivery);
}