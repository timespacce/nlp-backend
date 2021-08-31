using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AdessoVR
{
    namespace Interpretation
    {
        namespace Core
        {
            public sealed class TranscriptionCollector
            {
                /// singleton pattern
                private static readonly TranscriptionCollector instance = new TranscriptionCollector();

                public static TranscriptionCollector Get()
                {
                    return instance;
                }

                /// transcription queue
                private volatile Queue<string> transcription_queue = new Queue<string>();

                /// sync transcription W/R
                private readonly object transcription_queue_mutex = new object();

                public void QueueTranscript(string transcript)
                {
                    lock (transcription_queue_mutex)
                    {
                        /// 1. case insensitive
                        transcript = transcript.ToLower();

                        /// 2. tokenization
                        transcript = Regex.Replace(transcript, "\\.+", " . ");
                        transcript = Regex.Replace(transcript, "\\!+", " ! ");
                        transcript = Regex.Replace(transcript, "\\?+", " ? ");
                        transcript = Regex.Replace(transcript, ", ", " , ");
                        transcript = Regex.Replace(transcript, " +", " ");

                        transcription_queue.Enqueue(transcript);
                    }
                }

                public string DequeueTranscript()
                {
                    string transcripted_sequence = "";
                    lock (transcription_queue_mutex)
                    {
                        while (transcription_queue.Count > 0)
                        {
                            transcripted_sequence += " " + transcription_queue.Dequeue();
                        }
                    }
                    return transcripted_sequence;
                }

            }
        }
    }
}
