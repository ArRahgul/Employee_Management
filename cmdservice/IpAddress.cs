/*using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace cmdservice
{
    public class IpAddress
    {
        public static string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            try
            {
                bool dnsSuffixFound = false;
                NetworkInterface selectedInterface = null;

                // First, check for the DNS suffix "lan"
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties properties = ni.GetIPProperties();
                        foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                        {
                            if (properties.DnsSuffix.Equals("lan", StringComparison.OrdinalIgnoreCase) &&
                                ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                localIP = ip.Address.ToString();
                                dnsSuffixFound = true;
                                break;
                            }
                        }

                        if (dnsSuffixFound)
                        {
                            break;
                        }
                    }
                }

                // If no DNS suffix "lan" is found, check for IPv4 addresses starting with "192.168"
                if (!dnsSuffixFound)
                {
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up)
                        {
                            IPInterfaceProperties properties = ni.GetIPProperties();

                            foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                    ip.Address.ToString().StartsWith("192.168."))
                                {
                                    localIP = ip.Address.ToString();
                                    break;
                                }
                            }

                            if (!string.IsNullOrEmpty(localIP))
                            {
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(localIP))
                {
                    throw new Exception("No network adapters with the DNS suffix 'lan' or an active IPv4 address starting with '192.168.' found in the system!");
                }
            }
            catch (Exception ex)
            {
                // Handle exception or log error message
                throw new Exception("Error retrieving local IP address: " + ex.Message);
            }

            return localIP;
        }
    }
}
*/



using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace cmdservice
{
    public class IpAddress
    {
        public static string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties properties = ni.GetIPProperties();
                        foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                        {
                            if (properties.DnsSuffix.Equals("lan", StringComparison.OrdinalIgnoreCase) &&
                                ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                localIP = ip.Address.ToString();
                                return localIP;
                            }
                        }
                    }
                }

                // If no DNS suffix "lan" is found, check for IPv4 addresses starting with "192.168"
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPInterfaceProperties properties = ni.GetIPProperties();
                        foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                                ip.Address.ToString().StartsWith("192.168."))
                            {
                                localIP = ip.Address.ToString();
                                return localIP;
                            }
                        }
                    }
                }

                throw new Exception("No network adapters with the DNS suffix 'lan' or an active IPv4 address starting with '192.168.' found in the system!");
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving local IP address: " + ex.Message);
            }
        }
    }
}
