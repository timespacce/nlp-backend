using System;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class TransitionEvent
            {
                public string name { get; set; }

                public string audio_clip { get; set; }

                public string ssml { get; set; }

                public string event_type { get; set; }

                public AdessoVR.Model.Conversation.GESTURE gesture { get; set; }

                public AdessoVR.Model.Conversation.EMOTION emotion { get; set; }
            }
        }
    }
}