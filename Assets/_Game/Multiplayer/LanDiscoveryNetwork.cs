using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TankGame.Multiplayer
{
    internal sealed class LanDiscoveryNetwork
    {
        public readonly string Signature;
        public readonly IPAddress[] Destinations;

        internal LanDiscoveryNetwork(string signature, IPAddress[] destinations)
        { Signature = signature; Destinations = destinations; }

        internal static LanDiscoveryNetwork Read()
        {
            var targets = new HashSet<string> { IPAddress.Broadcast.ToString() };
            var signature = new List<string>();
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up || network.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || network.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                var properties = network.GetIPProperties();
                foreach (var entry in properties.UnicastAddresses)
                {
                    if (!IsUnicast(entry.Address) || entry.IPv4Mask == null) continue;
                    byte[] address = entry.Address.GetAddressBytes(), mask = entry.IPv4Mask.GetAddressBytes();
                    signature.Add(network.Id + ":" + entry.Address + "/" + entry.IPv4Mask);
                    if (Array.TrueForAll(mask, value => value == 255) || Array.TrueForAll(mask, value => value == 0)) continue;
                    byte[] broadcast = new byte[4];
                    for (int i = 0; i < 4; i++) broadcast[i] = (byte)(address[i] | ~mask[i]);
                    targets.Add(new IPAddress(broadcast).ToString());
                }
                // Hotspot owners are commonly the detected gateway. Never hard-code an Android subnet.
                foreach (var gateway in properties.GatewayAddresses)
                    if (IsUnicast(gateway.Address)) { targets.Add(gateway.Address.ToString()); signature.Add("gw:" + gateway.Address); }
            }
            signature.Sort(StringComparer.Ordinal);
            var addresses = new List<IPAddress>();
            foreach (string target in targets) { if (addresses.Count == 32) break; addresses.Add(IPAddress.Parse(target)); }
            return new LanDiscoveryNetwork(string.Join(";", signature), addresses.ToArray());
        }

        internal static bool IsUnicast(IPAddress address)
        {
            if (address == null || address.AddressFamily != AddressFamily.InterNetwork) return false;
            byte first = address.GetAddressBytes()[0];
            return first != 0 && first < 224 && !IPAddress.IsLoopback(address);
        }
    }
}
