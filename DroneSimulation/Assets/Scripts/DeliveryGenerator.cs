using UnityEngine;

public class DeliveryGenerator : MonoBehaviour
{
    public Controller Controller;

    public Delivery deliveryPrefab;
    public float DeliveryFrequency = 15;

    public float MinTimeStep = 5.0f;
    public float MaxTimeStep = 10.0f;
    //public int FrequencyVariation;

    public int DeliveryTimeout = 300;

    private float boundX;
    private float boundZ;
    private int gridSize;
    private int gridCellSize;
    private float buildingSizeX;
    private float buildingSizeZ;
    private int[,] buildingHeights;
    private bool[,] isNFZ;
    private bool[,] isRoad;

    private long currentID = 0;

    private const int smallLetterFee = 60;
    private const int largeLetterFee = 80;
    private const int smallEnvelopeFee = 127;
    private readonly int[] standardEnvelopeFees = { 140, 154, 163 };
    private const int largeEnvelopeFee = 205;
    private readonly int[] standardParcelFees = { 202, 214, 230, 245, 268, 383 };

    public void Init(float boundX, float boundZ, int gridSize, int gridCellSize, float buildingSizeX, float buildingSizeZ, int[,] buildingHeights, bool[,] isNFZ, bool[,] isRoad)
    {
        this.boundX = boundX;
        this.boundZ = boundZ;
        this.gridSize = gridSize;
        this.gridCellSize = gridCellSize;
        this.buildingSizeX = buildingSizeX;
        this.buildingSizeZ = buildingSizeZ;
        this.buildingHeights = buildingHeights;
        this.isNFZ = isNFZ;
        this.isRoad = isRoad;
    }

    private void generateDelivery()
    {
        int gridCellX;
        int gridCellZ;

        do
        {
            gridCellX = Random.Range(0, gridSize * (gridCellSize + 1) - 1);
            gridCellZ = Random.Range(0, gridSize * (gridCellSize + 1) - 1);
            
        } while (isNFZ[gridCellX, gridCellZ] || isRoad[gridCellX, gridCellZ]);

        float posX = gridCellX * buildingSizeX;
        float posZ = gridCellZ * buildingSizeZ;
        Delivery delivery = Instantiate(deliveryPrefab, new Vector3(posX, deliveryPrefab.transform.localScale.y, posZ), Quaternion.identity) as Delivery;

        delivery.ID = currentID;
        currentID++;
        delivery.BuildingHeight = buildingHeights[gridCellX, gridCellZ];
        Controller.InitFlightPath(delivery);
        delivery.Value = pickValue();
        delivery.TimeStep = Random.Range(MinTimeStep, MaxTimeStep);
        if (Random.Range(0, 2) == 0)
        {
            // Step reduction
            delivery.TimeValueFunc = new bool[10] { true, true, true, true, true, true, true, true, true, true };
        } else
        {
            // Halves at half of duration
            delivery.TimeValueFunc = new bool[10] { false, false, false, false, true, false, false, false, false, true };
        }
        
        delivery.Controller = this.Controller;
        delivery.StartTimeout(DeliveryTimeout);

        Controller.QueueDelivery(delivery);
    }

    private int pickValue()
    {
        PackageType packageType = (PackageType)Random.Range(0, (int)PackageType.COUNT);
        switch (packageType)
        {
            case PackageType.SMALL_LETTER:
                {
                    return smallLetterFee;
                }
            case PackageType.LARGE_LETTER:
                {
                    return largeLetterFee;
                }
            case PackageType.SMALL_ENVELOPE:
                {
                    return smallEnvelopeFee;
                }
            case PackageType.STANDARD_ENVELOPE:
                {
                    return standardEnvelopeFees[Random.Range(0, standardEnvelopeFees.Length)];
                }
            case PackageType.LARGE_ENVELOPE:
                {
                    return largeEnvelopeFee;
                }
            case PackageType.STANDARD_PARCEL:
                {
                    return standardParcelFees[Random.Range(0, standardParcelFees.Length)];
                }
        }
        return 0;
    }

    public void StartGenerating()
    {
        InvokeRepeating("generateDelivery", 0.1f, DeliveryFrequency);
    }

    public void StopGenerating()
    {
        CancelInvoke("generateDelivery");
    }

}

public enum PackageType
{
    SMALL_LETTER, LARGE_LETTER, SMALL_ENVELOPE, STANDARD_ENVELOPE, LARGE_ENVELOPE, STANDARD_PARCEL, COUNT
}