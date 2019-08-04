using Assets.Scripts;
using System;
using System.Collections;
using UnityEngine;

public class Delivery : MonoBehaviour, IComparable<Delivery>
{
    public Controller Controller;
    
    public long ID { get; set; }
    public int Value { get; set; }
    public int BuildingHeight { get; set; }
    public FlightPath FlightPath { get; set; }
    public float Priority { get; set; }
    public bool[] TimeValueFunc;
    public float TimeStep { get; set; }

    private DeliveryState deliveryState = DeliveryState.WAITING;

    public float creationTime;
    private float acceptedTime;
    private float completedTime;

    private Color waitingColor = new Color(255, 0, 0, 0.3f);
    private Color acceptedColor = new Color(0, 255, 0, 0.3f);

    void Start()
    {
        creationTime = Time.time;
    }

    public int GetValueAfterTime(float delay)
    {
        int totalDecreases = 0;
        foreach (bool b in TimeValueFunc)
        {
            if (b)
            {
                totalDecreases++;
            }
        }
        
        if (totalDecreases == 0)
        {
            return Value;
        }
        int decreaseAmount = Value / totalDecreases;
        float timeSinceCreation = (Time.time + delay) - creationTime;
        int index = Mathf.FloorToInt(timeSinceCreation / TimeStep);
        // Index of 0 means no reduction in value.
        if (index == 0)
        {
            return Value;
        }
        
        if (index > TimeValueFunc.Length)
        {
            return 0;
        }

        int decreaseCount = 0;

        for (int i = 0; i < index; i++)
        {
            if (TimeValueFunc[i])
            {
                decreaseCount++;
            }
        }

        // to ensure 0 (avoids rounding errors from integer division)
        if (decreaseCount == totalDecreases)
        {
            return 0;
        }

        int valueAfterTime = Value - (decreaseCount * decreaseAmount);
        return valueAfterTime;
    }

    public DeliveryState GetState()
    {
        return deliveryState;
    }

    public void SetState(DeliveryState state)
    {
      
        this.deliveryState = state;

        if (state == DeliveryState.COMPLETE)
        {
            completedTime = Time.time;
            float timeWaitingForCompletion = completedTime - creationTime;

            float jobLength = completedTime - acceptedTime;

            Controller.LogCompleteDelivery(this.GetValueAfterTime(0.0f), timeWaitingForCompletion, jobLength);

            Destroy(gameObject);

        } else if (state == DeliveryState.ACCEPTED)
        {
            acceptedTime = Time.time;
            float timeWaitingForAccept = acceptedTime - creationTime;

            Controller.LogAcceptedDelivery(timeWaitingForAccept, GetDestination());

            //TODO: Check this is correct.
            gameObject.GetComponent<Renderer>().material.color = acceptedColor;
        } else if (state == DeliveryState.WAITING)
        {
            gameObject.GetComponent<Renderer>().material.color = waitingColor;
        }

    }

    public Vector3 GetDestination()
    {
        return gameObject.transform.position;
    }

    public void StartTimeout(int timeout)
    {
        StartCoroutine(removeAfter(timeout));
    }

    private IEnumerator removeAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (deliveryState == DeliveryState.ACCEPTED)
        {
            yield break;
        }
        Controller.RemoveDelivery(this);
        Destroy(gameObject);
    }

    public int CompareTo(Delivery other)
    {
        if (this.ID == other.ID)
        {
            return 0;
        }
        return this.Priority.CompareTo(other.Priority);
    }
}



public enum DeliveryState
{
    WAITING, ACCEPTED, COMPLETE
}
