using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TedXParserLib;

namespace TedXParser.Tests
{
    [TestClass]
    public class UnitTest1
    {


        [TestMethod]
        public void parse_tedx_transcript()
        {
            const string transcriptFileName = "ted.txt";
            const string inputWavFileAudio = @"C:\aud_prog\sample_orig.wav";
            const string outputFile = @"C:\aud_prog\sample.mp3";

            TedXParserUtils.WaveToMp3(inputWavFileAudio, outputFile);
            var txt = File.ReadAllText(transcriptFileName);
           
            var offset = TimeSpan.FromSeconds(7.5);
            var offset2 = TimeSpan.FromSeconds(8);
            var allList= TedXParserUtils.ParseTranscript(txt);
            for (var i = 0; i < allList.Count; i++)
            {
                var fname2 = outputFile.Replace(".mp3", i + ".mp3");
                var fileText= outputFile.Replace(".mp3", i + ".txt");
                File.WriteAllText(fileText, allList[i ].Item2);
                var toTime = i < allList.Count-1 ?  allList[i+1].Item1: TimeSpan.FromMilliseconds(0);
                var  fromTime= allList[i].Item1;
                TedXParserUtils.TrimMp3(outputFile, fname2, fromTime.Add(offset), toTime.Add(offset2));
            }
        }
    }
}
