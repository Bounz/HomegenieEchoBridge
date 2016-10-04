﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NLog;

namespace VeraHuesBridge
{
    public class UdpStateInfo
    {
        public UdpStateInfo(UdpClient c, IPEndPoint ep )
        {
            client = c;
            endpoint = ep;
        }
        public UdpClient client;
        public IPEndPoint endpoint;
    }
    
    public class SSDPService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static bool running;
        private static string MulticastIP;
        private static string MulticastLocalIP;
        private static int MulticastPort;
        public static string UUID;
        public static int WebServerPort;
        
        private static UdpClient MulticastClient;

        private static byte[] byteDiscovery;
        public static string DiscoveryResponse;
        //{0}=IPAddress {1}=Port {2}=RandomUUID
        private static string discoveryTemplate = "HTTP/1.1 200 OK\r\n" +
            "CACHE-CONTROL: max-age=86400\r\n" +
            "EXT:\r\n" +
            "LOCATION: http://{0}:{1}/api/setup.xml\r\n" +
            "OPT: \"http://schemas.upnp.org/upnp/1/0/\"; ns=01\r\n" +
            "01-NLS: {2}\r\n" +
            "ST: urn:schemas-upnp-org:device:basic:1\r\n" +
            "USN: uuid:Socket-1_0-221438K0100073::urn:Belkin:device:**\r\n\r\n";


        //239.255.255.250   port 1900  10.10.26
        public SSDPService(string multicastIP, int multicastPort, string LocalIP, int webPort, string uuid)
        {
            logger.Info("New SSDP Service initiated on IP [{0}], port [{1}]", multicastIP, multicastPort);
            MulticastIP = multicastIP;
            MulticastPort = multicastPort;
            MulticastLocalIP = LocalIP;
            WebServerPort =webPort;
            UUID = uuid;
            DiscoveryResponse = string.Format(discoveryTemplate, MulticastLocalIP, WebServerPort, UUID);
            byteDiscovery = Encoding.ASCII.GetBytes(DiscoveryResponse);
            running = false;
        }
        public bool Start()
        {
            try
            {
                logger.Info("Starting SSDP Service on IP [{0}], port [{1}]...", MulticastIP, MulticastPort);
                MulticastClient = new UdpClient(MulticastPort);
                IPAddress ipSSDP = IPAddress.Parse(MulticastIP);

                logger.Info("Joining multicast group on IP [{0}]...", MulticastLocalIP);
                MulticastClient.JoinMulticastGroup(ipSSDP, IPAddress.Parse(MulticastLocalIP));

                running = true;

                UdpStateInfo udpListener = new UdpStateInfo(MulticastClient, new IPEndPoint(ipSSDP, MulticastPort));

                logger.Info("Starting Multicast Receiver...");
                MulticastClient.BeginReceive(new AsyncCallback(MulticastReceiveCallback), udpListener);
                logger.Info("SSDP Service started.");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error occured starting SSDP service.");
                throw ex;
            }

            return true;
        }

        public bool IsRunning()
        {
            return running;
        }

        public void Stop()
        {
            logger.Info("Stopping SSDP Service...");
            running = false;
            MulticastClient.Client.Shutdown(SocketShutdown.Both);
            MulticastClient.Close();
            logger.Info("SSDP Service stopped.");
            
        }
        public static void MulticastReceiveCallback(IAsyncResult ar)
        {
            try
            {
                
                UdpStateInfo udpListener = (UdpStateInfo)(ar.AsyncState);
                UdpClient client = udpListener.client;
                IPEndPoint endpoint = udpListener.endpoint;

                if (client != null)
                {
                    // logger.Info("Received a UDP multicast from IP [{0}], on port [{1}].", endpoint.Address.ToString(), endpoint.Port);
                    Byte[] receiveBytes = client.EndReceive(ar, ref endpoint);
                    string receiveString = Encoding.ASCII.GetString(receiveBytes);

                    //todo dw
                    //if (endpoint.Address.ToString() == "192.168.0.193") { logger.Debug("Multicast From: {0}\r\nData:\r\n{1}", endpoint.ToString(), receiveString); }
        
                    //discovery has occured, send our response
                    if (IsSSDPDiscoveryPacket(receiveString))
                    {
                        if (endpoint.Address.ToString() == "192.168.0.193") { logger.Info("Sending SSDP setup information..."); }

                        //MulticastClient.Send(byteDiscovery, byteDiscovery.Length, endpoint);
                        Socket WinSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


                        WinSocket.Connect(endpoint);
                        WinSocket.Send(byteDiscovery);
                        WinSocket.Shutdown(SocketShutdown.Both);
                        WinSocket.Close();
                        if (endpoint.ToString() == "192.168.0.193") { logger.Debug(string.Format("Sent Response To: {0}\r\nData:\r\n{1}", endpoint.ToString(), DiscoveryResponse)); }
                    }
                    else
                    {
                        if (endpoint.ToString() == "192.168.0.193") { logger.Debug("Not SSDP Packet"); }
                    }

                }
                if (running)
                {
                    //logger.Info("Restarted Multicast Receiver.");
                    MulticastClient.BeginReceive(new AsyncCallback(MulticastReceiveCallback), udpListener);
                }
                    
            }
            catch (Exception ex)
            {
                
                if (running)
                {
                    logger.Warn(ex, "Error occured in MulticastReceiveCallBack.");
                }
                else
                {
                    logger.Debug(ex, "Ignoring an Error occured in MulticastReceiveCallBack as SSDP service is not running.");
                }
                    
            }
        }

        private static bool IsSSDPDiscoveryPacket(string message)
        {
            //logger.Info("Testing if message is SSDP Discovery Packet...");
            //logger.Info("Examing message [{0}]", message);
            if (message != null && message.StartsWith("M-SEARCH * HTTP/1.1") && message.Contains("MAN: \"ssdp:discover\""))
            {
                // logger.Info("SSDP Discovery Packet detected.");
                return true;
            }
            //logger.Info("SSDP Discovery Packet not detected.");
            return false;

        }

    }


}
