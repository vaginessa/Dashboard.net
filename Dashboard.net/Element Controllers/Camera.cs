﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dashboard.net.Camera;
using MjpegProcessor;
using NetworkTables;

namespace Dashboard.net.Element_Controllers
{
    /// <summary>
    /// Class that controls the camera feed and displays it on the dashboard.
    /// Documentation for the module used: https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder
    /// </summary>
    public class Camera : Controller
    {

        private readonly string CAMERAPATH = "CameraPublisher", URLPATH = "streams", PROPERTYPATH = "Property";
        public ObservableCollection<string> AvailableCameras { get; private set; } = new ObservableCollection<string>();

        public RelayCommand OpenSettingsCommand { get; private set; }

        private MjpegDecoder camera;
        private Image display;
        private ComboBox cameraSelector;

        private bool isStreaming;
        /// <summary>
        /// Whether or not the camera is currently streaming the image from the robot.
        /// </summary>
        public bool IsStreaming
        {
            get
            {
                return isStreaming;
            }
            set
            {
                isStreaming = value;
                // Tell the command to open settings to refresh itself.
                OpenSettingsCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// The streaming URL of the selected camera
        /// </summary>
        private string CameraURL
        {
            get
            {
                return GetCameraURL(SelectedCamera);
            }
        }

        /// <summary>
        /// The string url of the camera settings
        /// </summary>
        private string CameraSettingsURL
        {
            get
            {
                string url = CameraURL;
                return (!string.IsNullOrEmpty(url)) ? url.Substring(0, url.LastIndexOf('/')) : null;
            }
        }

        /// <summary>
        /// The string name of the selected camera
        /// </summary>
        private string SelectedCamera
        {
            get
            {
                return cameraSelector.SelectedItem?.ToString();
            }
        }

        private string CameraPropertyPath
        {
            get
            {
                return string.Format("{0}/{1}/{2}", CAMERAPATH, SelectedCamera, PROPERTYPATH);
            }
        }

        /// <summary>
        /// The relaycommand that shows the feed in a new window.
        /// </summary>
        public RelayCommand OpenNewWindow { get; private set; }

        /// <summary>
        /// The other window with the camera feed in it.
        /// </summary>
        private CameraNewWindow OtherWindow { get; set; }
        private bool IsShowingOtherWindow
        {
            get
            {
                return OtherWindow != null && OtherWindow.IsActive; // TODO also check that the window isn't closed.
            }
        }

        public Camera(Master controller) : base(controller)
        {
            master.MainWindowSet += Master_MainWindowSet;

            camera = new MjpegDecoder();
            camera.FrameReady += Camera_FrameReady;
            camera.Error += Camera_Error;

            master._Dashboard_NT.ConnectionEvent += _Dashboard_NT_ConnectionEvent;

            OpenNewWindow = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = ShowInNewWindow
            };

            OpenSettingsCommand = new RelayCommand()
            {
                CanExecuteDeterminer = () => true,
                FunctionToExecute = (object parameter) => OpenCameraSettings()
            };
        }

        /// <summary>
        /// Handles errors with the camera by restarting the stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Camera_Error(object sender, ErrorEventArgs e)
        {
            IsStreaming = false;
            StartCamera();
        }

        private void _Dashboard_NT_ConnectionEvent(object sender, bool connected)
        {
            // When we connect, begin receiving the stream.
            if (connected)
            {
                // Get the available cameras
                AvailableCameras.Clear();
                ObservableCollection<string> cameras =  GetAvailableCameras();
                
                foreach (string camera in cameras)
                {
                    AvailableCameras.Add(camera);
                }
                
                // If we only have one camera, select it.
                if (AvailableCameras.Count == 1) cameraSelector.SelectedIndex = 0;
            }
            // If not connected, disconnect the camera
            else
            {
                AvailableCameras.Clear();
                StopCamera();
            }
        }

        /// <summary>
        /// Gets a list of the available cameras.
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<string> GetAvailableCameras()
        {
            ObservableCollection<string> cameras;

            // Get the sub tables in the camera table
            cameras = new ObservableCollection<string>(master._Dashboard_NT.GetSubTables(CAMERAPATH));

            return cameras;
        }

        private void Master_MainWindowSet(object sender, EventArgs e)
        {
            display = master._MainWindow.CameraBox;
            cameraSelector = master._MainWindow.CameraSelector;

            cameraSelector.SelectionChanged += CameraSelector_SelectionChanged;
        }

        private void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartCamera();
        }

        private string GetCameraURL(string selection)
        {
            if (string.IsNullOrEmpty(selection)) return null;
            // Format url for camera
            string pathToCameraURL = string.Format("{0}/{1}/{2}", CAMERAPATH, selection, URLPATH);

            // Get the property from the networktables
            Value thing = master._Dashboard_NT.GetValue(pathToCameraURL);
            if (thing == null || !NTInterface.IsValidValue(thing)) return "";
            string[] urlThing = thing.GetStringArray();

            // Fix URL
            string url = urlThing[0].Replace("mjpg:", "");
            // Return
            return url;
        }

        /// <summary>
        /// Starts streaming the caemra
        /// </summary>
        /// <param name="url"></param>
        private async void StartCamera()
        {
            // Begin streaming.
            string url = CameraURL;
            if (string.IsNullOrEmpty(url)) return;
            await Task.Run(() => StartCameraAsync(url));
            IsStreaming = true;
        }

        /// <summary>
        /// Stops the camera stream 
        /// </summary>
        private void StopCamera()
        {
            camera.StopStream();
            IsStreaming = false;
        }

        private async Task StartCameraAsync(string url)
        {
            camera.ParseStream(new Uri(url));
            await Task.Delay(0);
        }

        private void Camera_FrameReady(object sender, FrameReadyEventArgs e)
        {
            // Set the camera source.
            display.Source = e.BitmapImage;

            // Also show the image on the other window
            if (IsShowingOtherWindow) OtherWindow.ImageStream = e.BitmapImage;
        }

        /// <summary>
        /// Shows the camera feed in a new window.
        /// </summary>
        /// <param name="parameter"></param>
        private void ShowInNewWindow(object parameter = null)
        {
            // If we're not streaming, don't do anything and simply return.
            BitmapImage image = (IsStreaming) ? camera.BitmapImage : null;
            if (!IsShowingOtherWindow) OtherWindow = new CameraNewWindow(image);
            OtherWindow.Show();
            OtherWindow.Focus();
            OtherWindow.ImageStream = image;
        }

        /// <summary>
        /// Opens the settings page for the current camera.
        /// </summary>
        private void OpenCameraSettings()
        {
            string settingsURL = CameraSettingsURL;
            if (!string.IsNullOrEmpty(settingsURL)) Process.Start(settingsURL);
        }
    }
}