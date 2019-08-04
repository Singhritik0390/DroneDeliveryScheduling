namespace Assets.Scripts
{
    public class DroneAssignment
    {
        public Drone Drone { get; set; }
        public Delivery Delivery { get; set; }

        public DroneAssignment(Drone drone, Delivery delivery)
        {
            this.Drone = drone;
            this.Delivery = delivery;
        }
    }
}