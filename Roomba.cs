using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace RoombaRPiWinGamepad
{
    class Roomba
    {
        public enum RoombaOpCode : byte
        {
            INVALID = 0,
            START = 128,    //0x80
            BAUD = 129,     //0x81
            CONTROL = 130,  //0x82
            SAFE = 131,     //0x83
            FULL = 132,     //0x84
            POWER = 133,    //0x85
            SPOT = 134,     //0x86
            CLEAN = 135,    //0x87
            MAX = 136,      //0x88
            DRIVE = 137,    //0x89
            MOTORS = 138,   //0x8A
            LEDS = 139,     //0x8B
            SONG = 140,     //0x8C
            PLAY = 141,     //0x8D
            SENSORS = 142,  //0x8E
            DOCK = 143,      //0x8F
            DRIVEDIRECT = 145
        }

        public SerialDevice SerialPort { get; set; }
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;
        public bool IsBusy
        {
            get; set;
        }

        public Roomba(SerialDevice serialPort)
        {
            this.SerialPort = serialPort;
        }


        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        public async Task<string> WriteAsync(byte[] bytesToSend = null)
        {
            string status = "";
            if (IsBusy) return "BUSY";
            try
            {
                if (SerialPort != null)
                {
                    IsBusy = true;
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(SerialPort.OutputStream);
                    //Launch the WriteAsync task to perform the write
                    dataWriteObject.WriteBytes(bytesToSend); //.WriteString(sendText.Text);                
                    UInt32 bytesWritten = await dataWriteObject.StoreAsync().AsTask();
                    if (bytesWritten > 0)
                    {
                        status = string.Format("{0} byte(s) written successfully!", bytesWritten);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;
                //throw; //status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
                IsBusy = false;
            }
            return status;
        }

        //private void SendData_Click(object sender, EventArgs e)
        //{
        //        SerialPort.Write(new byte[] { (byte)RoombaOpCode.START }, 0, 1);
        //        SerialPort.BaseStream.Flush();
        //        SerialPort.Write(new byte[] { (byte)RoombaOpCode.CONTROL }, 0, 1);
        //        SerialPort.BaseStream.Flush();

        //        System.Threading.Thread.Sleep(20);

        //        switch (DataOut.Text)
        //        {
        //            case "Play Notes":
        //                LogMessage("Playing Notes");
        //                PlayNotes();
        //                break;
        //            case "Drive":
        //                LogMessage("Driving");
        //                Drive(1, 0);
        //                break;
        //            case "Turn Left":
        //                LogMessage("Turning Left");
        //                Drive(1, 1);
        //                break;
        //            case "Turn Right":
        //                LogMessage("Turning Right");
        //                Drive(1, -1);
        //                break;
        //            case "Stop":
        //                LogMessage("Stopping");
        //                Drive(0, 0);
        //                break;
        //            default:
        //                break;
        //        }
        //}


            
        public async Task PlayNotes()
        {
            for (int i = 31; i <= 127; i++)
            {
                await PlayNote(i);
                // Execution of the async method will continue one second later, but without blocking.
                await Task.Delay(20);
                //Thread.Sleep(20);
            }
        }

        public async Task PlayNote(int note)
        {
            byte[] cmd = { (byte)RoombaOpCode.SONG, 3, 1, (byte)note, (byte)10, (byte)RoombaOpCode.PLAY, 3 };
            await WriteAsync(cmd);// SerialPort.Write(cmd, 0, cmd.Length);
        }

        public async Task Drive(int velocity, int radius)
        {
            byte[] cmd = { (byte)RoombaOpCode.DRIVE, (byte)velocity, (byte)(velocity & 0xff), (byte)radius, (byte)(radius & 0xff) };
            await WriteAsync(cmd); // SerialPort.Write(cmd, 0, cmd.Length);
        }


        public async Task DriveWheels(int leftWheelVelocity, int rightWheelVelocity)
        {
            byte[] cmd = { (byte)RoombaOpCode.DRIVEDIRECT, (byte)rightWheelVelocity, (byte)(rightWheelVelocity & 0xff), (byte)leftWheelVelocity, (byte)(leftWheelVelocity & 0xff) };
            await WriteAsync(cmd); // SerialPort.Write(cmd, 0, cmd.Length);
        }


        public async Task SendStartCommand()
        {
            await WriteAsync(new byte[] { (byte)RoombaOpCode.START });

            await WriteAsync(new byte[] { (byte)RoombaOpCode.CONTROL });

            await Task.Delay(20);

            //        SerialPort.BaseStream.Flush();
            //        SerialPort.Write(new byte[] { (byte)RoombaOpCode.CONTROL }, 0, 1);
            //        SerialPort.BaseStream.Flush();

            //        System.Threading.Thread.Sleep(20);
        }

        public async Task SendBeep()
        {
            var bytesToSend = new byte[] { 140, 3, 1, 64, 16, 141, 3 };
            await WriteAsync(bytesToSend);

            await Task.Delay(20);

        }
    }
}
