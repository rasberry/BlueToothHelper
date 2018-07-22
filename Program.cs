using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BlueToothHelper
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1) {
				Usage();
				return;
			}

			if (!Enum.TryParse(args[0],true,out Action action)) {
				Console.Error.WriteLine("Unknown Action");
				return;
			}

			if (action == Action.Discover) {
				Discover();
			}
			else if (action == Action.Receive) {
				ReceiveFiles();
			}
		}

		static void Usage()
		{
			Console.WriteLine(""
				  +"Usage "+nameof(BlueToothHelper)+" (action)"
				+"\n Actions:"
				+"\n  Receive      Wait for files to be transfered from a device then saves them to local files"
				+"\n  Discover     Show information about all known devices"
			);
		}

		enum Action {
			None = 0,
			Receive,
			Discover
		}

		static void Discover()
		{
			Console.WriteLine("Looking for devices...");
			var bc = new BluetoothClient();
			var devices = bc.DiscoverDevices();
			foreach(var d in devices) {
				Console.WriteLine("\n[Device]"
					+"\n DeviceName    " + d.DeviceName
					+"\n ClassOfDevice " + d.ClassOfDevice.Device+" ["+d.ClassOfDevice.Service+"]"
					+"\n Connected     " + d.Connected
					+"\n Authenticated " + d.Authenticated
					+"\n DeviceAddress " + d.DeviceAddress
					+"\n Rssi          " + d.Rssi
				);

				Console.WriteLine("\n [Services]");
				var services = d.InstalledServices;
				foreach(Guid s in services) {
					try {
						var records = d.GetServiceRecords(s);
						foreach(var r in records) {
							foreach(var e in r) {
								if (e.Value.ElementType == ElementType.TextString) {
									Console.WriteLine("  "+e.Value.GetValueAsStringUtf8());
								}
							}
						}
					} catch(Exception e) {
						Console.WriteLine("  Unknown Service ["+s+"]");
					}
				}
			}
		}

		//static string BytesToHexAlpha(byte[] bytes)
		//{
		//	StringBuilder sb = new StringBuilder();
		//	foreach(byte b in bytes) {
		//		char c = Convert.ToChar(b);
		//		if (!Char.IsControl(c)) {
		//			sb.Append(c);
		//		} else {
		//			sb.Append(".");
		//		}
		//	}
		//	return sb.ToString();
		//}

		static void ReceiveFiles()
		{
			BluetoothRadio br = BluetoothRadio.PrimaryRadio;
			if (br == null) {
				Console.WriteLine("No BlueTooth radio found");
				return;
			}

			if (br.Mode != RadioMode.Discoverable) {
				br.Mode = RadioMode.Discoverable;
			}

			var listener = new ObexListener(ObexTransport.Bluetooth);
			listener.Start();
			Console.WriteLine("Listening for files... press any key to stop");

			//need to use another thread since we have two blocking calls
			var stopit = Task.Factory.StartNew(() => {
				Console.ReadKey(true); //blocks
				listener.Stop();
			});

			while(listener.IsListening)
			{
				try {
					var olc = listener.GetContext(); //blocks
					var olr = olc.Request;

					string filename = Uri.UnescapeDataString(olr.RawUrl.TrimStart(new char[] { '/' }));
					string final = DateTime.Now.ToString("yyMMddHHmmss") + "-" + filename;
					olr.WriteFile(final);
					Console.WriteLine("Wrote "+final);

				} catch(Exception e) {
					e.ToString();
					break;
				}
			}
		}
	}
}
