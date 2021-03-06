﻿using Dashboard.net.DataHandlers;
using Dashboard.net.Element_Controllers;
using Dashboard.net.RobotLogging;
using System;
using System.Collections.ObjectModel;

namespace Dashboard.net
{
    public class Master
    {
        public event EventHandler<MainWindow> MainWindowSet;

        /// <summary>
        /// The current instance of the master class
        /// </summary>
        public static Master currentInstance;

        private MainWindow masterWindow;
        public MainWindow _MainWindow
        {
            get
            {
                return masterWindow;
            }
            set
            {
                masterWindow = value;
                MainWindowSet?.Invoke(this, masterWindow);
            }
        }

        public ConstantMaster Constants { get; private set; }
        public NTInterface _Dashboard_NT { get; private set; }
        public AutonomousSelector _AutoSelector { get; private set; }
        public Timer _Timer { get; private set; }
        public Accelerometer _Accelerometer { get; private set; }
        public Lift _Lift { get; private set; }
        public Element_Controllers.Camera _Camera { get; private set; }
        public NetworkMonitor _Monitor { get; private set; }
        public ConnectionUI _ConnectionUI { get; private set; }
        public Cautioner _Cautioner { get; private set; }
        public Element_Controllers.Checklist ChecklistHandler { get; private set; }
        public MiscOperations _MiscOperations { get; private set; }

        /// <summary>
        /// The interface that will be logging the logs from the robot.
        /// </summary>
        public RobotLogInterface RobotLogger {get; private set;}

        public Master()
        {
            currentInstance = this;

            // Make constants first so that all the others have access to it right away
            Constants = new ConstantMaster();

            _Dashboard_NT = new NTInterface(this);
            _AutoSelector = new AutonomousSelector(this);
            _Timer = new Timer(this);
            _Accelerometer = new Accelerometer(this);
            _Lift = new Lift(this);
            _Monitor = new NetworkMonitor(this);
            _Camera = new Element_Controllers.Camera(this);
            _ConnectionUI = new ConnectionUI(this);
            _Cautioner = new Cautioner(this);
            ChecklistHandler = new Element_Controllers.Checklist(this);
            _MiscOperations = new MiscOperations(this);

            RobotLogger = new RobotLogInterface();
        }
    }
}
