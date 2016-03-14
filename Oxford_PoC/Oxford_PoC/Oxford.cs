using System;
using System.Configuration;
using Microsoft.ProjectOxford.SpeechRecognition;
using System.Threading;

namespace Oxford_PoC
{
    class Oxford
    {
        //Project Oxford Lizenz Key
        private string subscriptionKey = ConfigurationManager.AppSettings["primaryKey"];

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
        private string recoLanguage = ConfigurationManager.AppSettings["language"];

        //Mikrofon als Soundquelle
        private MicrophoneRecognitionClient micClient;

        //Eventhandler für Finale antwort von Microsoft
        private AutoResetEvent FinalResponseEvent;

        public Oxford()
        {
            //Response Event von MAIS
            FinalResponseEvent = new AutoResetEvent(false);

            //Starte das Logging für Shortphase Übertragungen
            LogRecognitionStart("microphone", recoLanguage, SpeechRecognitionMode.ShortPhrase);

            //Erstelle den Mic Client wenn er noch nicht vorhanden ist
            if (micClient == null)
            {
                //Mic Client für ShortPhrase in Deutsch mit SubscriptionKey
                micClient = CreateMicrophoneRecoClient(SpeechRecognitionMode.ShortPhrase, recoLanguage, SubscriptionKey);
            }
            //Starten der Aufnahme
            micClient.StartMicAndRecognition();
        }

        //Einfache Klasse für das starten des Loggings
        static void LogRecognitionStart(string recoSource, string recoLanguage, SpeechRecognitionMode recoMode)
        {
            Console.WriteLine("\n--- Start speech recognition using " + recoSource + " with " + recoMode + " mode in " + recoLanguage + " language ----\n\n");
        }


        //Get & Set für den SubscriptionKey
        private string SubscriptionKey
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

        //erzeugen des Mic Clients inklusiver der veschiedenen Result EventHandlers
        private MicrophoneRecognitionClient CreateMicrophoneRecoClient(SpeechRecognitionMode recoMode, string language, string subscriptionKey)
        {
            MicrophoneRecognitionClient micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                recoMode,
                language,
                subscriptionKey);

            // Event handlers für die Spracherkennung Ergebnisse
            //Mikrofon bereit?
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;

            //Partielle Resultate der Spracherkennung 
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            if (recoMode == SpeechRecognitionMode.ShortPhrase)
            {
                //Eventhandler für die erkannte Spracherkennung (Bestes Ergebnis)
                micClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            }

            //Fehlerhandler
            micClient.OnConversationError += OnConversationErrorHandler;

            return micClient;
        }

        //Eventhandlerausgabe Wenn Aufnahme starten kann
        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Console.WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
            Console.WriteLine("********* Microphone status: {0} *********", e.Recording);
            if (e.Recording)
            {
                Console.WriteLine("Please start speaking.");
            }
            Console.WriteLine();
        }

        //Eventhandler für Partielle Ergebnisse empfangen von Microsoft Azure
        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            Console.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            Console.WriteLine("{0}", e.PartialResult);
            Console.WriteLine();
        }

        //Eventhandler für Finales Ergebnis (Bestes Ergebnis) und wegwerfen des Mic Clients (Bug?)
        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Console.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

            FinalResponseEvent.Set();

            // Nachdem erhalt der finalen Antwort können wir die Mikrofonaufnahme beenden
            micClient.EndMicAndRecognition();

            //Finale Antwort schreiben
            WriteResponseResult(e);

        }
        //Methode zum schreiben der Finalen Antwort
        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            //Wenn Null dann nichts erkannt/gesprochen
            //Wenn nicht Null 
            //e.PhraseResponse[0].Confidence - Wahrscheinlichkeit das das richtige verstanden wurde
            //e.PhraseResponse[0].DisplayText - Die Wahrscheinlich gesprochenen Worte
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

        //Handler für Fehler
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {

            Console.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            Console.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            Console.WriteLine("Error text: {0}", e.SpeechErrorText);
            Console.WriteLine();
        }
    }
}
