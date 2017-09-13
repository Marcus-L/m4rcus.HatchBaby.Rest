using Windows.UI;

namespace m4rcus.HatchBaby.Rest
{
    public enum AudioTrack
    {
        TRACK_NONE = 0,
        TRACK_STREAM = 2,
        TRACK_PINK_NOISE = 3,
        TRACK_DRYER = 4,
        TRACK_OCEAN = 5,
        TRACK_WIND = 6,
        TRACK_RAIN = 7,
        TRACK_BIRD = 9,
        TRACK_CRICKETS = 10,
        TRACK_BRAHMS = 11,
        TRACK_TWINKLE = 13,
        TRACK_ROCKABYE = 14
    }

    public class RestCommand
    {
        public const string SET_POWER = "SI";
        public const string SET_COLOR = "SC";
        public const string SET_TRACK_NUMBER = "SN";
        public const string SET_VOLUME = "SV";

        public static string Format(string command, int value)
        {
            return $"{command}{value:X2}";
        }

        public static string Format(string command, Color color)
        {
            return $"{command}{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
        }
    }
}
