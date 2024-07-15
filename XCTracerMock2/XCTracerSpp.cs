namespace XCTracerMock
{
  using System;
  using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
  using System.Text;
  using nanoFramework.Device.Bluetooth;
  using nanoFramework.Device.Bluetooth.Advertisement;

  /// <summary>
  /// Implementation of Nordic Serial SPP profile.
  /// </summary>
  public class XCTracerSpp : IXCTracerSpp
  {
    // UUID for Nordic UART service
    // https://developer.nordicsemi.com/nRF_Connect_SDK/doc/latest/nrf/libraries/bluetooth_services/services/nus.html#id4
    private Guid ServiceUUID = new("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    private Guid RxCharacteristicUUID = new ("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
    private Guid TxCharacteristicUUID = new ("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");

    private readonly GattServiceProvider _serviceProvider;
    private readonly GattLocalCharacteristic _txCharacteristic;
    private bool _isConnected = false;

    /// <summary>
    /// Return true id client connected
    /// </summary>
    public bool IsConnected { get => _isConnected; }

    /// <summary>
    /// Event handler for receiving data
    /// </summary>
    public event IXCTracerSpp.RxDataEventHandler ReceivedData;

    /// <summary>
    /// Event Handler for connection state change
    /// </summary>
    public event IXCTracerSpp.ConnectedEventHandler ConnectedEvent;

    /// <summary>
    /// Constructor for Nordic serial SPP profile
    /// </summary>
    public XCTracerSpp()
    {

      GattServiceProviderResult gspr = GattServiceProvider.Create(ServiceUUID);
      if (gspr.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
      {
        throw new ArgumentException("Unable to create service");
      }

      _serviceProvider = gspr.ServiceProvider;

      // Define RX characteristic
      var rxParam = new GattLocalCharacteristicParameters()
      {
        UserDescription = "RX Characteristic",
        CharacteristicProperties = GattCharacteristicProperties.Write | GattCharacteristicProperties.WriteWithoutResponse
      };

      GattLocalCharacteristicResult rxCharRes = _serviceProvider.Service.CreateCharacteristic(RxCharacteristicUUID, rxParam);
      if (rxCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
      {
        throw new ArgumentException("Unable to create RX Characteristic");
      }

      GattLocalCharacteristic rxCharacteristic = rxCharRes.Characteristic;
      rxCharacteristic.WriteRequested += RxCharacteristic_WriteRequested;


      // Define TX characteristic
      var txParam = new GattLocalCharacteristicParameters()
      {
        UserDescription = "TX Characteristic",
        CharacteristicProperties = GattCharacteristicProperties.Notify
      };

      var txCharRes = _serviceProvider.Service.CreateCharacteristic(TxCharacteristicUUID, txParam);
      if (txCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
      {
        throw new ArgumentException("Unable to create TX Characteristic");
      }

      _txCharacteristic = txCharRes.Characteristic;
      _txCharacteristic.SubscribedClientsChanged += TxCharacteristicSubscribedClientsChanged;
    }

    private void TxCharacteristicSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    {
      _isConnected = (sender.SubscribedClients.Length > 0);

      // Fire event when connection state changes
      ConnectedEvent?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// Start device advertising
    /// </summary>
    /// <param name="deviceName">Device name for Advertising</param>
    /// <returns></returns>
    public bool Start(string deviceName)
    {
      // Create a manufacturer data section:
      var manufacturerData = new BluetoothLEManufacturerData
      {
        // Set the company ID for the manufacturer data.
        // 0x000D   Texas instruments.
        CompanyId = 0x000D
      };

      // Create payload
      DataWriter writer = new();

      writer.WriteBytes(new byte[] { 0x58, 0x43, 0x54, 0x52, 0x41, 0x43, 0x45, 0x52, 0x00 });

      manufacturerData.Data = writer.DetachBuffer();

      BluetoothLEServer.Instance.DeviceName = deviceName;

      var gattServiceProviderAdvertisingParameters = new GattServiceProviderAdvertisingParameters()
      {
        IsConnectable = true,
        IsDiscoverable = true
      };

      gattServiceProviderAdvertisingParameters.Advertisement.ManufacturerData.Add(manufacturerData);
      gattServiceProviderAdvertisingParameters.Advertisement.LocalName = deviceName;

      gattServiceProviderAdvertisingParameters.Advertisement.Flags =
        BluetoothLEAdvertisementFlags.GeneralDiscoverableMode |
        BluetoothLEAdvertisementFlags.ClassicNotSupported;

      _serviceProvider.StartAdvertising(gattServiceProviderAdvertisingParameters);

      return true;
    }

    /// <summary>
    /// Stop Nordic SPP UART device
    /// Stop advertising.
    /// </summary>
    public void Stop()
    {
      _serviceProvider?.StopAdvertising();
    }

    /// <summary>
    /// Send data bytes to connected client
    /// </summary>
    /// <param name="data">byte array to send</param>
    /// <returns></returns>
    public bool SendBytes(byte[] data)
    {
      var dr = new DataWriter();
      dr.WriteBytes(data);
      GattClientNotificationResult[] results = _txCharacteristic.NotifyValue(dr.DetachBuffer());
      if (results.Length > 0 && results[0].ProtocolError == 0)
      {
        return true;
      }
      return false;
    }

    /// <summary>
    /// Send data as string
    /// </summary>
    /// <param name="data">string to send</param>
    /// <returns></returns>
    public bool SendString(string data)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(data);

      int sourceStart = 0;
      int bytesLeft = bytes.Length;

      while (bytesLeft > 20)
      {
        byte[] array = new byte[20];
        Array.Copy(bytes, sourceStart, array, 0, 20);
        SendBytes(array);

        sourceStart += 20;
        bytesLeft -= 20;
      }
      
      if (bytesLeft > 0)
      {
        byte[] array = new byte[bytesLeft];
        Array.Copy(bytes, sourceStart, array, 0, bytesLeft);
        SendBytes(array);
      }

      return true;
    }

    /// <summary>
    /// Event handler for Received data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="WriteRequestEventArgs"></param>
    private void RxCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
    {
      GattWriteRequest request = WriteRequestEventArgs.GetRequest();

      byte[] data = new byte[request.Value.Length];

      DataReader rdr = DataReader.FromBuffer(request.Value);
      rdr.ReadBytes(data);

      ReceivedData?.Invoke(this, new XCSppReceivedDataEventArgs(data));

      if (request.Option == GattWriteOption.WriteWithResponse)
      {
        request.Respond();
      }
    }
  }
}

