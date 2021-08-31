using System;
using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Task
            {
                public string name;

                public List<string> phrases_names;

                public string header;

                public string positive_description;

                public string negative_description;

                public string image;

                ///

                [NonSerialized]
                public int index;

                [NonSerialized]
                public List<AdessoVR.Model.Conversation.Phrase> phrases = new List<AdessoVR.Model.Conversation.Phrase>();

                ///

                [NonSerialized]
                public bool correct;
            }
        }
    }
}
