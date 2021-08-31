using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Phrase
            {
                public string name;

                public List<string> keywords;

                public List<List<string>> words = new List<List<string>>();

                [NonSerialized]
                public int index;

                [OnDeserialized()]
                internal void OnDeserializedMethod(StreamingContext context)
                {
                    foreach (string keyword in keywords)
                    {
                        List<string> tokens = keyword.Split(' ').ToList();
                        words.Add(tokens);
                    }
                }
            }
        }
    }
}
