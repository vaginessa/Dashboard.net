﻿using NetworkTables;
using NetworkTables.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Dashboard.net.Element_Controllers
{
    public class SmartDashboard : Controller, INotifyPropertyChanged
    {
        public string ConnectedAddress { get; private set; }


        public RelayCommand OnConnect { get; private set; } = new RelayCommand
        {
            CanExecuteDeterminer = () => true
        };

        public event EventHandler<bool> ConnectionEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        private Dispatcher mainDispatcher;

        public bool IsConnected
        {
            get
            {
                return (_SmartDashboard != null && _SmartDashboard.IsConnected);
            }
        }

        public string StatusMessage
        {
            get
            {
                if (IsConnected) return "CONNECTED";
                else return "OFFLINE";
            }
        }

        /// <summary>
        /// The actual SmartDashboard object for getting, setting and dealing with values
        /// </summary>
        private NetworkTable __SmartDashboard;
        public NetworkTable _SmartDashboard
        {
            get
            {
                return __SmartDashboard;
            }
            set
            {
                __SmartDashboard = value;
                __SmartDashboard.AddConnectionListener(OnConnectionEvent, true);
                _SmartDashboard.AddTableListener(OnTableValuesChanged);
            }
        }

        public SmartDashboard(Master controller) : base(controller)
        {
            OnConnect.FunctionToExecute = OnConnectClick;
            master.MainWindowSet += OnMainWindowSet;
        }

        public async Task<bool> Connect()
        {
            NetworkTable.SetPort(1735);
            NetworkTable.SetIPAddress(ConnectedAddress);
            NetworkTable.SetClientMode();
            NetworkTable.Initialize();

            _SmartDashboard = NetworkTable.GetTable("SmartDashboard");

            // TODO do the connection stuff in a different thread.
            while (!_SmartDashboard.IsConnected) { }

            await Task.Delay(0);

            return true;

        }

        private Dictionary<string, Action<Value>> ListenerFunctions 
            = new Dictionary<string, Action<Value>>();
        /// <summary>
        /// Function that listens for the given key to change and then calls
        /// the given function when it changes.
        /// </summary>
        /// <param name="key"></param>
        public void AddKeyListener(string key, Action<Value> functionToExecute)
        {
            ListenerFunctions.Add(key, functionToExecute);
        }


        #region Event Listeners
        /// <summary>
        /// Listens for changes in the SmartDashboard table and then calls the function listening.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="arg4"></param>
        private void OnTableValuesChanged(ITable table, string key, Value value, NotifyFlags arg4)
        {
            if (!ListenerFunctions.ContainsKey(key)) return;
            mainDispatcher.Invoke(() => NTValueChanged(key, value));
        }

        /// <summary>
        /// Fired when a networktables key changes.
        /// </summary>
        /// <param name="key">The key that changed</param>
        /// <param name="value">The new value for that key</param>
        private void NTValueChanged(string key, Value value)
        {
            ListenerFunctions[key](value);
        }


        public async void OnConnectClick(object connectAddress)
        {
            ConnectedAddress = connectAddress.ToString();
            bool connected = await Task.Run(Connect);

            if (!connected) return;

            ConnectionEvent?.Invoke(this, true);
            PropertyChanged(this, new PropertyChangedEventArgs("IsConnected"));
            PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
        }

        /// <summary>
        /// Fired on connect and disconnect events.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        private void OnConnectionEvent(IRemote arg1, ConnectionInfo arg2, bool arg3)
        {
            //ConnectionEvent?.Invoke(this, IsConnected);
            // TODO make this work
        }

        protected override void OnMainWindowSet(object sender, EventArgs e)
        {
            mainDispatcher = master._MainWindow.Dispatcher;
        }

        #endregion
    }
}