namespace XCTracerMock.Pos
{
  using System;

  public static class NmeaGenerator
  {
    public static string GetGPGGA(GPSPosition postion)
    {
      var time = DateTime.UtcNow;

      var timeString = $"{time.Hour:D2}{time.Minute:D2}{time.Second:D2}";

      var latitude = DmsPoint.FromDecimal(postion.Latitude, PointType.Lat).ToString();
      var longitude = DmsPoint.FromDecimal(postion.Longitude, PointType.Lon).ToString();

      var height = $"{postion.Altitude:F1},M";

      var result = $"GPGGA,{timeString},{latitude},{longitude},1,08,0.9,{height},0,M,,";

      var checksum = GetChecksum(result);

      result = $"${result}*{checksum}";

      return result;
    }

    public static string GetGNRMC(GPSPosition postion)
    {
      var time = DateTime.UtcNow;

      var timeString = $"{time.Hour:D2}{time.Minute:D2}{time.Second:D2}.00";
      var dateString = $"{time.Day:D2}{time.Month:D2}{(time.Year-2000):D2}";

      var latitude = DmsPoint.FromDecimal(postion.Latitude, PointType.Lat).ToString();
      var longitude = DmsPoint.FromDecimal(postion.Longitude, PointType.Lon).ToString();

      var groundSpeed = $"{postion.GroundSpeed:F1}";

      var heading = $"{postion.Heading:F2}";

      var result = $"GNRMC,{timeString},A,{latitude},{longitude},{groundSpeed},{heading},{dateString},0.0,E,A";

      var checksum = GetChecksum(result);

      result = $"${result}*{checksum}";

      return result;
    }

    public static string GetChecksum(string input)
    {
      byte checksum = 0;

      for (int i = 0;i < input.Length;i++)
      {
        var c3 = input[i];

        checksum = (byte)(checksum ^ c3);
      }

      byte b = (byte)checksum;

      return b.ToString("X2");
    }
  }
}
