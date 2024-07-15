namespace XCTracerMock
{
  using System;
  using System.Text;

  public class XCSppReceivedDataEventArgs
  {
    private readonly byte[] _data;

    internal XCSppReceivedDataEventArgs(byte[] data)
    {
      _data = data;
    }

    /// <summary>
    /// Received data as byte[].
    /// </summary>
    public byte[] DataBytes { get => _data; }

    /// <summary>
    /// Received data as string.
    /// </summary>
    public String DataString { get => Encoding.UTF8.GetString(_data, 0, _data.Length); }
  }
}
