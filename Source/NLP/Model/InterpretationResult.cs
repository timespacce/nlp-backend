using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            public class InterpretationResult
            {
                public bool correct_answer { get; set; }

                public bool level_transition { get; set; }

                public List<TransitionEvent> matched_transition_events { get; set; }

                /// [1 .. MAX_LEVELS]
                public int current_level { get; set; }

                public int current_level_score { get; set; }

                /// [0 .. MAX_STAGES] in 'current_level'
                public int current_stage { get; set; }

                /// [0 .. MAX_STEP] in 'current_stage'
                public int highest_step_id { get; set; }
            }
        }
    }
}
