using m4rcus.HatchBaby.Rest;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // output debugging info to console
            Trace.Listeners.Add(new ConsoleTraceListener());

            if (args.Length == 1)
            {
                // connect directly without discovery if address is known, ex "CBBABE52631F"
                var bluetoothAddress = ulong.Parse(args[0], System.Globalization.NumberStyles.HexNumber);
                using (var rest = new RestDevice("My Device", bluetoothAddress))
                {
                    Task.WaitAll(DoTest(rest));
                }
            }
            else
            {
                foreach (var rest in RestDevice.Discover().Result)
                {
                    using (rest) // dispose of the device when done with "using" or .Dispose()
                    {
                        Task.WaitAll(DoTest(rest));
                    }
                }
            }
        }

        static async Task DoTest(RestDevice rest)
        {
            // reset to red/quiet
            await rest.SetAudioTrack(AudioTrack.TRACK_NONE);
            await rest.SetColor(Colors.Red);
            await rest.SetPower(true);
            await Task.Delay(1000);

            // toggle on and off, green and blue
            await rest.SetPower(!rest.Data.Power);
            await Task.Delay(1000);
            await rest.SetColor(Colors.Green);
            await rest.SetPower(!rest.Data.Power);
            await Task.Delay(1000);
            await rest.SetColor(Colors.Blue);

            // set volume, audio track
            await rest.SetVolume(60);
            await rest.SetAudioTrack(AudioTrack.TRACK_STREAM);
            await Task.Delay(6000);
            await rest.SetVolume(10);
        }
    }
}
