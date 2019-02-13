using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

using System.ComponentModel;
using OAS_DriverLibrary;
//Imports System.Windows.Threading
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace AcculoadOASDriver
{
	public class DriverInterface : BaseClass
	{
		//Demo Enumerations
		private bool InstanceFieldsInitialized = false;

        Socket sock;
        string acculoadArmAddress;
        string acculoadUnits;

		public DriverInterface()
		{
			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
                sock = socket();
			}
		}

		private void InitializeInstanceFields()
		{
			localTimer = new Timer(TimerRoutine, null, Timeout.Infinite, Timeout.Infinite);
		}

        public enum CommandTypes
        {
            Status = 0,
            IVVolume = 1,
            GVVolume = 2,
            GSTVolume = 3,
            GSVVolume = 4,
            TransactionNumber = 5
        }

        public enum VolumeUnits
        {
            Gallons = 0,
            Barrels = 1,
            CubicMeters = 2
        }

        //Require Variables
        private string m_DriverName = "Accuload Driver";

		//Lists of  Properties For Driver Interface and Tags
		private List<OAS_DriverLibrary.ClassProperty> m_DriverProps = new List<OAS_DriverLibrary.ClassProperty>();

		//Shadowed Events
		public new delegate void AsyncReadCallbackEventHandler(OAS_DriverLibrary.ClassTagValue[] TagData);
		public new event AsyncReadCallbackEventHandler AsyncReadCallback;
		public new delegate void UpdateSystemErrorEventHandler(bool ErrorIsActive, string Category, int MessageID, string Message);
		public new event UpdateSystemErrorEventHandler UpdateSystemError;

		//Demo Code
		private bool m_Connected;
		//Active Tags
		private Hashtable m_Tags = new Hashtable();
		private Hashtable m_StaticTagValues = new Hashtable();
		//Used to simulate different Polling Rates
		private Hashtable m_LastUpdateTime = new Hashtable();
		private Timer localTimer;
		private bool m_InTimerRoutine;

#region Driver Section

		public override string DriverName
		{
			get
			{
				return m_DriverName;
			}
			set
			{
				m_DriverName = value;
			}
		}

		public override List<ClassProperty> DriverConfig
		{
			get
			{
				return m_DriverProps;
			}
			set
			{
				m_DriverProps = value;

			}
		}

		public override List<ClassProperty> GetDefaultDriverConfig()
		{
			try
			{
				List<ClassProperty> DriverProps = new List<ClassProperty>();
				DriverProps.Clear();
				// Define the properties for the Interface.  Typically IP Address, Port Number, database connection settings, etc.
                DriverProps.Add(new ClassProperty("IPAddress", "IP Address", "Device IP Address", typeof(string), "", ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("ArmAddress", "Arm Address", "Address of the arm to connect to (number must include the preceding 0).", typeof(String), "01", ClassProperty.ePropInputType.Manual));
                DriverProps.Add(new ClassProperty("Units", "Units", "Used to set the units the Accuload is configured to measure", typeof(VolumeUnits), VolumeUnits.Gallons, ClassProperty.ePropInputType.Manual));

                return DriverProps;
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Configuration", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
			}
			return null;
		}

		public override void Connect()
		{
			try
			{
				//Add Connection Logic. m_DriverProps is a list of ClassDriverProperty in the same order of Get Driver Config
				//SelectDriverType DriverType = (AcculoadOASDriver.DriverInterface.SelectDriverType)GetPropValue(m_DriverProps, "DriverType");
				//int DriverType0Integer = Convert.ToInt32(GetPropValue(m_DriverProps, "DriverType0Integer"));
				//int DriverType1Integer = Convert.ToInt32(GetPropValue(m_DriverProps, "DriverType1Integer"));
				//int DriverType2Integer = Convert.ToInt32(GetPropValue(m_DriverProps, "DriverType1And2Integer"));
				//double ExampleDouble = Convert.ToDouble(GetPropValue(m_DriverProps, "ExampleDouble"));
                string IpAddress = Convert.ToString(GetPropValue(m_DriverProps, "IPAddress"));
                acculoadArmAddress = Convert.ToString(GetPropValue(m_DriverProps, "ArmAddress"));
                acculoadUnits = Convert.ToString(GetPropValue(m_DriverProps, "Units"));

				if (!(m_Connected))
				{
					localTimer.Change(100, 100);

                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IpAddress), 7734);

                    try
                    {
                        try
                        {
                            sock.Connect(endPoint);
                        }
                        catch (ObjectDisposedException od)
                        {
                            sock = socket();
                            sock.Connect(endPoint);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Some other socket exception");
                        }
                        

                        Console.WriteLine("Connected to {0}", sock.RemoteEndPoint.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception occurred");
                        Console.WriteLine(e.ToString());
                    }


				}
				m_Connected = true;
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Connect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
			}
		}

		public override bool Disconnect()
		{
			try
			{
				if (!(m_Connected))
				{
					return m_Connected;
				}

				//Add Disconnection Logic
				localTimer.Change(Timeout.Infinite, Timeout.Infinite);

                sock.Close();

                Console.WriteLine("Socket disconnected");

				lock (m_Tags.SyncRoot)
				{
					m_Tags.Clear();
				}

				m_Connected = false;
				return m_Connected;
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Disconnect", 1, "GetDefaultDriverConfig Exception: " + ex.Message);
			}
			return false;
		}
#endregion

#region Tag Section
		//This Function defines the tag configuration properties and builds the UI For the Tag Configuration Properties
		//Place items in the order you want them to appear in the UI
		//Adding "-->" to the Property Description will add the next property to the right of the current property.
		//If you have a blank String for the Property Help no Help button will be displayed.

		//Note: Do not use the property names TagName and PollingRate.  These are OAS property names that are already defined for the interface.

		public override List<ClassProperty> GetDefaultTagConfig()
		{
			try
			{
				List<ClassProperty> m_TagProps = new List<ClassProperty>();

                //m_TagProps.Add(new ClassProperty("SimType", "Simulation Type", @"The simulation type of a Parameter can be set to one of the following types.
                //            Dynamic: Read only value that changes dynamically from one of the Dynamic Simuation Types
                //            Static: Value is fixed and can be written to.", typeof(SimTypes), SimTypes.Dynamic, ClassProperty.ePropInputType.Manual));

                //m_TagProps.Add(new ClassProperty("DynamicSimType", "Dynamic Simulation Type", @"The dynamic simulation type of a Parameter can be set to one of the following types.
                //            Ramp: Value changes from 0 to 100.
                //            Random: Value changes randomly from 0 to 100
                //            Sine: Value changes from -1 to 1", typeof(DynamicSimTypes), DynamicSimTypes.Ramp, ClassProperty.ePropInputType.Manual, "Visible,SimType.Dynamic"));

                m_TagProps.Add(new ClassProperty("CommandType", "Command Type", @"The command type tells OAS what command to send to the accuload.
                                                                                    -IV = No Adjustments (Raw measurement)
                                                                                    -GV = Volume corrected with Meter Factor
                                                                                    -GST = Gross @ standard temperature
                                                                                    -GSV = Gross @ standard temperature and pressure corrected", typeof(CommandTypes), CommandTypes.Status, ClassProperty.ePropInputType.Manual));

				return m_TagProps;
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Configuration", 1, "GetDefaultTagConfig Exception: " + ex.Message);
			}
			return null;
		}

		public override void AddTags(List<ClassProperty>[] Tags)
		{
			try
			{
				//Add Logic. Props is a list of ClassProperty in the same order of Get Tag Config
				lock (m_Tags.SyncRoot)
				{
					foreach (List<ClassProperty> Props in Tags)
					{
						// Use the TagName as a unique identifier for the Tag Name and Paramater being interfaced with.
						string TagID = Convert.ToString(GetPropValue(Props, "TagName"));
						// Use the polling rate to set the communication rate to your device or software application.
						// If you interface uses async callbacks with a subscription rate you could create multple collections of tags based on PollingRate.
						double PollingRate = Convert.ToDouble(GetPropValue(Props, "PollingRate"));

						if (m_Tags.Contains(TagID))
						{
							m_Tags[TagID] = Props;
						}
						else
						{
							m_Tags.Add(TagID, Props);
						}
						if (m_LastUpdateTime.Contains(TagID))
						{
							m_LastUpdateTime.Remove(TagID);
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Communications", 1, "AddTags Exception: " + ex.Message);
			}
		}

		public override void RemoveTags(string[] Tags)
		{
			try
			{
				lock (m_Tags.SyncRoot)
				{
					foreach (string TagID in Tags)
					{
						if (m_Tags.Contains(TagID))
						{
							m_Tags.Remove(TagID);
						}
						if (m_LastUpdateTime.Contains(TagID))
						{
							m_LastUpdateTime.Remove(TagID);
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Communications", 2, "RemoveTags Exception: " + ex.Message);
			}
		}

		// This call is performed when a Device Read is executed in OAS.
		public override ClassTagValue[] SyncRead(List<ClassProperty>[] Tags)
		{
			try
			{
				DateTime currentTime = DateTime.Now;
				double localSeconds = currentTime.Second + (currentTime.Millisecond / 1000.0);
				ArrayList localArrayList = new ArrayList();

				lock (m_StaticTagValues.SyncRoot)
				{
					foreach (List<ClassProperty> TagItems in Tags)
					{
						string TagID = Convert.ToString(GetPropValue(TagItems, "TagName"));
						object Value = null;

                        CommandTypes CommandType = (AcculoadOASDriver.DriverInterface.CommandTypes)GetPropValue(TagItems, "CommandType");

                        Value = commandResponse(CommandType.ToString());
						bool Quality = false;
						if (Value != null)
						{
							Quality = true;
						}
						localArrayList.Add(new ClassTagValue(TagID, Value, currentTime, Quality));
					}
				}

				return (ClassTagValue[])localArrayList.ToArray(typeof(ClassTagValue));
			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Communications", 3, "SyncRead Exception: " + ex.Message);
			}

			return null;
		}


		public override void WriteValues(string[] TagIDs, object[] Values, List<ClassProperty>[] TagProperties)
		{
			try
			{
                //Add write Logic to actual driver
                //	int Index = 0;
                //	for (Index = 0; Index < TagIDs.GetLength(0); Index++)
                //	{
                //		SimTypes SimType = (AcculoadOASDriver.DriverInterface.SimTypes)GetPropValue(TagProperties[Index], "SimType");
                //		if (SimType == SimTypes.Static)
                //		{
                //			lock (m_StaticTagValues.SyncRoot)
                //			{
                //				if (m_StaticTagValues.Contains(TagIDs[Index]))
                //				{
                //					m_StaticTagValues[TagIDs[Index]] = Values[Index];
                //				}
                //				else
                //				{
                //					m_StaticTagValues.Add(TagIDs[Index], Values[Index]);
                //				}
                //			}
                //		}
                //	}
            }
            catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Communications", 4, "WriteValues Exception: " + ex.Message);
			}
		}

        #endregion

        #region Accuload code

        Socket socket()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private string commandResponse(string commandType)
        {
            byte[] sendData;
            byte[] receiveData = new byte[255];
            int rec = 0;
            switch(commandType)
            {
                case "Status":
                    sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RS" + "\x03");
                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);
                    Array.Resize(ref receiveData, rec);
                    string statusResultString = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
                    Console.WriteLine("Raw result string: {0}", statusResultString.ToString());
                    if (statusResultString.Contains("AU"))
                        return "Arm Authorized";
                    else if (statusResultString.Contains("BD"))
                        return "Idle";
                    else if (statusResultString.Contains("FL") || statusResultString.Contains("TP"))
                        return "In Progress";
                    else
                        return "Unable to determine";
                    break;
                case "IVVolume":
                    //Check the status to see if the accuload is in progress.  If not, pull the most recent transaction
                    if (accuLoadRunning())
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT R" + "\x03");
                    }
                    else
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT R 001" + "\x03");
                    }

                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);

                    double ivVolume = volumeConvert(receiveData, rec);

                    return ivVolume.ToString();
                    break;
                case "GVVolume":
                    if (accuLoadRunning())
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT G" + "\x03");
                    }
                    else
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT G 001" + "\x03");
                    }
                    
                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);
                    double gvVolume = volumeConvert(receiveData, rec);

                    return gvVolume.ToString();
                    break;
                case "GSTVolume":
                    if (accuLoadRunning())
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT N" + "\x03");
                    }
                    else
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT N 001" + "\x03");
                    }
                    
                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);
                    double gstVolume = volumeConvert(receiveData, rec);

                    return gstVolume.ToString();
                    break;
                case "GSVVolume":
                    if (accuLoadRunning())
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT P" + "\x03");
                    }
                    else
                    {
                        sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RT P 001" + "\x03");
                    }

                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);
                    double gsvVolume = volumeConvert(receiveData, rec);

                    return gsvVolume.ToString();
                    break;
                case "TransactionNumber":
                    int transactionNumber = 0;
                    sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "TN 001" + "\x03");
                    sock.Send(sendData, 0, sendData.Length, 0);
                    rec = sock.Receive(receiveData);

                    if (accuLoadRunning())
                    {
                        transactionNumber = Convert.ToInt32(Encoding.ASCII.GetString(receiveData, 7, 4)) + 1;
                    }
                    else
                    {
                        transactionNumber = Convert.ToInt32(Encoding.ASCII.GetString(receiveData, 7, 4));
                    }

                    return transactionNumber.ToString();
                    break;
            }
            return "We didn't find a case for that";

        }

        private bool accuLoadRunning()
        {
            byte[] sendData;
            byte[] receiveData = new byte[255];
            int rec;
            sendData = Encoding.ASCII.GetBytes("\x02" + acculoadArmAddress + "RS" + "\x03");
            sock.Send(sendData, 0, sendData.Length, 0);
            rec = sock.Receive(receiveData);
            Array.Resize(ref receiveData, rec);
            string statusResultString = Encoding.ASCII.GetString(receiveData, 0, receiveData.Length);
            Console.WriteLine("Raw result string: {0}", statusResultString.ToString());
            if (statusResultString.Contains("AU") || statusResultString.Contains("BD"))
                return false;
            else
                return true;
        }
        private double volumeConvert(byte[] volumeBytes, int size)
        {
            Array.Resize(ref volumeBytes, size);

            double volume = Convert.ToDouble(Encoding.ASCII.GetString(volumeBytes, 15, 8));

            if (acculoadUnits == "Gallons")
                volume = volume / 42;

            return volume;
        }


        #endregion
        #region Demo Driver Code
        // This is a simple example of getting the properties of a tag and using that to generate a update to the tag value
        private void TimerRoutine(object State)
		{
			try
			{
				if (m_InTimerRoutine)
				{
					return;
				}
				m_InTimerRoutine = true;
				DateTime currentTime = DateTime.Now;
				double localSeconds = currentTime.Second + (currentTime.Millisecond / 1000.0);

				ArrayList localArrayList = new ArrayList();

				lock (m_Tags.SyncRoot)
				{
					lock (m_StaticTagValues.SyncRoot)
					{
						List<ClassProperty> TagItems = null;
						foreach (DictionaryEntry de in m_Tags)
						{
							string TagID = Convert.ToString(de.Key);
							TagItems = (List<ClassProperty>)de.Value;

							// Just simulating using the PollingRate property
							bool OKToPoll = true;
							if (m_LastUpdateTime.Contains(TagID))
							{
								double PollingRate = Convert.ToDouble(GetPropValue(TagItems, "PollingRate"));
								DateTime lastUpdateTime = Convert.ToDateTime(m_LastUpdateTime[TagID]);
								if (lastUpdateTime.AddSeconds(PollingRate) > currentTime)
								{
									OKToPoll = false;
								}
							}

							if (OKToPoll)
							{
								if (m_LastUpdateTime.Contains(TagID))
								{
									m_LastUpdateTime[TagID] = currentTime;
								}
								else
								{
									m_LastUpdateTime.Add(TagID, currentTime);
								}

								object Value = null;
                                CommandTypes CommandType = (AcculoadOASDriver.DriverInterface.CommandTypes)GetPropValue(TagItems, "CommandType");
                                Value = commandResponse(CommandType.ToString());
                                bool Quality = false;
								if (Value != null)
								{
									Quality = true;
								}
								// You can include mutiple values to the same tag with different timestamps in the same callback if you like.
								// In this example it just updates when the timer fires and the check for the PollingRate succeeds.
								localArrayList.Add(new ClassTagValue(TagID, Value, currentTime, Quality));

							}
						}
					}
				}
				// Firing this event will update the tag values
				if (AsyncReadCallback != null)
					AsyncReadCallback((ClassTagValue[])localArrayList.ToArray(typeof(ClassTagValue)));

				// The following can be used in any routine to post an error during Runtime operation of OAS.
				//RaiseEvent UpdateSystemError(True, "Communications", 1, "An example of posting a system error")


			}
			catch (Exception ex)
			{
				if (UpdateSystemError != null)
					UpdateSystemError(true, "Communications", 5, "TimerRoutine Exception: " + ex.Message);
			}
			m_InTimerRoutine = false;
		}


        #endregion

    }





}