# Hatch Baby Rest BLE API (.NET)

This .NET API allows programmatic control over the basic functions of the <a href="https://www.hatchbaby.com/pages/rest">Hatch Baby Rest</a> Smart Night Light, Sound Machine and OK-to-Wake, including control of Power, Color, Volume and Audio Track.

<img src="https://cdn.shopify.com/s/files/1/0956/6514/collections/HatchBabyRestNightlightRemoteBlue.jpg" height="350">

## Requirements
* .NET Framework v4.6.1+
* Windows 10 Creators Update (OS Version 15063 or later) - to support non-pairing Bluetooth LE communication
* Windows 10 Compatible Bluetooth LE hardware  (tested on <a href="https://www.intel.com/content/www/us/en/products/wireless/wireless-products/dual-band-wireless-ac-7260.html">Intel Dual Band Wireless-AC 7260</a> with integrated Bluetooth)

## Installation
```powershell
Install-Package m4rcus.HatchBaby.Rest
```

## Usage

Examples in C#:

```C#
using m4rcus.HatchBaby.Rest;
```

### Discovering devices
```C#
foreach (var rest in RestDevice.Discover().Result)
{
    using (rest) // dispose of the device when done with "using" or .Dispose()
    {
        // save rest.Name and rest.BluetoothAddress
        // for later use
        await rest.SetPower(true);
        await rest.SetVolume(60);
        await rest.SetAudioTrack(AudioTrack.TRACK_STREAM);
    }
}
```

### Using devices via saved bluetooth address

```C#
// connect directly without discovery if address is known, ex "CBBABE52631F"
var bluetoothAddress = ulong.Parse("CBBABE52631F", System.Globalization.NumberStyles.HexNumber);
using (var rest = new RestDevice("MyHatchRest", bluetoothAddress))
{
    Console.WriteLine(rest.Data.Color);
    await rest.SetColor(Colors.Red);
    await Task.Delay(5000);
    await rest.SetPower(false);
}
```

### Viewing debug output on console

```C#
static void Main(string[] args)
{
    // output debugging info to console
    Trace.Listeners.Add(new ConsoleTraceListener());
}
```
See <a href="./TestConsole/Program.cs">the sample console app</a> for more examples.

## Contributing

This library currently supports Power, Color, Volume and Audio Track. For other functionality (events, time), PRs are welcome!
