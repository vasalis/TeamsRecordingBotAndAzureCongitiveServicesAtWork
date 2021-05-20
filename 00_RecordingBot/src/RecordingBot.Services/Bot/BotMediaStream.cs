// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="BotMediaStream.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>The bot media stream.</summary>
// ***********************************************************************-

using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using Microsoft.Skype.Internal.Media.Services.Common;
using RecordingBot.Services.Contract;
using RecordingBot.Services.Media;
using RecordingBot.Services.ServiceSetup;
using RecordingBot.Services.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace RecordingBot.Services.Bot
{
    /// <summary>
    /// Class responsible for streaming audio and video.
    /// </summary>
    public class BotMediaStream : ObjectRootDisposable
    {
        /// <summary>
        /// The participants
        /// </summary>
        internal List<IParticipant> participants;

        /// <summary>
        /// The audio socket
        /// </summary>
        private readonly IAudioSocket _audioSocket;
        /// <summary>
        /// The media stream
        /// </summary>
        private readonly IMediaStream _mediaStream;
        /// <summary>
        /// The event publisher
        /// </summary>
        private readonly IEventPublisher _eventPublisher;

        /// <summary>
        /// The settings
        /// </summary>
        private readonly AzureSettings _settings;

        /// <summary>
        /// The call identifier
        /// </summary>
        private readonly string _callId;

        private Dictionary<uint, MySTT> mSpeechToTextPool = new Dictionary<uint, MySTT>();

        /// <summary>
        /// Return the last read 'audio quality of experience data' in a serializable structure
        /// </summary>
        /// <value>The audio quality of experience data.</value>
        public SerializableAudioQualityOfExperienceData AudioQualityOfExperienceData { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotMediaStream" /> class.
        /// </summary>
        /// <param name="mediaSession">he media session.</param>
        /// <param name="callId">The call identity</param>
        /// <param name="logger">The logger.</param>
        /// <param name="eventPublisher">Event Publisher</param>
        /// <param name="settings">Azure settings</param>
        /// <exception cref="InvalidOperationException">A mediaSession needs to have at least an audioSocket</exception>
        public BotMediaStream(
            ILocalMediaSession mediaSession,
            string callId,
            IGraphLogger logger,
            IEventPublisher eventPublisher,
            IAzureSettings settings
        )
            : base(logger)
        {
            ArgumentVerifier.ThrowOnNullArgument(mediaSession, nameof(mediaSession));
            ArgumentVerifier.ThrowOnNullArgument(logger, nameof(logger));
            ArgumentVerifier.ThrowOnNullArgument(settings, nameof(settings));

            this.participants = new List<IParticipant>();

            _eventPublisher = eventPublisher;
            _callId = callId;
            _settings = (AzureSettings)settings;
            _mediaStream = new MediaStream(
                settings,
                logger,
                mediaSession.MediaSessionId.ToString()
            );

            // Subscribe to the audio media.
            this._audioSocket = mediaSession.AudioSocket;
            if (this._audioSocket == null)
            {
                throw new InvalidOperationException("A mediaSession needs to have at least an audioSocket");
            }

            this._audioSocket.AudioMediaReceived += this.OnAudioMediaReceived;
        }

        /// <summary>
        /// Gets the participants.
        /// </summary>
        /// <returns>List&lt;IParticipant&gt;.</returns>
        public List<IParticipant> GetParticipants()
        {
            return participants;
        }

        /// <summary>
        /// Gets the audio quality of experience data.
        /// </summary>
        /// <returns>SerializableAudioQualityOfExperienceData.</returns>
        public SerializableAudioQualityOfExperienceData GetAudioQualityOfExperienceData()
        {
            AudioQualityOfExperienceData = new SerializableAudioQualityOfExperienceData(this._callId, this._audioSocket.GetQualityOfExperienceData());
            return AudioQualityOfExperienceData;
        }

        /// <summary>
        /// Stops the media.
        /// </summary>
        public async Task StopMedia()
        {
            await _mediaStream.End();
            // Event - Stop media occurs when the call stops recording
            _eventPublisher.Publish("StopMediaStream", "Call stopped recording");
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            // Event Dispose of the bot media stream object
            _eventPublisher.Publish("MediaStreamDispose", disposing.ToString());

            base.Dispose(disposing);

            this._audioSocket.AudioMediaReceived -= this.OnAudioMediaReceived;
        }

        /// <summary>
        /// Receive audio from subscribed participant.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The audio media received arguments.</param>
        private void OnAudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
        {
            this.GraphLogger.Info($"Received Audio: [AudioMediaReceivedEventArgs(Data=<{e.Buffer.Data.ToString()}>, Length={e.Buffer.Length}, Timestamp={e.Buffer.Timestamp})]");

            try
            {
                if (e.Buffer != null && e.Buffer.UnmixedAudioBuffers != null)
                {
                    for (int i = 0; i < e.Buffer.UnmixedAudioBuffers.Length; i++)
                    {
                        // Transcribe
                        var lTrans = this.GetSTTEngine(e.Buffer.UnmixedAudioBuffers[i].ActiveSpeakerId);
                        if (lTrans != null)
                        {
                            lTrans.Transcribe(e.Buffer.UnmixedAudioBuffers[i]);
                        }
                    }
                }

                // await _mediaStream.AppendAudioBuffer(e.Buffer, this.participants);
                e.Buffer.Dispose();
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex);
            }
            finally
            {
                e.Buffer.Dispose();
            }

        }

        private MySTT GetSTTEngine(uint aUserId)
        {
            try
            {
                if (this.mSpeechToTextPool.ContainsKey(aUserId))
                {
                    var lexit = this.mSpeechToTextPool[aUserId];

                    if (!lexit.IsParticipantResolved)
                    {
                        // Try to resolved again
                        var lParticipantInfo = this.TryToResolveParticipant(aUserId);

                        //Update if resolved.
                        if (lParticipantInfo != null && lParticipantInfo.Item3)
                        {
                            lexit.UpdateParticipant(lParticipantInfo);
                        }
                    }

                    return lexit;
                }
                else
                {
                    var lParticipantInfo = this.TryToResolveParticipant(aUserId);

                    if (lParticipantInfo != null)
                    {
                        var lNewSE = new MySTT(this._callId, lParticipantInfo.Item1, lParticipantInfo.Item2, lParticipantInfo.Item3, this.GraphLogger, this._eventPublisher, this._settings);
                        this.mSpeechToTextPool.Add(aUserId, lNewSE);

                        return lNewSE;
                    }
                    else
                    {
                        this.GraphLogger.Error($"Initilizing GetSTTEngine failed for userid: {aUserId} -> couldn't get participant info.");
                    }
                }
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"GetSTTEngine failed for userid: {aUserId}. Details: {ex.Message}");                
            }

            return null;
        }

        private Tuple<String, String, Boolean> TryToResolveParticipant(uint aUserId)
        {
            try
            {
                bool lIsParticipantResolved = false;
                string lUserDisplayName = aUserId.ToString();
                string lUserId = aUserId.ToString();

                IParticipant participant = this.GetParticipantFromMSI(aUserId);
                var participantDetails = participant?.Resource?.Info?.Identity?.User;

                if (participantDetails != null)
                {
                    lUserDisplayName = participantDetails.DisplayName;
                    lUserId = participantDetails.Id;
                    lIsParticipantResolved = true;
                }

                return new Tuple<string, string, bool>(lUserDisplayName, lUserId, lIsParticipantResolved);
            }
            catch (Exception ex)
            {
                this.GraphLogger.Error(ex, $"TryToResolveParticipant failed for userid: {aUserId}. Details: {ex.Message}");
            }

            return null;
        }

        private IParticipant GetParticipantFromMSI(uint msi)
        {
            return this.participants.SingleOrDefault(x => x.Resource.IsInLobby == false && x.Resource.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }
    }
}
