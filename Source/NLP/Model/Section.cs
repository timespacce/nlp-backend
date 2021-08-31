using System;
using System.Collections.Generic;


namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Section
            {
                public string name;

                public int order;

                public List<string> stages_names;

                [NonSerialized]
                public List<Stage> stages = new List<Stage>();
            }
        }
    }
}
