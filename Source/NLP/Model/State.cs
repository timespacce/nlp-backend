using System;
using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class State
            {
                public Collocutor collocutor;

                public List<Phrase> phrases;

                public State(Collocutor collocutor, List<Phrase> phrases)
                {
                    this.collocutor = collocutor;
                    this.phrases = phrases;
                }
            }
        }
    }
}