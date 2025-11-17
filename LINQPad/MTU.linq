<Query Kind="Program">
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.NetworkInformation</Namespace>
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	byte[] LogicalAnd(byte[] x, byte[] y)
	{
		var result = new byte[x.Length];
		for (int i = 0; i != x.Length; ++i)
			result[i] = (byte) (x[i] & y[i]);
		return result;
	}
	
	bool Equal(byte[] x, byte[] y)
	{
		for (int i = 0; i != x.Length; ++i)
			if (x[i] != y[i])
				return false;
		return true;
	}
	
	byte[] IPv6PrefixMask(int prefixLength)
	{
		var result = new byte[16];
		int index = 0;
		while (true)
		{
			if (prefixLength >= 8)
			{
				result[index++] = 0xFF;
				prefixLength -= 8;
				if (prefixLength == 0)
					break;
			}
			else
			{
				result[index++] = (byte) (0xFF << (8 - prefixLength));
				break;
			}
		}
		return result;
	}

	//var ipAddresses = (await Dns.GetHostAddressesAsync("::1"))
	//var ipAddresses = (await Dns.GetHostAddressesAsync("localhost"))
	//var ipAddresses = (await Dns.GetHostAddressesAsync("media2020"))
	var ipAddresses = (await Dns.GetHostAddressesAsync("www.microsoft.com"))
		.Where(x => x.AddressFamily == AddressFamily.InterNetwork || x.AddressFamily == AddressFamily.InterNetworkV6)
		.ToList();
	ipAddresses.Select(x => x.ToString()).Dump();
	
	var networkInterfacesIPv4 = NetworkInterface.GetAllNetworkInterfaces()
			.Where(x => x.Supports(NetworkInterfaceComponent.IPv4))
			.Select(x =>
			{
				var ipProperties = x.GetIPProperties();
				return new NetworkAdapter(NetworkInterface: x, IPProperties: ipProperties, IPv4Properties: ipProperties.GetIPv4Properties());
			})
			.ToList();
	
	var networkInterfacesIPv6 = NetworkInterface.GetAllNetworkInterfaces()
			.Where(x => x.Supports(NetworkInterfaceComponent.IPv6))
			.Select(x =>
			{
				var ipProperties = x.GetIPProperties();
				return new NetworkAdapter(NetworkInterface: x, IPProperties: ipProperties, IPv6Properties: ipProperties.GetIPv6Properties());
			})
			.ToList();

	var ipAddress = ipAddresses.First();
	var isLoopback = ipAddress.Equals(IPAddress.Loopback) || ipAddress.Equals(IPAddress.IPv6Loopback);
	var binaryIpAddress = ipAddress.GetAddressBytes();
	var networkInterfaces = ipAddress.AddressFamily == AddressFamily.InterNetwork ? networkInterfacesIPv4 : networkInterfacesIPv6;
	var compatibleInterfaces = networkInterfaces
		.Where(x => (!isLoopback && x.IPProperties.GatewayAddresses.Count != 0) || x.IPProperties.UnicastAddresses.Where(y => y.Address.AddressFamily == ipAddress.AddressFamily).Any(y =>
		{
			var binaryMask = y.Address.AddressFamily == AddressFamily.InterNetwork ? y.IPv4Mask.GetAddressBytes() : IPv6PrefixMask(y.PrefixLength);
			return Equal(LogicalAnd(binaryMask, binaryIpAddress), LogicalAnd(binaryMask, y.Address.GetAddressBytes()));
		}));

	if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
	{
		compatibleInterfaces.Select(x => x.NetworkInterface.Name).Dump();
		var mtu = compatibleInterfaces.Min(x => (uint)x.IPv4Properties.Mtu);
		mtu.Dump();
	}
	else
	{
		compatibleInterfaces.Select(x => x.NetworkInterface.Name).Dump();
		var mtu = compatibleInterfaces.Min(x => (uint)x.IPv6Properties.Mtu);
		mtu.Dump();
	}
}

sealed record NetworkAdapter(System.Net.NetworkInformation.NetworkInterface NetworkInterface, IPInterfaceProperties IPProperties, IPv4InterfaceProperties IPv4Properties = null, IPv6InterfaceProperties IPv6Properties = null);
