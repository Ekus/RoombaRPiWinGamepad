using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;

namespace RoombaRPiWinGamepad
{

    /// <summary>
    /// **** Controllers Class ****
    /// HID Controller devices - XBox controller
    ///   Data transfer helpers: message parsers, direction to motor value translatores, etc.
    /// </summary>
    public class Controllers
    {
        public static bool FoundLocalControlsWorking = false;

        #region ----- Xbox HID-Controller -----

        private static XboxHidController controller;
        private static int lastControllerCount = 0;
      
        public static ControllerDirection Direction { get; set; }
        public static int Magnitude { get;set;}

        public static async void XboxJoystickInit()
        {
            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);

            if (deviceInformationCollection.Count == 0)
            {
                Debug.WriteLine("No Xbox360 controller found!");
            }
            lastControllerCount = deviceInformationCollection.Count;

            foreach (DeviceInformation d in deviceInformationCollection)
            {
                Debug.WriteLine("Device ID: " + d.Id);

                HidDevice hidDevice = await HidDevice.FromIdAsync(d.Id, Windows.Storage.FileAccessMode.Read);

                if (hidDevice == null)
                {
                    try
                    {
                        var deviceAccessStatus = DeviceAccessInformation.CreateFromId(d.Id).CurrentStatus;

                        if (!deviceAccessStatus.Equals(DeviceAccessStatus.Allowed))
                        {
                            Debug.WriteLine("DeviceAccess: " + deviceAccessStatus.ToString());
                            FoundLocalControlsWorking = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Xbox init - " + e.Message);
                    }

                    Debug.WriteLine("Failed to connect to the controller!");
                }

                controller = new XboxHidController(hidDevice);
                controller.DirectionChanged += Controller_DirectionChanged;
            }
        }



        public static async void XboxJoystickCheck()
        {
            string deviceSelector = HidDevice.GetDeviceSelector(0x01, 0x05);
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelector);
            if (deviceInformationCollection.Count != lastControllerCount)
            {
                lastControllerCount = deviceInformationCollection.Count;
                XboxJoystickInit();
            }
        }

        private static void Controller_DirectionChanged(ControllerVector sender)
        {
            FoundLocalControlsWorking = true;
            Debug.WriteLine("Direction: " + sender.Direction + ", Magnitude: " + sender.Magnitude);

            Direction = sender.Direction;
            Magnitude = sender.Magnitude;


            //XBoxToRobotDirection((sender.Magnitude < 2500) ? ControllerDirection.None : sender.Direction, sender.Magnitude);

            //MotorCtrl.speedValue = sender.Magnitude;
        }

        //static void XBoxToRobotDirection(ControllerDirection dir, int magnitude)
        //{
        //    switch (dir)
        //    {
        //        case ControllerDirection.Down: SetRobotDirection(CtrlCmds.Backward, magnitude); break;
        //        case ControllerDirection.Up: SetRobotDirection(CtrlCmds.Forward, magnitude); break;
        //        case ControllerDirection.Left: SetRobotDirection(CtrlCmds.Left, magnitude); break;
        //        case ControllerDirection.Right: SetRobotDirection(CtrlCmds.Right, magnitude); break;
        //        case ControllerDirection.DownLeft: SetRobotDirection(CtrlCmds.BackLeft, magnitude); break;
        //        case ControllerDirection.DownRight: SetRobotDirection(CtrlCmds.BackRight, magnitude); break;
        //        case ControllerDirection.UpLeft: SetRobotDirection(CtrlCmds.ForwardLeft, magnitude); break;
        //        case ControllerDirection.UpRight: SetRobotDirection(CtrlCmds.ForwardRight, magnitude); break;
        //        default: SetRobotDirection(CtrlCmds.Stop, (int)CtrlSpeeds.Max); break;
        //    }
        //}
        //#endregion

        //#region ----- general command/control helpers -----

        //public enum CtrlCmds { Stop, Forward, Backward, Left, Right, ForwardLeft, ForwardRight, BackLeft, BackRight };
        //public enum CtrlSpeeds { Min = 0, Mid = 5000, Max = 10000 }

        //public static long msLastDirectionTime;
        //public static CtrlCmds lastSetCmd;
        //public static void SetRobotDirection(CtrlCmds cmd, int speed)
        //{

        //    switch (cmd)
        //    {
        //        case CtrlCmds.Forward: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms2; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms1; break;
        //        case CtrlCmds.Backward: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms1; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms2; break;
        //        case CtrlCmds.Left: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms1; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms1; break;
        //        case CtrlCmds.Right: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms2; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms2; break;
        //        case CtrlCmds.ForwardLeft: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.stop; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms1; break;
        //        case CtrlCmds.ForwardRight: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms2; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.stop; break;
        //        case CtrlCmds.BackLeft: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.stop; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.ms2; break;
        //        case CtrlCmds.BackRight: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.ms1; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.stop; break;
        //        default:
        //        case CtrlCmds.Stop: MotorCtrl.waitTimeLeft = MotorCtrl.PulseMs.stop; MotorCtrl.waitTimeRight = MotorCtrl.PulseMs.stop; break;
        //    }
        //    if (speed < (int)CtrlSpeeds.Min) speed = (int)CtrlSpeeds.Min;
        //    if (speed > (int)CtrlSpeeds.Max) speed = (int)CtrlSpeeds.Max;
        //    MotorCtrl.speedValue = speed;

        //    dumpOnDiff(cmd.ToString());

        //    if (!MainPage.isRobot)
        //    {
        //        String sendStr = "[" + (Convert.ToInt32(cmd)).ToString() + "]:" + cmd.ToString();
        //        NetworkCmd.SendCommandToRobot(sendStr);
        //    }
        //    msLastDirectionTime = MainPage.stopwatch.ElapsedMilliseconds;
        //    lastSetCmd = cmd;
        //}

        //private static MotorCtrl.PulseMs lastWTL, lastWTR;
        //private static int lastSpeed;
        //static void dumpOnDiff(String title)
        //{
        //    if ((lastWTR == MotorCtrl.waitTimeRight) && (lastWTL == MotorCtrl.waitTimeLeft) && (lastSpeed == MotorCtrl.speedValue)) return;
        //    Debug.WriteLine("Motors {0}: Left={1}, Right={2}, Speed={3}", title, MotorCtrl.waitTimeLeft, MotorCtrl.waitTimeRight, MotorCtrl.speedValue);
        //    lastWTL = MotorCtrl.waitTimeLeft;
        //    lastWTR = MotorCtrl.waitTimeRight;
        //    lastSpeed = MotorCtrl.speedValue;
        //}

        //public static long msLastMessageInTime;
        //static bool lastHidCheck = false;
        //public static void ParseCtrlMessage(String str)
        //{
        //    char[] delimiterChars = { '[', ']', ':' };
        //    string[] words = str.Split(delimiterChars);
        //    if (words.Length >= 2)
        //    {
        //        int id = Convert.ToInt32(words[1]);
        //        if (id >= 0 && id <= 8)
        //        {
        //            CtrlCmds cmd = (CtrlCmds)id;
        //            if (FoundLocalControlsWorking)
        //            {
        //                if (lastHidCheck != FoundLocalControlsWorking) Debug.WriteLine("LOCAL controls found - skipping messages.");
        //            }
        //            else
        //            {
        //                if (lastHidCheck != FoundLocalControlsWorking) Debug.WriteLine("No local controls yet - using messages.");
        //                SetRobotDirection(cmd, (int)CtrlSpeeds.Max);
        //            }
        //            lastHidCheck = FoundLocalControlsWorking;
        //        }
        //    }
        //    msLastMessageInTime = MainPage.stopwatch.ElapsedMilliseconds;
        //}

       #endregion
    }
}