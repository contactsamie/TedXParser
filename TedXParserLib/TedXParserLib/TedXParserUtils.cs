

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.SoundIn;
using NAudio.Lame;
using NAudio.Wave;
using WasapiLoopbackCapture = CSCore.SoundIn.WasapiLoopbackCapture;
using WaveFileReader = NAudio.Wave.WaveFileReader;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using NAudio.Lame;
using NAudio.Wave;
using MediaFoundationEncoder = CSCore.MediaFoundation.MediaFoundationEncoder;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using NAudio.Lame;
using NAudio.Wave;

namespace TedXParserLib
{
    public  class TedXParserUtils
    {
        public static void WaveToMp3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
            using (var reader = new NAudio.Wave.WaveFileReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }
        public static  void TrimMp3(string inputPath, string outputPath, TimeSpan? begin, TimeSpan? end)
        {
            try
            {
                if (begin.HasValue && end.HasValue && begin > end)
                    throw new ArgumentOutOfRangeException("end", "end should be greater than begin");

                using (var reader = new Mp3FileReader(inputPath))
                using (var writer = File.Create(outputPath))
                {
                    Mp3Frame frame;
                    while ((frame = reader.ReadNextFrame()) != null)
                        if (reader.CurrentTime >= begin || !begin.HasValue)
                        {
                            if (reader.CurrentTime <= end || !end.HasValue)
                                writer.Write(frame.RawData, 0, frame.RawData.Length);
                            else break;
                        }
                }
            }
            catch (Exception)
            {


            }
        }
        public static void Mp3ToWave(string mp3FileName, string waveFileName)
        {
            using (var reader = new NAudio.Wave.Mp3FileReader(mp3FileName))
            using (var writer = new NAudio.Wave.WaveFileWriter(waveFileName, reader.WaveFormat))
                reader.CopyTo(writer);
        }
        public static void Mp4ToMp3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
            using (var reader = new MediaFoundationReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }
        public static List<Tuple<TimeSpan, string>> ParseTranscript(string txt)
        {
            List<Tuple<TimeSpan, string>> allList = new List<Tuple<TimeSpan, string>>();
            var times = new List<TimeSpan>();
            txt = new Regex(@"(\d+?:\d+)(.*)").Replace(txt, m =>
            {
                var timeTmp = m.Groups[1].Value.Split(':');
                var time = TimeSpan.FromMinutes(Convert.ToDouble(timeTmp[0]));
                var timeSpan = time.Add(TimeSpan.FromSeconds(Convert.ToDouble(timeTmp[1])));
                times.Add(timeSpan);
                return "|"+ m.Groups[1].Value;
            });
            var parts = txt.Trim().TrimStart('|').Split('|').ToList();
            
            allList = parts.Select((t, i) => new Tuple<TimeSpan, string>(times[i], t)).ToList();
         
            return allList;
        }
    }
}
