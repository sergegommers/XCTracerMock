namespace XCTracerMock.Pos
{
  using System;

  public enum PointType
  {
    Lat,
    Lon
  }

  public class DmsPoint
  {
    // thx https://adamprescott.net/2013/07/17/convert-latitudelongitude-between-decimal-and-degreesminutesseconds-in-c/

    public int Degrees { get; set; }

    public double Minutes { get; set; }

    public PointType PointType { get; set; }

    public DmsPoint(int degrees, double minutes, PointType pointType)
    {
      Degrees = degrees;
      Minutes = minutes;
      PointType = pointType;
    }

    public static DmsPoint FromDecimal(double value, PointType pointType)
    {
      var Latitude = new DmsPoint
      (
        ExtractDegrees(value),
        ExtractMinutes(value),
        pointType
      );

      return Latitude;
    }

    public override string ToString()
    {
      var nmeaString = $"{Degrees:D2}{Minutes:F4},";
      nmeaString += PointType == PointType.Lat
        ? Degrees < 0 ? "S" : "N"
        : Degrees < 0 ? "W" : "E";

      return nmeaString;
    }

    public static int ExtractDegrees(double value)
    {
      int val = (int)value;
      return val;
    }

    public static double ExtractMinutes(double value)
    {
      value = Math.Abs(value);
      double minutes = (value - ExtractDegrees(value)) * 60;
      return minutes;
    }
  }
}
