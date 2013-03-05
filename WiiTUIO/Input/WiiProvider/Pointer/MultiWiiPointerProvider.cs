﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using WiimoteLib;
using System.Runtime.InteropServices;
using System.Drawing;
using WindowsInput;
using WiiTUIO.Properties;
using System.Windows.Controls;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// The WiiProvider implements <see cref="IProvider"/> in order to offer a type of object which uses the Wiimote to generate new event frames.
    /// </summary>
    public class MultiWiiPointerProvider : IProvider
    {
        private double WIIMOTE_DISCONNECT_THRESHOLD = 2000; //If we haven't recieved input from a wiimote in 2 seconds we consider it disconnected.
        private ulong OLD_FRAME_THRESHOLD = 200; //Timeout for a previous frame from a Wiimote to be considered old, so we wont enable it when getting input from other wiimotes.

        private Mutex pDeviceMutex = new Mutex();

        private Thread wiimoteConnectorThread;

        private Dictionary<string, WiimoteControl> pWiimoteMap = new Dictionary<string, WiimoteControl>();

        private WiimoteCollection pWC;

        private UserControl settingsControl = null;

        private EventHandler<WiimoteChangedEventArgs> wiimoteChangedEventHandler;
        private EventHandler<WiimoteExtensionChangedEventArgs> wiimoteExtensionChangedEventHandler;

        #region Properties and Constructor
        /// <summary>
        /// Boolean which indicates if we are generating input or not.
        /// </summary>
        private bool bRunning = false;

        /// <summary>
        /// An input classifier which we will use to organise points.
        /// </summary>
        public SpatioTemporalClassifier InputClassifier { get; protected set; }

        /// <summary>
        /// A property to determine if this input provider is running (and thus generating events).
        /// </summary>
        public bool IsRunning { get { return this.bRunning; } }

        /// <summary>
        /// This defines an event which is raised when a new frame of touch events is prepared and ready to be dispatched by this provider.
        /// </summary>
        public event EventHandler<FrameEventArgs> OnNewFrame;

        #region Battery State
        /// <summary>
        /// An event which is fired when the battery state changes.
        /// </summary>
        public event Action<int> OnBatteryUpdate;

        public event Action<int,int> OnConnect;
        public event Action<int,int> OnDisconnect;

        /// <summary>
        /// The internal battery state.
        /// </summary>
        private int iBatteryState = 0;

        /// <summary>
        /// Get the current battery state.
        /// </summary>
        public int BatteryState
        {
            get
            {
                return iBatteryState;
            }
            protected set
            {
                if (value != iBatteryState)
                {
                    iBatteryState = value;
                    if (OnBatteryUpdate != null)
                        OnBatteryUpdate(iBatteryState);
                }
            }
        }
        #endregion

        /// <summary>
        /// Construct a new wiimote provider.
        /// </summary>
        public MultiWiiPointerProvider()
        {

            this.settingsControl = new WiiPointerProviderSettings();

            this.pWC = new WiimoteCollection();

            this.wiimoteChangedEventHandler = new EventHandler<WiimoteChangedEventArgs>(handleWiimoteChanged);
            this.wiimoteExtensionChangedEventHandler = new EventHandler<WiimoteExtensionChangedEventArgs>(handleWiimoteExtensionChanged);


            /*
            this.mouseMode = this.keyMapper.KeyMap.Pointer.ToLower() == "mouse";
            this.showPointer = Settings.Default.pointer_moveCursor;
            if (this.showPointer && !this.mouseMode)
            {
                this.duoTouch.enableHover();
            }
            else
            {
                this.duoTouch.disableHover();
            }
            */
        }

        #endregion

        #region Start and Stop

        /// <summary>
        /// Instructs this input provider to begin generating events.
        /// </summary>
        public void start()
        {
            wiimoteConnectorThread = new Thread(new ThreadStart(wiimoteConnectorThreadWorker));
            wiimoteConnectorThread.Start();

            // Set the running flag.
            this.bRunning = true;

        }

        private void wiimoteConnectorThreadWorker()
        {
            Exception pError;
            while (this.bRunning)
            {
                if (!this.initialiseWiimoteConnections(out pError))
                {
                    Console.WriteLine("Could not establish connection to a Wiimote: " + pError.Message, pError);
                }
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Instructs this input provider to stop generating events.
        /// </summary>
        public void stop()
        {
            // Set the running flag.
            this.bRunning = false;

            /*
            try
            {
                this.wiimoteConnectorThread.Abort();
            }
            catch { }
            */

            this.teardownWiimoteConnections();

            this.pWC.Clear();

        }
        #endregion

        #region Connection creation and teardown.
        /// <summary>  
        /// This method creates and sets up our connection to our class-gloal Wiimote device.
        /// This destroys any existing connection before creating a new one.
        /// </summary>
        /// <param name="pErrorReport">A reference to an exception which we want to contain our error if one happened.</param>
        private bool initialiseWiimoteConnections(out Exception pErrorReport)
        {
            // If we have an existing device, teardown the connection.
            //this.teardownWiimoteConnection();

            pErrorReport = null;

            this.pWC.Clear();
            this.pWC.FindAllWiimotes();

            foreach (Wiimote pDevice in pWC)
            {
                try
                {
                    if (!pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                    {
                        Console.WriteLine("Trying to connect " + pDevice.HIDDevicePath);
                        // Try to establish a connection, enable the IR reader and flag some LEDs.
                        pDevice.Connect();
                        pDevice.SetReportType(InputReport.IRAccel, true);

                        pDevice.SetRumble(true);

                        Thread stopRumbleThread = new Thread(stopRumble);
                        stopRumbleThread.Start(pDevice);

                        int id = this.getFirstFreeId();
                        pDevice.SetLEDs((id - 1) % 4 + 1);

                        WiimoteControl control = new WiimoteControl(id,pDevice);

                        pDeviceMutex.WaitOne(); //Don't mess with the list of wiimotes if it is enumerating in an update
                        pWiimoteMap[pDevice.HIDDevicePath] = control;
                        pDeviceMutex.ReleaseMutex();

                        // Hook up device event handlers.
                        pDevice.WiimoteChanged += this.wiimoteChangedEventHandler;
                        pDevice.WiimoteExtensionChanged += this.wiimoteExtensionChangedEventHandler;

                        OnConnect(id, this.pWiimoteMap.Count);
                    }
                    else if (pWiimoteMap[pDevice.HIDDevicePath].LastWiimoteEventTime != null && DateTime.Now.Subtract(pWiimoteMap[pDevice.HIDDevicePath].LastWiimoteEventTime).TotalMilliseconds > WIIMOTE_DISCONNECT_THRESHOLD)
                    {
                        Console.WriteLine("Teardown " + pDevice.HIDDevicePath + " because of timeout with delta " + DateTime.Now.Subtract(pWiimoteMap[pDevice.HIDDevicePath].LastWiimoteEventTime).TotalMilliseconds);
                        teardownWiimoteConnection(pWiimoteMap[pDevice.HIDDevicePath].Wiimote);
                    }
                }
                // If something went wrong - notify the user..
                catch (Exception pError)
                {
                    // Ensure we are ok.
                    try
                    {
                        Console.WriteLine("Teardown "+ pDevice.HIDDevicePath +" because of " + pError.Message);
                        this.teardownWiimoteConnection(pDevice);
                    }
                    finally { }
                    // Say we screwed up.
                    pErrorReport = pError;
                    //throw new Exception("Error establishing connection: " + , pError);
                    
                }
                
            }
            if(pErrorReport != null)
            {
                return false;
            }
            
            return true;
        }

        private int getFirstFreeId()
        {
            HashSet<int> usedIDs = new HashSet<int>();
            foreach (WiimoteControl control in pWiimoteMap.Values)
            {
                usedIDs.Add(control.ID);
            }

            int id = 1;
            while (usedIDs.Contains(id))
            {
                id++;
            }
            return id;
        }

        /// <summary>
        /// This method destroys our connection to our class-global Wiimote device.
        /// </summary>
        private void teardownWiimoteConnections()
        {
            if (pWiimoteMap.Count > 0)
            {
                IEnumerable<WiimoteControl> controls = new Queue<WiimoteControl>(pWiimoteMap.Values);
                foreach (WiimoteControl control in controls)
                {
                    teardownWiimoteConnection(control.Wiimote);
                }
            }
        }

        private void teardownWiimoteConnection(Wiimote pDevice)
        {
            if (pDevice != null)
            {
                pDeviceMutex.WaitOne();
                pDevice.WiimoteChanged -= this.wiimoteChangedEventHandler;
                pDevice.WiimoteExtensionChanged -= this.wiimoteExtensionChangedEventHandler;
                int wiimoteid;
                if (pWiimoteMap.Keys.Contains(pDevice.HIDDevicePath))
                {
                    wiimoteid = this.pWiimoteMap[pDevice.HIDDevicePath].ID;
                    this.pWiimoteMap.Remove(pDevice.HIDDevicePath);
                }
                else
                {
                    wiimoteid = this.pWiimoteMap.Count + 1;
                }
                pDeviceMutex.ReleaseMutex();

                pDevice.SetRumble(false);

                // Close the connection and dispose of the device.
                pDevice.Disconnect();
                pDevice.Dispose();

                OnDisconnect(wiimoteid, this.pWiimoteMap.Count);
            }
        }
        #endregion

        private void stopRumble(Object device)
        {
            Console.WriteLine("Starting stopRumble thread");
            Thread.Sleep(80);
            Wiimote pDevice = (Wiimote)device;
            bool rumbleStatus = true;
            int retries = 0;
            while (rumbleStatus && retries < 100) //Sometimes the Wiimote does not disable the rumble on the first try
            {
                try {
                    if (pDevice != null)
                    {
                        pDevice.SetRumble(false);
                        Thread.Sleep(30);
                        rumbleStatus = pDevice.WiimoteState.Rumble;
                        Console.WriteLine("Rumble status for " + pDevice.HIDDevicePath + " is " + rumbleStatus);
                    }
                } catch {
                    rumbleStatus = true;
                }
                retries++;
            }
        }

        #region Wiimote Event Handlers
        /// <summary>
        /// This is called when an extension is attached or unplugged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            // Check we have a valid device.
            //if (this.pDevice == null)
            //    return;
            /*
            // If an extension is attached at runtime we want to enable it.
            if (e.Inserted)
                this.pDevice.SetReportType(InputReport.IRExtensionAccel, true);
            else
                this.pDevice.SetReportType(InputReport.IRAccel, true);
             * */
        }

        /// <summary>
        /// This is called when the state of the wiimote changes and a new state report is available.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            if (bRunning)
            {
                pDeviceMutex.WaitOne();
                if (pWiimoteMap.Keys.Contains(((Wiimote)sender).HIDDevicePath))
                {
                    WiimoteControl senderControl = pWiimoteMap[((Wiimote)sender).HIDDevicePath];

                    senderControl.handleWiimoteChanged(sender, e);

                    if (senderControl.FrameQueue.Count > 0)
                    {
                        FrameEventArgs senderFrame = senderControl.FrameQueue.Dequeue();

                        Queue<WiiContact> allContacts = new Queue<WiiContact>(senderFrame.Contacts);

                        foreach (WiimoteControl control in pWiimoteMap.Values) //Asynchronusly include contacts for all connected Wiimotes, send only hover and move events.
                        {
                            if (control != senderControl)
                            {
                                FrameEventArgs lastFrame = control.LastFrameEvent;
                                if (lastFrame != null)
                                {
                                    ulong timeDelta = ((ulong)Stopwatch.GetTimestamp() / 10000) - (lastFrame.Timestamp / 10000);
                                    if (timeDelta < OLD_FRAME_THRESHOLD) //Happens when the pointer is out of reach
                                    {
                                        IEnumerable<WiiContact> contacts = lastFrame.Contacts;
                                        foreach (WiiContact contact in contacts)
                                        {
                                            if (contact.Type == ContactType.EndToHover)
                                            {
                                                WiiContact newContact = new WiiContact(contact.ID, ContactType.Hover, contact.Position, new Vector(Util.ScreenWidth, Util.ScreenHeight));
                                                allContacts.Enqueue(newContact);
                                            }
                                            else if (contact.Type == ContactType.Start)
                                            {
                                                WiiContact newContact = new WiiContact(contact.ID, ContactType.Move, contact.Position, new Vector(Util.ScreenWidth, Util.ScreenHeight));
                                                allContacts.Enqueue(newContact);
                                            }
                                            else if (contact.Type == ContactType.End || contact.Type == ContactType.EndFromHover)
                                            {
                                            }
                                            else //contact type was hover or move
                                            {
                                                allContacts.Enqueue(contact);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //Console.WriteLine("Sending " + allContacts.Count + " contacts");
                        FrameEventArgs newFrame = new FrameEventArgs(senderFrame.Timestamp, allContacts);

                        this.OnNewFrame(this, newFrame);
                    }

                    /*
                    bool handledAll = false;
                    Queue<WiiContact> allContacts = new Queue<WiiContact>(2);
                    ulong timestamp = 0;

                    foreach (WiimoteControl control in pWiimoteMap.Values)
                    {
                        handledAll = control.Handled;
                        if (handledAll == false)
                        {
                            break;
                        }
                    }
                
                    if (handledAll)
                    {
                        foreach (WiimoteControl control in pWiimoteMap.Values)
                        {
                            if (control.FrameQueue.Count > 0)
                            {
                                FrameEventArgs lastFrameEvent = control.FrameQueue.Dequeue();
                                while (control.FrameQueue.Count > 0)
                                {

                                    //Fast forward events which are only consisting of Move or Hover events
                                    bool importantContact = false;
                                    foreach (WiiContact contact in lastFrameEvent.Contacts)
                                    {
                                        if (contact.Type != ContactType.Hover && contact.Type != ContactType.Move)
                                        {
                                            importantContact = true;
                                            break;
                                        }
                                    }

                                    //If the next event contains an End event this one is important too, because End events need to be at the same position as the last one.
                                    FrameEventArgs nextFrameEvent = control.FrameQueue.Peek();
                                    foreach (WiiContact contact in nextFrameEvent.Contacts)
                                    {
                                        if (contact.Type == ContactType.End || contact.Type == ContactType.EndFromHover || contact.Type == ContactType.EndToHover)
                                        {
                                            importantContact = true;
                                            break;
                                        }
                                    }

                                    if (importantContact)
                                    {
                                        break;
                                    }

                                    lastFrameEvent = control.FrameQueue.Dequeue();
                                }

                                timestamp = timestamp < lastFrameEvent.Timestamp ? timestamp : lastFrameEvent.Timestamp;

                                foreach (WiiContact contact in lastFrameEvent.Contacts)
                                {
                                    allContacts.Enqueue(contact);
                                }
                            }
                            control.Handled = false;
                        }
                        if (allContacts.Count > 0)
                        {
                            FrameEventArgs newFrame = new FrameEventArgs(timestamp, allContacts);

                            this.OnNewFrame(this, newFrame);
                        }
                    }
                 * */
                }
                pDeviceMutex.ReleaseMutex();
            }
        }
        #endregion

        public UserControl getSettingsControl()
        {
            return this.settingsControl;
        }
    }
}