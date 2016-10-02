﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using Owin;
using System.Web.Http;
using NLog;
namespace VeraHuesBridge
{
   public  class WebServer
    {

       private static Logger logger = LogManager.GetCurrentClassLogger();
        
        private IDisposable webApplication;

         public WebServer(){
             logger.Info("New webserver initiated.");
         }


        public WebServer(string ipAddress, int port, string uuid, int defaultIntensity, string deviceConfigFile)
        {
            logger.Info("New webserver initiated.  Using deviceConfig file [{0}].", deviceConfigFile);
            Globals.IPAddress=ipAddress;
            Globals.Port = port;
            Globals.BaseAddress = "http://" + Globals.IPAddress + ":" + Globals.Port + "/";
            //Globals.BaseAddress = "http://*:8080";
            Globals.UUID = uuid;
            Globals.DefaultIntensity = defaultIntensity;

            logger.Debug(ipAddress);
            logger.Debug(port);
            logger.Debug(uuid);
            logger.Debug(defaultIntensity);

            Globals.DeviceList=new Devices(deviceConfigFile);
            logger.Info("Webserver created.  DeviceConfig holds [{0}] device(s)", Globals.DeviceList.Count());

        }


        public void Start()
        {
            logger.Info("Webserver starting up, listening on {0}", Globals.BaseAddress);
            webApplication = WebApp.Start<WebServerStartup>(url: Globals.BaseAddress);
            logger.Info("Webserver started.");
            //System.Threading.Thread.Sleep(50000);
        }

        public void Stop()
        {
            logger.Info("Webserver stopping...");
            webApplication.Dispose();
            logger.Info("Webserver stopped.");
        }
  }
}
