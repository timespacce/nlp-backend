using System;
using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Option
            {
                [NonSerialized]
                public Tuple<Task, TransitionEvent> task_to_transition_event;
            }

            [Serializable]
            public class Step
            {
                [NonSerialized]
                public List<Option> options = new List<Option>();

            }

            [Serializable]
            public class Stage
            {
                public string name;

                public List<string> descriptor;

                [NonSerialized]
                public bool passed;

                [NonSerialized]
                public List<Step> steps = new List<Step>();
            }
        }
    }
}
