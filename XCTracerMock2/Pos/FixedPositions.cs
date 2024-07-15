namespace XCTracerMock.Pos
{
  public static class FixedPositions
  {
    public static GPSPosition Leuven()
    {
      var pos = new GPSPosition
      {
        Latitude = 50.8795258,
        Longitude = 4.7012044,
        Altitude = 100
      };

      return pos;
    }

    public static GPSPosition Chabre()
    {
      var pos = new GPSPosition
      {
        Latitude = 44.297410,
        Longitude = 5.762342,
        Altitude = 1328
      };

      return pos;
    }

    public static GPSPosition BucSederon()
    {
      var pos = new GPSPosition
      {
        Latitude = 44.21265,
        Longitude = 5.47826,
        Altitude = 1310
      };

      return pos;
    }
  }
}
