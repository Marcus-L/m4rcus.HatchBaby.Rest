using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI;

namespace m4rcus.HatchBaby.Rest
{
    public class RestDevice : IDisposable
    {
        private class BT
        {
            public static readonly Guid SVC_ADVERTISING = new Guid("02260001-5efd-47eb-9c1a-de53f7a2b232");
            public static readonly Guid SVC_REST = new Guid("02240001-5efd-47eb-9c1a-de53f7a2b232");
            public static readonly Guid CHAR_RX = new Guid("02240003-5efd-47eb-9c1a-de53f7a2b232");
            public static readonly Guid CHAR_TX = new Guid("02240002-5efd-47eb-9c1a-de53f7a2b232");
            public static readonly Guid CHAR_FEEDBACK = new Guid("02260002-5efd-47eb-9c1a-de53f7a2b232");
        }

        private BluetoothLEDevice Device;
        private GattCharacteristic CharRX;
        private GattCharacteristic CharTX;
        private GattCharacteristic CharFeedback;
        private Queue<Action<byte[]>> CommandCallbacks = new Queue<Action<byte[]>>();

        public string Name { get; private set; }
        public ulong BluetoothAddress { get; private set; }
        private RestDeviceData Data_;
        public RestDeviceData Data
        {
            get
            {
                if (Data_ == null) 
                {
                    Task.WaitAll(ConnectDevice());
                }
                return Data_;
            }
            private set {
                Data_ = value;
            }
        }

        public RestDevice(string name, ulong bluetoothAddress)
        {
            Name = name;
            BluetoothAddress = bluetoothAddress;
        }

        internal static void Dbg(string message)
        {
            Debug.WriteLine(message, "m4rcus.HatchBaby.Rest");
        }

        private async Task<bool> ConnectDevice()
        {
            if (Device == null)
            {
                string bta = BluetoothAddress.ToString("X");
                Device = await BluetoothLEDevice.FromBluetoothAddressAsync(BluetoothAddress);
                if (Device == null)
                {
                    return false;
                }
                Dbg("connected to device: " + bta);

                var characteristics = new List<GattCharacteristic>();
                var gatt = await Device.GetGattServicesAsync();
                var svc_rest = gatt.Services.First(s => s.Uuid == BT.SVC_REST);
                var svc_advertising = gatt.Services.First(s => s.Uuid == BT.SVC_ADVERTISING);
                characteristics.AddRange((await svc_rest.GetCharacteristicsAsync()).Characteristics);
                characteristics.AddRange((await svc_advertising.GetCharacteristicsAsync()).Characteristics);
                try
                {
                    // save TX/RX/Manufacturer channels
                    CharTX = characteristics.First(c => c.Uuid == BT.CHAR_TX);
                    CharRX = characteristics.First(c => c.Uuid == BT.CHAR_RX);
                    CharFeedback = characteristics.First(c => c.Uuid == BT.CHAR_FEEDBACK);
                }
                catch (Exception)
                {
                    Dbg("error configuring characteristics: " + bta);
                    Device = null;
                    return false;
                }
                await RefreshData();
                #region there is a bug with UWP read notifications, this does not work (yet)
                //CharRX.ValueChanged += (obj, args) => ProcessRXData(args.CharacteristicValue);
                // var status = await CharRX.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                // if (status != GattCommunicationStatus.Success)
                // {
                //     throw new Exception("Could not connect to receive channel.");
                // }
                // Dbg("registered for rx notify: " + bta);
                #endregion
            }
            return true;
        }

        private void ProcessRXData(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            var rx_data = DataReader.FromBuffer(buffer);
            rx_data.ReadBytes(data);
            CommandCallbacks.Dequeue()(data);
        }

        public void Dispose()
        {
            if (Device != null)
            {
                Device.Dispose();
                Device = null;
                CharRX = null;
                CharTX = null;
            }
        }

        public async Task<bool> SetPower(bool on)
        {
            var result = await SendCommand(RestCommand.Format(RestCommand.SET_POWER, on ? 1 : 0));
            return (result == "OK");
        }

        public async Task<bool> SetVolume(int volume)
        {
            if (volume < 0 || volume > 100)
            {
                throw new ArgumentException("volume must be between 0-100");
            }
            var result = await SendCommand(RestCommand.Format(RestCommand.SET_VOLUME, volume));
            return (result == "OK");
        }

        public async Task<bool> SetAudioTrack(AudioTrack audioTrack)
        {
            var result = await SendCommand(RestCommand.Format(RestCommand.SET_TRACK_NUMBER, (int)audioTrack));
            return (result == "OK");
        }

        public async Task<bool> SetColor(Color color)
        {
            var result = await SendCommand(RestCommand.Format(RestCommand.SET_COLOR, color));
            return (result == "OK");
        }

        public async Task<bool> RefreshData()
        {
            var mfd = await CharFeedback.ReadValueAsync(BluetoothCacheMode.Uncached);
            Data = RestDeviceData.FromBuffer(mfd.Value);
            return true;
        }
        public async Task<string> SendCommand(string command, bool refreshData = true)
        {
            Dbg("sending command: " + command);
            var data = await SendCommand(Encoding.UTF8.GetBytes(command));
            if (data == null)
            {
                Dbg("error sending command: " + command);
                return "";
            }
            else
            {
                var response = Encoding.UTF8.GetString(data);
                Dbg("received response: " + response);
                if (refreshData)
                {
                    await Task.Delay(150); // let bluetooth catch up
                    await RefreshData();
                }
                return response;
            }
        }

        public async Task<byte[]> SendCommand(byte[] data)
        {
            if (await ConnectDevice())
            {
                var tcs = new TaskCompletionSource<byte[]>();
                var writer = new DataWriter();
                writer.WriteBytes(data);
                CommandCallbacks.Enqueue(rx_data => tcs.SetResult(rx_data));
                await CharTX.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);

                // workaround valuechanged bug (wait a bit then poll the RX characteristic)
                await Task.Delay(500);
                ProcessRXData((await CharRX.ReadValueAsync(BluetoothCacheMode.Uncached)).Value);

                return await tcs.Task;
            }
            else
            {
                return null;
            }
        }

        public static async Task<IEnumerable<RestDevice>> Discover(int timeout = 7)
        {
            var tcs = new TaskCompletionSource<IEnumerable<RestDevice>>();
            var rests = new Dictionary<ulong, RestDevice>();
            var advertisements = new Dictionary<ulong, BluetoothLEAdvertisementReceivedEventArgs>();
            var ble_watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            ble_watcher.Received += (obj, adv) =>
            {
                // name is received in one advertisement, manufacturer data in a different one
                if (!advertisements.ContainsKey(adv.BluetoothAddress))
                {
                    advertisements[adv.BluetoothAddress] = adv;
                }
                // accumulate the required info in the cache
                var cached = advertisements[adv.BluetoothAddress];
                if (cached.Advertisement.LocalName == "" && adv.Advertisement.LocalName != "")
                {
                    cached.Advertisement.LocalName = adv.Advertisement.LocalName;
                }
                if (cached.Advertisement.ManufacturerData.Count == 0 &&
                    adv.Advertisement.ManufacturerData.Count == 1)
                {
                    cached.Advertisement.ManufacturerData.Add(adv.Advertisement.ManufacturerData[0]);
                }
                // check if the device is a Rest
                if (IsRestAdvertisement(cached.Advertisement) && !rests.ContainsKey(cached.BluetoothAddress))
                {
                    Dbg("discovered rest device: " + cached.BluetoothAddress.ToString("X"));
                    var rest = new RestDevice(cached.Advertisement.LocalName, cached.BluetoothAddress);
                    rests.Add(cached.BluetoothAddress, rest);
                }
            };
            ble_watcher.Stopped += (obj, e) =>
            {
                Dbg("discovery complete");
                tcs.SetResult(rests.Values);
            };
            ble_watcher.Start();
            Dbg("starting discovery");

            // wait until timeout 
            await Task.Delay(timeout * 1000);
            ble_watcher.Stop();

            return await tcs.Task;
        }

        private static bool IsRestAdvertisement(BluetoothLEAdvertisement adv)
        {
            // return false if not all the data is available
            if (adv.ManufacturerData.Count == 0 || adv.LocalName == "")
            {
                return false;
            }

            var mData = adv.ManufacturerData[0];
            byte[] data = new byte[mData.Data.Length];
            DataReader.FromBuffer(mData.Data).ReadBytes(data);

            // check manufacturer id
            if (mData.CompanyId == 1076 ||
                mData.CompanyId == 21842)
            {
                // check first byte
                if (data.Length >= 1)
                {
                    return data[0] == 'r' || data[0] == 'R';
                }
            }
            return false;
        }
    }
}
