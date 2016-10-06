﻿using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using NLog;
using System.Net.Http;

namespace EchoBridge
{
    public static class Utilities
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public static string GetRandomUuidString()
        {
            Logger.Info("RandomUUIDString generated.");
            return Guid.NewGuid().ToString();
            //return "88f6698f-2c83-4393-bd03-cd54a9f8595"; // https://xkcd.com/221/
        }


        public static bool MakeHttpRequest(string url, string httpVerb, string contentType, string body)
        {
            Logger.Info("Performing HTTPRequest URL:[{0}], Method: [{1}], ContentType: [{2}], Body: [{3}]", url, httpVerb, contentType, body);

            HttpResponseMessage response = null;
            var content = new StringContent(body, Encoding.UTF8, contentType);
            if (string.IsNullOrEmpty(httpVerb)) httpVerb = "GET";

            using (var client = new HttpClient())
            {
                switch (httpVerb.ToUpper())
                {
                    case "GET":
                        response = client.GetAsync(url).Result;
                        break;
                    case "POST":
                        if (contentType == "application/json")
                        {
                            response = client.PostAsJsonAsync(url, content).Result;
                        }
                        else
                        {
                            response = client.PostAsync(url, content).Result;
                        }
                        break;
                    case "PUT":
                        if (contentType == "application/json")
                        {
                            response = client.PutAsJsonAsync(url, content).Result;
                        }
                        else
                        {
                            response = client.PutAsync(url, content).Result;
                        }
                        break;
                    default:
                        Logger.Warn("Unsupported HTTPverb: " + httpVerb);
                        throw new ApplicationException("Unsupported HTTPverb: " + httpVerb);
                }

            }
            Logger.Info("HTTPRequest returned with StatusCode [{0}] and ReasonPhrase [{1}],", response.StatusCode, response.ReasonPhrase);
            return response.IsSuccessStatusCode;


        }





        public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            Logger.Info("Writing XML file [{0}]...", filePath);
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an XML file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the XML file.</returns>
        // FROM http://blog.danskingdom.com/saving-and-loading-a-c-objects-data-to-an-xml-json-or-binary-file/
        // EXAMPLES 
        //      List<Person> people = XmlSerialization.ReadFromXmlFile<List<Person>>("C:\people.txt");
        //      XmlSerialization.WriteToXmlFile<List<People>>("C:\people.txt", people);
        public static T ReadFromXmlFile<T>(string filePath) where T : new()
        {
            Logger.Info("Reading XML file [{0}]...", filePath);
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }


        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            Logger.Info("Writing JSON file [{0}]...", filePath);
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = Newtonsoft.Json.JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the Json file.</returns>
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            Logger.Info("Reading JSON file [{0}]...", filePath);
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
    }

}
