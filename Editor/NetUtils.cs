#pragma warning disable IDE0130, IDE0090, IDE0063
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Neovim.Editor
{
  public static class NetUtils
  {
    private static readonly IPEndPoint DefaultLoopbackEp = new IPEndPoint(IPAddress.Loopback, port: 0);
    public static int GetRandomAvailablePort()
    {
      using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
      {
        socket.Bind(DefaultLoopbackEp);
        return ((IPEndPoint)socket.LocalEndPoint).Port;
      }
    }

#if UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX
    public static bool IsUnixSocketAlive(string path)
    {
      if (!File.Exists(path)) return false;
      try
      {
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Connect(new UnixDomainSocketEndPoint(path));
        return true;
      }
      catch (SocketException)
      {
        // stale socket — clean it up so the next launch works cleanly
        try { File.Delete(path); } catch (Exception) { }
        return false;
      }
    }
#endif // UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX

    public static bool IsPortInUse(string ip, int port)
    {
      IPAddress _ip = IPAddress.Parse(ip);
      try
      {
        TcpListener list = new TcpListener(_ip, port);
        list.Start();
        list.Stop();
      }
      catch (SocketException)
      {
        return true;
      }
      return false;
    }

    /// <summary>
    /// Whether something is accepting TCP connections on the given local address. Unlike IsPortInUse, which tries
    /// to bind the port itself, this doesn't report ports that merely can't be bound: binding a port inside a
    /// Windows excluded port range (reserved by Hyper-V/WSL/Docker) fails with "access denied" even though nothing
    /// is listening on it.
    /// </summary>
    public static bool IsListening(string ip, int port)
    {
      IPAddress addr = IPAddress.Parse(ip);
      foreach (IPEndPoint ep in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
      {
        if (ep.Port == port && (ep.Address.Equals(addr) || ep.Address.Equals(IPAddress.Any)))
          return true;
      }
      return false;
    }
  }
}
