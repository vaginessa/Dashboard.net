﻿using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using Dashboard.net.Element_Controllers;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dashboard.net.DataHandlers
{
    public static class DataDealer
    {

        public static readonly string CHECKLISTKEY = "checklist", CAUTIONERKEY = "cautioner", CONSTANTSKEY = "constants", MISCKEY = "misc";

        private static string dataLocation;
        /// <summary>
        /// The location of the data json file.
        /// </summary>
        public static string DataLocation
        {
            get
            {
                // If the variable for the location is null or empty, fix it.
                if (string.IsNullOrEmpty(dataLocation))
                {
                    dataLocation = Path.Combine(Environment.
                        GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dashboard.net Data");

                    // If we're debugging or the data location doesn't exist, change it to the current directory
                    dataLocation = (Debugger.IsAttached) ? Environment.CurrentDirectory : dataLocation;

                    // If the directory doesn't exist, create it.
                    if (!Directory.Exists(dataLocation)) Directory.CreateDirectory(dataLocation);
                }
                return dataLocation;
            }
        }
        /// <summary>
        /// The location of the data file that contains the data.
        /// </summary>
        public static string DataFileLocation
        {
            get
            {
                return Path.Combine(DataLocation, "data.json");
            }
        }

        static object key = new object();

        /// <summary>
        /// Returns true if the data file exists, false otherwise.
        /// </summary>
        private static bool DataFileExists
        {
            get
            {
                return File.Exists(DataFileLocation);
            }
        }

        /// <summary>
        /// Creates the data file, overwritting it if it doesn't exist.
        /// </summary>
        private static void CreateDataFile()
        {
            File.Create(DataFileLocation).Close();
        }

        /// <summary>
        /// Erases the contents of the DataFile and restarts the application because of it.
        /// Cannot be undone.
        /// </summary>
        internal static void EraseDataFile()
        {
            if (DataFileExists) File.Delete(DataFileLocation);

        }

        /// <summary>
        /// Function used to restart this application.
        /// </summary>
        internal static void RestartApplication()
        {
            Process.Start(Application.ResourceAssembly.Location);
            CloseApplication();
        }

        private static void CloseApplication()
        {
            Application.Current.Shutdown();
        }

        #region Basic read write functions
        /// <summary>
        /// Encodes the data hashtable into json and writes it to the data file.
        /// </summary>
        /// <param name="dataToWrite"></param>
        private static void WriteData(Hashtable dataToWrite)
        {
            // Lock this segment so that other threads don't cause problems.
            lock (key)
            {
                // Make sure the file exists
                if (!DataFileExists) CreateDataFile();

                // Serialize hashtable
                string writable = JsonConvert.SerializeObject(dataToWrite);

                // Make new writer
                StreamWriter writer = new StreamWriter(DataFileLocation);

                // Write data.
                writer.Write(writable);

                // Close writer
                writer.Close();
            }
        }

        private static Hashtable ReadData()
        {
            string fileContents = "";
            // Lock this segment so other threads don't cause problems.
            lock (key)
            {
                if (!DataFileExists)
                {
                    CreateDataFile();
                    return new Hashtable();
                }

                StreamReader reader = new StreamReader(DataFileLocation);
                fileContents = reader.ReadLine();
                reader.Close();
            }
            if (fileContents == "" || fileContents == null) return new Hashtable();

            Hashtable data_hashtable =
                (Hashtable)JsonConvert.DeserializeObject<Hashtable>(fileContents) ;

            // Make hashtable of the data that will be returned at the end.
            Hashtable dataToBeReturned = new Hashtable();
            foreach (string key in data_hashtable.Keys)
            {
                var data = data_hashtable[key];
                if (data is JArray)
                {
                    dataToBeReturned[key] = (JArray)data;
                    Type type = dataToBeReturned[key].GetType();
                }
                else if (data is JObject)
                {
                    dataToBeReturned[key] = (JObject)data;
                }
            }

            return dataToBeReturned;
        }
        #endregion

        /// <summary>
        /// Replaces the current value of the key provided with the new value
        /// </summary>
        /// <param name="key">The key to replace</param>
        /// <param name="newValue">The new value to set that key to</param>
        private static void UpdateAndWrite(string key, object newValue)
        {
            Hashtable currentData = ReadData();
            currentData[key] = newValue;

            WriteData(currentData);
        }

        #region Checklist read write methods
        /// <summary>
        /// Method for writing checklist data to the file.
        /// </summary>
        /// <param name="CheckListData"></param>
        public static void WriteChecklistData(List<string> CheckListData)
        {
            UpdateAndWrite(CHECKLISTKEY, CheckListData);
        }

        /// <summary>
        /// Reads the file and returns the checklist data.
        /// </summary>
        /// <returns>The checklist data.</returns>
        public static List<string> ReadCheckListData()
        {
            // Read and convert then return.
            return ((JArray)ReadData()[CHECKLISTKEY])?.ToObject<List<string>>();
        }

        #endregion

        #region Cautioner read write methods
        /// <summary>
        /// Writes new checklist data to the file
        /// </summary>
        /// <param name="data">The new data to write to the file</param>
        public static void WriteCautionerData(Hashtable data)
        {
            UpdateAndWrite(CAUTIONERKEY, data);
        }
        /// <summary>
        /// Reads the cautioner data and returns it
        /// </summary>
        /// <returns>The cautioner data present on file</returns>
        public static Hashtable ReadCautionerData()
        {
            Hashtable goodData = new Hashtable();
            // Read and convert the data
            Hashtable data = ((JObject)(ReadData()[CAUTIONERKEY]))?.ToObject<Hashtable>();
            if (data == null) return null;

            goodData[Cautioner.ENABLEDKEY] = data[Cautioner.ENABLEDKEY];
            goodData[Cautioner.IGNOREKEY] = ((JArray)data[Cautioner.IGNOREKEY]).ToObject<ObservableCollection<string>>();

            return goodData;
        }
        #endregion

        #region ConstantMaster read write methods
        /// <summary>
        /// Writes the constants data to the file in order to be retrieved later
        /// </summary>
        /// <param name="data">The constants to write to file</param>
        public static void WriteConstants(Hashtable data)
        {
            UpdateAndWrite(CONSTANTSKEY, data);
        }

        public static Hashtable ReadConstants()
        {
            // Read the data and return it.
            Hashtable readData = ((JObject)ReadData()[CONSTANTSKEY])?.ToObject<Hashtable>();
            return readData;
        }
        #endregion

        #region MiscData read write methods
        /// <summary>
        /// Sets the misc data at the given key to the value of the given object.
        /// </summary>
        /// <param name="key">The key for the data in the misc hashtable</param>
        /// <param name="data">The data to write at the key location.</param>
        public static void AppendMiscData(string key, object data)
        {
            Hashtable currentMiscData = ReadMiscData();

            if (currentMiscData == null)
            {
                currentMiscData = new Hashtable();
            }

            currentMiscData[key] = data; ;

            UpdateAndWrite(MISCKEY, currentMiscData);
        }
        /// <summary>
        /// Read all misc data and return the hashtable.
        /// </summary>
        /// <returns>The hashtable for the misc data.</returns>
        private static Hashtable ReadMiscData()
        {
            return ((JObject)ReadData()[MISCKEY])?.ToObject<Hashtable>(); 
        }
        /// <summary>
        /// Reads the misc data located at the specified key and returns it. Null if it doesn't exist.
        /// </summary>
        /// <param name="key">The key location of the data to read</param>
        /// <returns>The object at the given key</returns>
        public static object ReadMiscData(string key)
        {
            Hashtable readData = ReadMiscData();
            return (readData != null && readData.ContainsKey(key)) ? readData[key] : null;
        }
        #endregion
    }
}
