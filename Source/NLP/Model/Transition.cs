using System;
using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Transition
            {
                public AdessoVR.Model.Conversation.TransitionEvent transitionEvent;

                public string name;

                public float orderWeight;

                public List<string> trigger;

                public List<string> outcome;
            }
        }
    }
}
