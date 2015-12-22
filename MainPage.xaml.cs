using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RoombaRPiWinGamepad
{
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Private variables
        /// </summary>
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        Roomba roomba = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        public MainPage()
        {
            this.InitializeComponent();
            comPortInput.IsEnabled = false;
            sendTextButton.IsEnabled = false;
            listOfDevices = new ObservableCollection<DeviceInformation>();
            ListAvailablePorts();

            GetModeAndStartup();

        }

        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async void ListAvailablePorts()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                status.Text = "Select a device and connect";

                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                }

                DeviceListSource.Source = listOfDevices;
                comPortInput.IsEnabled = true;
                if (listOfDevices.Count == 1)
                {

                    ConnectDevices.SelectedIndex = 0;
                    comPortInput_Click(null, null);
                }

                else
                    ConnectDevices.SelectedIndex = -1;

            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Add text to rcvdText textbox to invoke rcvdText_TextChanged event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                status.Text = "Select a device and connect";
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];

            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 115200; //57600; // //9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;

                // Display configured settings
                status.Text = "Serial port configured successfully!\n ----- Properties ----- \n";
                status.Text += "BaudRate: " + serialPort.BaudRate.ToString() + "\n";
                status.Text += "DataBits: " + serialPort.DataBits.ToString() + "\n";
                status.Text += "Handshake: " + serialPort.Handshake.ToString() + "\n";
                status.Text += "Parity: " + serialPort.Parity.ToString() + "\n";
                status.Text += "StopBits: " + serialPort.StopBits.ToString() + "\n";

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                rcvdText.Text = "Waiting for data...";

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;
                roomba = new Roomba(serialPort);

                await roomba.SendStartCommand();
                //sendTextButton_Click(null, null); //send 

                await roomba.SendBeep();
                
                rcvdText.Text+= "\nInit done";
                Debug.WriteLine("Init done");

            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Read the current running mode (controller host name) from local config file.
        /// Initialize accordingly
        /// </summary>
        public async void GetModeAndStartup()
        {
            
            Controllers.XboxJoystickInit();
            DispatcherTimerSetup();

        }

        /// <summary>
        /// sendTextButton_Click: Action to take when 'WRITE' button is clicked
        /// - Create a DataWriter object with the OutputStream of the SerialDevice
        /// - Create an async task that performs the write operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void sendTextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (roomba != null && roomba.SerialPort != null && !roomba.IsBusy)
                {
                    byte[] bytesToSend = null;
                    switch (sendText.Text)
                    {
                        case "p": bytesToSend = new byte[] { 128 }; break;
                        case "s": await roomba.SendStartCommand(); break;// new byte[] { 131 }; break;
                        case "beep": bytesToSend = new byte[] { 140, 3, 1, 64, 16, 141, 3 }; break;

                        case "play": await roomba.PlayNotes(); return;

                    }

                    status.Text = await roomba.WriteAsync(bytesToSend);

                }
                else
                {
                    status.Text = "Roomba not connected or not ready";
                }
            }
            catch (Exception ex)
            {
                status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        ///// <summary>
        ///// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        ///// </summary>
        ///// <returns></returns>
        //private async Task WriteAsync(byte[] bytesToSend = null)
        //{
        //    Task<UInt32> storeAsyncTask;
        //    if (sendText.Text.Length != 0)
        //    {

        //        //else
        //        //{
        //        //    bytesToSend = sendText.Text.ToCharArray();
        //        //}
        //        // Load the text from the sendText input text box to the dataWriter object
        //        dataWriteObject.WriteBytes(bytesToSend); //.WriteString(sendText.Text);                

        //        // Launch an async task to complete the write operation
        //        storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

        //        UInt32 bytesWritten = await storeAsyncTask;
        //        if (bytesWritten > 0)
        //        {                    
        //            status.Text = sendText.Text + '\n';
        //            status.Text += "Bytes written successfully!";
        //        }
        //        sendText.Text = "";
        //    }
        //    else
        //    {
        //        status.Text = "Enter the text you want to write and then click on 'WRITE'";
        //    }
        //}

        /// <summary>
        /// rcvdText_TextChanged: Action to take when text is entered in the 'Read Data' textbox
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void rcvdText_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);
                    await ReadAsync(ReadCancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    Debug.WriteLine(status.Text);

                    CloseDevice();
                }
                else
                {
                    status.Text = ex.Message;
                    Debug.WriteLine(ex.Message);

                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                rcvdText.Text = dataReaderObject.ReadString(bytesRead);
                Debug.WriteLine(rcvdText.Text);

                status.Text = "\nBytes read successfully!";
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            comPortInput.IsEnabled = true;
            sendTextButton.IsEnabled = false;
            rcvdText.Text = "";
            listOfDevices.Clear();
        }

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "";
                CancelReadTask();
                CloseDevice();
                ListAvailablePorts();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        DispatcherTimer dispatcherTimer;
        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(40);
            dispatcherTimer.Start();
        }


        void dispatcherTimer_Tick(object sender, object e)
        {
            //MoveRoombaWheels();
            MoveRoomba();
        }

        int VELOCITYCHANGE = 200;
        int ROTATIONCHANGE = 300;

        private async void MoveRoomba()
        {
            if (roomba != null && !roomba.IsBusy)
            {
                //controls.Update();
                int velocity = (Controllers.Magnitude);//.LeftStick.Position.Y);
                switch (Controllers.Direction)
                {
                    case ControllerDirection.Down: await roomba.Drive(-1, 0); break;
                    case ControllerDirection.Up: await roomba.Drive(1, 0); break;
                    case ControllerDirection.Right: await roomba.Drive(1, -1); break;
                    case ControllerDirection.Left: await roomba.Drive(1, 1); break;
                    default: await roomba.Drive(0, 0); break;

                }
            }
        }

        private ControllerDirection lastDirection;
        private async void MoveRoombaWheels()
        {
            if (roomba != null && !roomba.IsBusy)
            {
                //if (Controllers.Direction != lastDirection)
                //{
                    //controls.Update();
                    int velocity = (Controllers.Magnitude/5);//.LeftStick.Position.Y);
                    switch (Controllers.Direction)
                    {
                        case ControllerDirection.Up: await roomba.DriveWheels(velocity, velocity); break;

                        case ControllerDirection.UpLeft: await roomba.DriveWheels(velocity / 2, velocity); break;
                        case ControllerDirection.UpRight: await roomba.DriveWheels(velocity, velocity / 2); break;

                        case ControllerDirection.Left: await roomba.DriveWheels(-velocity, velocity); break;
                        case ControllerDirection.Right: await roomba.DriveWheels(velocity, -velocity); break;

                        case ControllerDirection.DownLeft: await roomba.DriveWheels(-velocity, -velocity / 2); break;
                        case ControllerDirection.DownRight: await roomba.DriveWheels(-velocity / 2, -velocity); break;

                        case ControllerDirection.Down: await roomba.DriveWheels(-velocity, -velocity); break;

                        default: await roomba.DriveWheels(0, 0); break;

                    }
                    //lastDirection = Controllers.Direction;
                    //txtVelocity.Text = velocity.ToString();
               // }

               
            }
        }

        private async void sendTextButton_Copy_Click(object sender, RoutedEventArgs e)
        {
            await roomba.SendStartCommand();
            await roomba.WriteAsync(new byte[] { 131 });
            await roomba.SendBeep();
        }

        private async void btnLights_Click(object sender, RoutedEventArgs e)
        {
            await roomba.WriteAsync(new byte[] { 139,8,0,128 });
        }
    }
}
