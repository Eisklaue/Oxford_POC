using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Microsoft.ProjectOxford.SpeechRecognition;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Oxford_PoC
{
    class Program
    {
        //Project Oxford Lizenz Key
        static string subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];

        /*
        Sprachauswahl
            American English: "en-us"
            British English: "en-gb"
            German: "de-de"
            Spanish: "es-es"
            French: "fr-fr"
            Italian: "it-it"
            Mandarin: "zh-cn"
        */
        static string recoLanguage = "de-de";

        //Mikrofon als Soundquelle
        static MicrophoneRecognitionClient micClient;

        static AutoResetEvent FinalResponseEvent;

        static void Main(string[] args)
        {


            //Response Event von MAIS
            FinalResponseEvent = new AutoResetEvent(false);

            Console.WriteLine("Was wollen Sie tun?\n1. ShortPhrase Recognition\n2. LongPhrase Recognition\nAuswahl: ");

            ConsoleKeyInfo input = Console.ReadKey();

            switch (input.KeyChar)
            {
                //Short phrase recongition verwendet das Mikrophone
                case ('1'):
                    Console.WriteLine("Short");
                    LogRecognitionStart("microphone", recoLanguage, SpeechRecognitionMode.ShortPhrase);

                    if (micClient == null)
                    {
                        micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.ShortPhrase, recoLanguage, SubscriptionKey);
                    }
                    micClient.StartMicAndRecognition();

                    break;
                case ('2'):
                    Console.WriteLine("Long");
                    break;
            }

        }

        static void LogRecognitionStart(string recoSource, string recoLanguage, SpeechRecognitionMode recoMode)
        {
            Console.WriteLine("\n--- Start speech recognition using " + recoSource + " with " + recoMode + " mode in " + recoLanguage + " language ----\n\n");
        }

        static string SubscriptionKey
        {
            get
            {
                return subscriptionKey;
            }

            set
            {
                subscriptionKey = value;
            }
        }

        static MicrophoneRecognitionClient CreateMicrophoneRecoClient(SpeechRecognitionMode recoMode, string language, string subscriptionKey)
        {
            MicrophoneRecognitionClient micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                recoMode,
                language,
                subscriptionKey);

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            if (recoMode == SpeechRecognitionMode.ShortPhrase)
            {
                micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            }

            micClient.OnConversationError += OnConversationErrorHandler;

            return micClient;
        }

        static void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Console.WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
            Console.WriteLine("********* Microphone status: {0} *********", e.Recording);
            if (e.Recording)
            {
                Console.WriteLine("Please start speaking.");
            }
            Console.WriteLine();
        }

        static void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Console.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            Console.WriteLine("{0}", e.PartialResult);
            Console.WriteLine();
        }

        static void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Console.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

            FinalResponseEvent.Set();

            // we got the final result, so it we can end the mic reco.  No need to do this
            // for dataReco, since we already called endAudio() on it as soon as we were done
            // sending all the data.
            micClient.EndMicAndRecognition();

            // BUGBUG: Work around for the issue when cached _micClient cannot be re-used for recognition.
            micClient.Dispose();
            micClient = null;

            WriteResponseResult(e);

        }

        static void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                Console.WriteLine("No phrase resonse is available.");
            }
            else
            {
                Console.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    Console.WriteLine("[{0}] Confidence={1}, Text=\"{2}\"",
                                    i, e.PhraseResponse.Results[i].Confidence,
                                    e.PhraseResponse.Results[i].DisplayText);
                }
                Console.WriteLine();
            }
        }

        static void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {

            Console.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            Console.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            Console.WriteLine("Error text: {0}", e.SpeechErrorText);
            Console.WriteLine();
        }
    }
}
