namespace XCTracerMock.Pos
{
  public class GPSPosition
  {
    public GPSPosition()
    {
    }

    public GPSPosition(double latitude, double longitude, double altitude, double groundSpeed, double heading)
    {
      Latitude = latitude;
      Longitude = longitude;
      Altitude = altitude;
      GroundSpeed = groundSpeed;
      Heading = heading;
    }

    public double Latitude
    {
      get; set;
    }

    public double Longitude
    {
      get; set;
    }

    public double Altitude
    {
      get; set;
    }

    public double GroundSpeed
    {
      get; set;
    }

    public double Heading
    {
      get; set;
    }
  }
}
