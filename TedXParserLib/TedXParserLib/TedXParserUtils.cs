

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
       
        private readonly WaveOut _outputter = new WaveOut()
        {
            DesiredLatency = 5000 //arbitrary but <1k is choppy and >1e5 errors
               ,
            NumberOfBuffers = 1 // 1,2,4 all work...
               ,
            DeviceNumber = 0
        };
        private WasapiCapture _capture;
        private WaveWriter _w;
        public void StartRecord(string file)
        {
            _capture = new WasapiLoopbackCapture();

                // _capture = new WasapiLoopbackCapture();
                _capture.Initialize();
                _w = new WaveWriter(file, _capture.WaveFormat);
                _capture.DataAvailable += (s, capData) => _w.Write(capData.Data, capData.Offset, capData.ByteCount);
                _capture.Start();
           
        }
        public void StopRecord()
        {
            if (_w == null || _capture == null) return;
            _capture.Stop();
            _w.Dispose();
            _w = null;
            _capture.Dispose();
            _capture = null;
        }
       
        public  TimeSpan Play(string text)
        {
            var reader = new NAudio.Wave.AudioFileReader(text);
            _outputter.Init(reader);
            _outputter.Play();
            return reader.TotalTime;
        }
        public void Stop()
        {
            _outputter.Stop();
        }

        public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {

            var name = outPath;
            var startTimeSpan = cutFromStart;
            var endTimeSpan = cutFromEnd;
            using (IWaveSource source = CodecFactory.Instance.GetCodec(inPath))
            using (var mediaFoundationEncoder = MediaFoundationEncoder.CreateWMAEncoder(source.WaveFormat, name ))
            {
                AddTimeSpan(source, mediaFoundationEncoder, startTimeSpan, endTimeSpan);
            }
            // WaveToMp3(name + ".wav", name + ".mp3");
        
        }
        public static void TrimWavFile(string inPath, string outFile, TimeSpan? expectedDuration)
        {
           
            IWaveSource waveSource =  CodecFactory.Instance.GetCodec(inPath);
            TimeSpan totalTime = waveSource.GetLength();
            var name = outFile.Split('.')[0]+"_tmp.wav";
          
            using (IWaveSource source = CodecFactory.Instance.GetCodec(inPath))
            using (var mediaFoundationEncoder = MediaFoundationEncoder.CreateWMAEncoder(source.WaveFormat, name))
            {
                AddTimeSpan(source, mediaFoundationEncoder, totalTime);
            }

            if(expectedDuration!=null)
            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(x =>
            {
                TrimWavFile(name, outFile, TimeSpan.FromMilliseconds(0), expectedDuration.Value);
            }).Wait();

        }
        private const short FILTER_FREQ_LOW = -10000;
        private const short FILTER_FREQ_HIGH = 10000;
        private static void AddTimeSpan(IWaveSource source, IWriteable mediaFoundationEncoder, TimeSpan total)
        {
            int read = 0;
            long bytesToEncode = source.GetBytes(total);
            var buffer = new byte[source.WaveFormat.BytesPerSecond];
            bool canWrite = false;
            
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                int bytesToWrite = (int)Math.Min(read, bytesToEncode);

                if (!canWrite)
                {
                    var t=   ComplementToSigned( buffer, bytesToWrite);
                    if (t > 100 || t< -100)
                    {
                        canWrite = true;
                    }
                }
                if (canWrite)
                {
                    mediaFoundationEncoder.Write(buffer, 0, bytesToWrite);
                }
              
                bytesToEncode -= bytesToWrite;
            }
        }
        private static short ComplementToSigned( byte[] bytArr, int intPos) // 2's complement to normal signed value
        {
            short snd = BitConverter.ToInt16(bytArr, intPos/2);
            if (snd != 0)
                snd = Convert.ToInt16((~snd | 1));
            return snd;
        }
        private static void AddTimeSpan(IWaveSource source, IWriteable mediaFoundationEncoder, TimeSpan startTimeSpan, TimeSpan endTimeSpan)
        {
            source.SetPosition(startTimeSpan);

            int read = 0;
            long bytesToEncode = source.GetBytes(endTimeSpan - startTimeSpan);

            var buffer = new byte[source.WaveFormat.BytesPerSecond];
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                int bytesToWrite = (int)Math.Min(read, bytesToEncode);
                mediaFoundationEncoder.Write(buffer, 0, bytesToWrite);
                bytesToEncode -= bytesToWrite;
            }
        }

       

        public static void WaveToMp3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
            using (var reader = new NAudio.Wave.WaveFileReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }
        public static void Mp3ToWave(string mp3FileName, string waveFileName)
        {
            using (var reader = new NAudio.Wave.Mp3FileReader(mp3FileName))
            using (var writer = new NAudio.Wave.WaveFileWriter(waveFileName, reader.WaveFormat))
                reader.CopyTo(writer);
        }
        public static byte[] ConvertWavToMp3(byte[] wavFile)
        {
            using (var retMs = new MemoryStream())
            using (var ms = new MemoryStream(wavFile))
            using (var rdr = new NAudio.Wave.WaveFileReader(ms))
            using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, 128))
            {
                rdr.CopyTo(wtr);
                return retMs.ToArray();
            }
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
            txt = new Regex(@"(\d+?:\d+)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase).Replace(txt, m =>
            {
                var timeTmp = m.Groups[1].Value.Replace(":", "");
                var time = TimeSpan.FromSeconds(Convert.ToDouble(timeTmp));
                times.Add(time);
                return "|";
            });
            var parts = txt.Trim().TrimStart('|').Split('|').ToList();
            
            allList = parts.Select((t, i) => new Tuple<TimeSpan, string>(times[i], t)).ToList();
         
            return allList;
        }
    }
}
