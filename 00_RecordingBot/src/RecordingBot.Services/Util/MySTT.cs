using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingBot.Services.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Translation;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Skype.Bots.Media;
    using Newtonsoft.Json;
    using RecordingBot.Services.Contract;
    using RecordingBot.Services.ServiceSetup;

    /// <summary>
    /// Hello.
    /// </summary>
    public class MySTT : IDisposable
    {
        private const int SAMPLESPERSECOND = 16000;
        private const int BITSPERSAMPLE = 16;
        private const int NUMBEROFCHANNELS = 1;

        private readonly IEventPublisher _eventPublisher;

        private AudioConfig mAudioConfig;
        private TranslationRecognizer mTranslationRecognizer;
        private PushAudioInputStream mInputStream;
        private SpeechTranslationConfig mTransSpeechConfig;
        private string mCallId = string.Empty;
        private string mWho = string.Empty;
        private string mWhoId = string.Empty;
        private bool mInContinousRecMode = false;

        private string mFunctionsEndPoint = string.Empty;
        private HttpClient mHttpClient = new HttpClient();
        private TranscriptionEntity mCurrentTranscriptionObject;

        private readonly AzureSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySTT"/> class.
        /// </summary>
        /// <param name="aCallId">Name.</param>
        /// <param name="aWho">Name..</param>
        /// <param name="aWhoId">Name...</param>
        public MySTT(string aCallId, string aWho, string aWhoId, IGraphLogger logger, IEventPublisher eventPublisher, IAzureSettings settings)
        {
            try
            {
                _eventPublisher = eventPublisher;
                _settings = (AzureSettings)settings;

                this.mCallId = aCallId;
                this.mWho = aWho;
                this.mWhoId = aWhoId;

                this.SetupTranscriptionAndTranslationService();
                this.SetupPersistanceEndPoint();
            }
            catch (Exception ex)
            {
                _eventPublisher.Publish("MySTT Instantiation - Failed", $"{ex.Message}");
            }            
        }

        private void SetupTranscriptionAndTranslationService()
        {
            try
            {
                var lCognitiveKey = _settings.AzureCognitiveKey;
                var lCognitiveRegion = _settings.AzureCognitiveRegion;

                _eventPublisher.Publish("MySTT Setup", $"Got region: {lCognitiveRegion}, key starting from: {lCognitiveKey??lCognitiveKey.Substring(0, lCognitiveKey.Length /2)}");

                this.mTransSpeechConfig = SpeechTranslationConfig.FromSubscription(lCognitiveKey, lCognitiveRegion);
                var fromLanguage = "en-US";
                var toLanguages = new List<string> { "el-GR" };
                //var toLanguages = new List<string> { "ru-RU" };
                this.mTransSpeechConfig.SpeechRecognitionLanguage = fromLanguage;
                toLanguages.ForEach(this.mTransSpeechConfig.AddTargetLanguage);
                this.mInputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(SAMPLESPERSECOND, BITSPERSAMPLE, NUMBEROFCHANNELS));

                this.mAudioConfig = AudioConfig.FromStreamInput(this.mInputStream);
                this.mTranslationRecognizer = new TranslationRecognizer(this.mTransSpeechConfig, this.mAudioConfig);

                this.mTranslationRecognizer.Recognizing += this.MSpeechRecognizer_Recognizing;
                this.mTranslationRecognizer.Recognized += this.MSpeechRecognizer_Recognized;
                this.mTranslationRecognizer.SpeechEndDetected += this.MSpeechRecognizer_SpeechEndDetected;

                this.StartRecognisionIfNeeded();
            }
            catch (Exception ex)
            {
                _eventPublisher.Publish("MySTT Setup - Failed", $"Failed to initialize: {ex.Message}");                
            }            
        }

        private void SetupPersistanceEndPoint()
        {            
            this.mFunctionsEndPoint = _settings.PersistenceEndPoint;

            _eventPublisher.Publish("SetupPersistanceEndPoint", $"Got end point: {this.mFunctionsEndPoint}");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.mTranslationRecognizer.Recognizing -= this.MSpeechRecognizer_Recognizing;
            this.mTranslationRecognizer.Recognized -= this.MSpeechRecognizer_Recognized;
            this.mTranslationRecognizer.SpeechEndDetected -= this.MSpeechRecognizer_SpeechEndDetected;

            this.mInputStream.Dispose();
            this.mAudioConfig.Dispose();
            this.mTranslationRecognizer.Dispose();
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="aBuffer">Buffer.</param>
        public void Transcribe(UnmixedAudioBuffer aBuffer)
        {
            try
            {
                if (aBuffer.OriginalSenderTimestamp >= 0 && aBuffer.Length > 0)
                {
                    long lKey = aBuffer.OriginalSenderTimestamp;

                    // Start recognition if needed
                    this.StartRecognisionIfNeeded();

                    byte[] managedArray = new byte[aBuffer.Length];
                    var handler = aBuffer.Data;
                    int start = 0;
                    int length = (int)aBuffer.Length;
                    Marshal.Copy(handler, managedArray, start, length);

                    // Write to stream
                    this.mInputStream.Write(managedArray);
                }
            }
            catch (Exception ex)
            {
                // TODO
                throw ex;
            }
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">e.</param>
        private void MSpeechRecognizer_SpeechEndDetected(object sender, RecognitionEventArgs e)
        {   
            this.StopRecognition();
        }

        /// <summary>
        /// Hello.
        /// </summary>
        private void StartRecognisionIfNeeded()
        {
            if (!this.mInContinousRecMode)
            {
                this.mInContinousRecMode = true;
                this.mTranslationRecognizer.StartContinuousRecognitionAsync();
            }
        }

        /// <summary>
        /// Hello.
        /// </summary>
        private void StopRecognition()
        {
            this.mInContinousRecMode = false;
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="sender">sender.</param>
        /// <param name="e">e.</param>
        private void MSpeechRecognizer_Recognized(object sender, TranslationRecognitionEventArgs e)
        {
            var t = Task.Run(() => this.PersistAsync(e.Result.Text, e.Result.Translations));
            t.Wait();

            this.ResetCurrentTranscriptionEntity();
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="sender">sender.</param>
        /// <param name="e">e.</param>
        private void MSpeechRecognizer_Recognizing(object sender, TranslationRecognitionEventArgs e)
        {
            var t = Task.Run(() => this.PersistAsync(e.Result.Text, e.Result.Translations));
            t.Wait();
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="aText">Text.</param>
        /// <param name="aTranslations">Translations.</param>
        /// <returns>Task.</returns>
        private async Task PersistAsync(string aText, IReadOnlyDictionary<string, string> aTranslations)
        {
            try
            {
                TranscriptionEntity lItem = this.GetCurrentTranscriptionEntity();
                lItem.When = DateTime.UtcNow;
                lItem.Text = aText;

                if (aTranslations != null)
                {
                    foreach (var lItemTrans in aTranslations)
                    {
                        lItem.Translations = lItemTrans.Value;
                    }
                }

                var lPayload = new StringContent(JsonConvert.SerializeObject(lItem), Encoding.UTF8, "application/json");

                await this.mHttpClient.PostAsync(this.mFunctionsEndPoint, lPayload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _eventPublisher.Publish("PersistAsync", $"Erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Hello.
        /// </summary>
        /// <returns>hey.</returns>
        private TranscriptionEntity GetCurrentTranscriptionEntity()
        {
            if (this.mCurrentTranscriptionObject == null)
            {
                this.mCurrentTranscriptionObject = new TranscriptionEntity();
                this.mCurrentTranscriptionObject.Id = Guid.NewGuid().ToString();
                this.mCurrentTranscriptionObject.CallId = this.mCallId;
                this.mCurrentTranscriptionObject.Who = this.mWho;
                this.mCurrentTranscriptionObject.WhoId = this.mWhoId;
            }

            return this.mCurrentTranscriptionObject;
        }

        /// <summary>
        /// Hello.
        /// </summary>
        private void ResetCurrentTranscriptionEntity()
        {
            this.mCurrentTranscriptionObject = null;
        }
    }
}
