using System;
using Windows.Storage.Streams;
using Windows.UI;

namespace m4rcus.HatchBaby.Rest
{
    public class RestDeviceData
    {
        private const byte B_HEADER1 = 0x13;
        private const byte B_HEADER2 = 0x34;
        private const byte B_COLOR = 0x43;
        private const byte B_EVENT_NEXT = 0x45;
        private const byte B_PRESET_POWER = 0x50;
        private const byte B_AUDIO = 0x53;
        private const byte B_TIME = 0x54;
        private const byte B_EVENT_ACTIVE = 0x65;

        public bool Power { get; set; }

        public Color Color { get; set; }
        public int Volume { get; set; }

        public AudioTrack AudioTrack { get; set; }

        public static RestDeviceData FromBuffer(IBuffer buffer)
        {
            var retval = new RestDeviceData();
            var data = new byte[buffer.Length];
            DataReader.FromBuffer(buffer).ReadBytes(data);
            //RestDevice.Dbg("manufacturer data: " + BitConverter.ToString(data));

            int i = 0; // data index
            while (i < data.Length)
            {
                switch (data[i])
                {
                    case B_HEADER1:
                    case B_HEADER2:
                        // TODO: verify header
                        i += 3;
                        break;
                    case B_COLOR:
                        retval.Color = Color.FromArgb(data[i+4], data[i+1], data[i+2], data[i+3]);
                        i += 5;
                        break;
                    case B_EVENT_NEXT:
                        // TODO: parse next event
                        i += 6;
                        break;
                    case B_PRESET_POWER:
                        // TODO: parse preset
                        retval.Power = (data[i+1] & 0b1100_0000) != 0b1100_0000;
                        i += 2;
                        break;
                    case B_AUDIO:
                        retval.AudioTrack = (AudioTrack)data[i+1];
                        retval.Volume = data[i+2];
                        i += 3;
                        break;
                    case B_TIME:
                        // TODO: parse time
                        i += 5;
                        break;
                    case B_EVENT_ACTIVE:
                        // TODO: parse active event
                        i += 2;
                        break;
                    case 0:
                        i += 1;
                        break;
                    default:
                        RestDevice.Dbg("unknown data id: " + data[i].ToString("X"));
                        i += 1;
                        break;
                }
            }

            RestDevice.Dbg($"power:{retval.Power}, color:{retval.Color}, vol:{retval.Volume}, trk: {retval.AudioTrack}");
            return retval;
        }
    }
}