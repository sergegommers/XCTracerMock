namespace XCTracerMock.Pos
{
  using System;

  /// <summary>
  /// The navigation class
  /// </summary>
  public class Navigation
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Navigation"/> class.
    /// </summary>
    /// <param name="position">The position.</param>
    public Navigation(GPSPosition position)
    {
      Position = position;
    }

    /// <summary>
    /// Gets or sets the position.
    /// </summary>
    public GPSPosition Position
    {
      get;
      set;
    }

    /// <summary>
    /// Gets or sets the heading in degrees.
    /// 0 degrees is to North, 90 degrees is to East
    /// </summary>
    public double Heading
    {
      get;
      set;
    }

    /// <summary>
    /// Gets or sets the air speed in meter/second.
    /// </summary>
    public double AirSpeed
    {
      get;
      set;
    }

    /// <summary>
    /// Gets or sets the wind speed in meter/second.
    /// </summary>
    public double WindSpeed
    {
      get;
      set;
    }

    /// <summary>
    /// Gets or sets the wind direction.
    /// 0 degrees is from North, 90 degrees is from East
    /// </summary>
    public double WindDirection
    {
      get;
      set;
    }

    /// <summary>
    /// Gets or sets the time in milliseconds between position updates.
    /// </summary>
    public double TimeDelta
    {
      get;
      set;
    }

    public double VerticalSpeed
    {
      get;
      set;
    }

    /// <summary>
    /// Updates the position.
    /// </summary>
    /// <param name="headingDelta">The heading delta.</param>
    /// <param name="verticalSpeed">The vertical speed in meter/second.</param>
    /// <returns>The updated <see cref="Contracts.GPSPosition"/></returns>
    public GPSPosition UpdatePosition(double headingDelta)
    {
      Heading += headingDelta;
      while (Heading > 360.0)
      {
        Heading -= 360.0;
      }

      while (Heading < -360.0)
      {
        Heading += 360.0;
      }

      double newAltitude = Position.Altitude += this.VerticalSpeed * TimeDelta / 1000.0;
      newAltitude = Math.Max(newAltitude, 0.0);

      double realCourse = (-Heading + 90.0) * Math.PI / 180.0;

      double realWindDirection = (-WindDirection - 90.0) * Math.PI / 180.0;

      var eastDelta = (Math.Cos(realCourse) * AirSpeed + Math.Cos(realWindDirection) * WindSpeed) * TimeDelta / 1000.0;
      var northDelta = (Math.Sin(realCourse) * AirSpeed + Math.Sin(realWindDirection) * WindSpeed) * TimeDelta / 1000.0;

      var groundSpeed = Math.Sqrt(eastDelta * eastDelta + northDelta * northDelta);

      Position = this.MovePoint(Position, northDelta, eastDelta, groundSpeed, Heading);
      Position.Altitude = newAltitude;

      return Position;
    }

    private GPSPosition MovePoint(GPSPosition position, double northDelta, double eastDelta, double groundSpeed, double heading)
    {
      // Earth’s radius, sphere
      double earthRadius = 6378137;

      // Coordinate offsets in radians
      var latitudeDelta = northDelta / earthRadius;
      var longitudeDelta = eastDelta / (earthRadius * Math.Cos(Math.PI * position.Latitude / 180.0));

      // OffsetPosition, decimal degrees
      var latitude = position.Latitude + latitudeDelta * 180.0 / Math.PI;
      var longitude = position.Longitude + longitudeDelta * 180.0 / Math.PI;

      return new GPSPosition(latitude, longitude, position.Altitude, groundSpeed, heading);
    }
  }
}
