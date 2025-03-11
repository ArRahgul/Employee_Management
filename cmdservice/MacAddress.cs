using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace cmdservice
{
    public class MacAddress
    {
        public static List<string> GetMacAddresses()
        {
            // Get all network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find the network interfaces with a valid IPv4 address assigned via DHCP
            var activeInterfaces = networkInterfaces
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Where(nic => nic.GetIPProperties().UnicastAddresses
                    .Any(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork && addr.PrefixOrigin == PrefixOrigin.Dhcp));

            List<string> macAddresses = new List<string>();

            foreach (var nic in activeInterfaces)
            {
                // Get the MAC address of the active network interface
                string macAddress = nic.GetPhysicalAddress().ToString();

                // Format the MAC address to standard format
                macAddress = string.Join(":", Enumerable.Range(0, macAddress.Length / 2)
                    .Select(i => macAddress.Substring(i * 2, 2)));

                macAddresses.Add(macAddress);
            }

            if (macAddresses.Count > 0)
            {
                return macAddresses;
            }
            else
            {
                // Use getmac command as a fallback
                macAddresses = GetMacAddressesUsingGetmacCommand();
                if (macAddresses.Count > 0)
                {
                    return macAddresses;
                }
                else
                {
                    throw new Exception("No active network interface found with a DHCP-assigned IPv4 address and unable to retrieve MAC addresses using getmac command.");
                }
            }
        }

        private static List<string> GetMacAddressesUsingGetmacCommand()
        {
            List<string> macAddresses = new List<string>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "getmac";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Regex macRegex = new Regex(@"([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})");
                MatchCollection matches = macRegex.Matches(output);

                foreach (Match match in matches)
                {
                    macAddresses.Add(match.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving MAC addresses using getmac command: " + ex.Message);
            }

            return macAddresses;
        }
    }
}
