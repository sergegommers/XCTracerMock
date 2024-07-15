namespace XCTracerMock
{
  using nanoFramework.Networking;
  using nanoFramework.Runtime.Native;
  using nanoFramework.WebServer;
  using System;
  using System.Diagnostics;
  using System.Threading;
  using XCTracerMock.Pos;

  public static class Program
  {
    static XCTracerSpp spp;

    private static GPSPosition navPosition;

    private static Navigation navigation;

    private static WebServer server;

    private const string Ssid = "C0227-LT008 6731";
    private const string Password = "3c0L0-98";

    private static double headingDelta = 0.0f;

    public static void Main()
    {
      // Create Instance of Bluetooth Serial profile
      spp = new XCTracerSpp();

      // Add event handles for received data and Connections 
      spp.ReceivedData += Spp_ReceivedData;
      spp.ConnectedEvent += Spp_ConnectedEvent;

      // Start Advertising SPP service
      spp.Start("XC-Tracer");

      navPosition = FixedPositions.Chabre();

      navigation = new Navigation(navPosition)
      {
        Heading = 0.0,
        AirSpeed = 0,
        TimeDelta = 1000
      };

      var res = WifiNetworkHelper.ConnectDhcp(Ssid, Password, token: new CancellationTokenSource(60_000).Token);
      if (!res)
      {
        Console.WriteLine("Can't connect to the WiFi, check your credentials.");
        return;
      }

      // Setting up the web server
      server = new WebServer(80, HttpProtocol.Http);
      server.CommandReceived += ServerCommandReceived;
      server.Start();

      var time = DateTime.Parse("2024-6-27T12:55:0Z");

      Rtc.SetSystemTime(time);

      while (true)
      {
        Thread.Sleep(1000);
        if (spp.IsConnected)
        {
          navPosition = navigation.UpdatePosition(headingDelta);

          string nmea = NmeaGenerator.GetGPGGA(navPosition);
          spp.SendString($"{nmea}\r\n");

          nmea = NmeaGenerator.GetGNRMC(navPosition);
          spp.SendString($"{nmea}\r\n");

          // report GPS satellites
          spp.SendString("$GPGSV,2,1,08,02,74,042,45,04,18,190,36,07,67,279,42,12,29,323,36*77\r\n");
          spp.SendString("$GPGSV,2,2,08,15,30,050,47,19,09,158,,26,12,281,40,27,38,173,41*7B\r\n");
        }
      }
    }

    private static void ServerCommandReceived(object obj, WebServerEventArgs e)
    {
      const string PageProcess = "req";
      const string ParamHeading = "he";
      const string ParamAirSpeed = "as";
      const string ParamWindDirection = "wd";
      const string ParamWindSpeed = "ws";
      const string ParamVerticalSpeed = "vs";

      var url = e.Context.Request.RawUrl;
      if (url.IndexOf($"/{PageProcess}") == 0)
      {
        string resp = string.Empty;
        var parameters = WebServer.DecodeParam(url);
        foreach (UrlParameter param in parameters)
        {
          if (param.Name == ParamHeading)
          {
            if (double.TryParse(param.Value, out var value))
            {
              headingDelta = value;
              resp += $"{nameof(headingDelta)}: {headingDelta} ";
            }

            continue;
          }

          if (param.Name == ParamAirSpeed)
          {
            if (double.TryParse(param.Value, out var value))
            {
              navigation.AirSpeed = value;
              resp += $"{nameof(navigation.AirSpeed)}: {navigation.AirSpeed} ";
            }

            continue;
          }

          if (param.Name == ParamWindDirection)
          {
            if (double.TryParse(param.Value, out var value))
            {
              navigation.WindDirection = value;
              resp += $"{nameof(navigation.WindDirection)}: {navigation.WindDirection} ";
            }

            continue;
          }

          if (param.Name == ParamWindSpeed)
          {
            if (double.TryParse(param.Value, out var value))
            {
              navigation.WindSpeed = value;
              resp += $"{nameof(navigation.WindSpeed)}: {navigation.WindSpeed} ";
            }

            continue;
          }

          if (param.Name == ParamVerticalSpeed)
          {
            if (double.TryParse(param.Value, out var value))
            {
              navigation.VerticalSpeed = value;
              resp += $"{nameof(navigation.VerticalSpeed)}: {navigation.VerticalSpeed} ";
            }

            continue;
          }
        }

        WebServer.OutPutStream(e.Context.Response, resp);
        return;
      }

      string strResp = string.Empty;
      strResp += "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
      strResp += "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Free Flight GPS Mock</title>";
      strResp += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/></head><body>";
      strResp += "<meta http-equiv=\"Cache-control\" content=\"no-cache\"/>";
      //create the script part
      strResp += "<script language=\"JavaScript\">var xhr = new XMLHttpRequest();function btnclicked(boxMSG, cmdSend) {";
      strResp += "document.getElementById('status').innerHTML=\"waiting\";";
      strResp += "xhr.open('GET', cmdSend + boxMSG.value);";
      strResp += "xhr.send(null); xhr.onreadystatechange = function() {if (xhr.readyState == 4) {document.getElementById('status').innerHTML=xhr.responseText;}};}";
      strResp += "</script>";
      //body
      strResp += "</head><body><table >";
      strResp += $"<tr><td>Heading</td><td><input id=\"he\" type=\"text\" value=\"{headingDelta}\" /></td><td><input id=\"HeBtn\" type=\"button\" value=\"Update\" onclick=\"btnclicked(document.getElementById ('he'),'{PageProcess}?{ParamHeading}=')\"  /></td></tr>";
      strResp += $"<tr><td>Air Speed</td><td><input id=\"as\" type=\"text\" value=\"{navigation.AirSpeed}\" /></td><td><input id=\"AsBtn\" type=\"button\" value=\"Update\" onclick=\"btnclicked(document.getElementById('as'),'{PageProcess}?{ParamAirSpeed}=')\" /></td></tr>";
      strResp += $"<tr><td>Vertical Speed</td><td><input id=\"vs\" type=\"text\" value=\"{navigation.VerticalSpeed}\" /></td><td><input id=\"VsBtn\" type=\"button\" value=\"Update\" onclick=\"btnclicked(document.getElementById('vs'),'{PageProcess}?{ParamVerticalSpeed}=')\" /></td></tr>";
      strResp += $"<tr><td>Wind Direction</td><td><input id=\"wd\" type=\"text\" value=\"{navigation.WindDirection}\" /></td><td><input id=\"WdBtn\" type=\"button\" value=\"Update\" onclick=\"btnclicked(document.getElementById('wd'),'{PageProcess}?{ParamWindDirection}=')\" /></td></tr>";
      strResp += $"<tr><td>Wind Speed</td><td><input id=\"ws\" type=\"text\" value=\"{navigation.WindSpeed}\" /></td><td><input id=\"WsBtn\" type=\"button\" value=\"Update\" onclick=\"btnclicked(document.getElementById('ws'),'{PageProcess}?{ParamWindSpeed}=')\" /></td></tr>";
      strResp += "</table><div id=\"status\"></div></body></html>";
      WebServer.OutPutStream(e.Context.Response, strResp);
    }

    private static void Spp_ConnectedEvent(IXCTracerSpp sender, EventArgs e)
    {
      Debug.WriteLine($"Client connected:{sender.IsConnected}");
    }

    private static void Spp_ReceivedData(IXCTracerSpp sender, XCSppReceivedDataEventArgs ReadRequestEventArgs)
    {
      string message = ReadRequestEventArgs.DataString;
      Debug.WriteLine($"Received=>{message}");
    }
  }
}
