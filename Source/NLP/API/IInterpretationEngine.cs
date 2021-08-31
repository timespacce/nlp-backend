using System;
using System.Collections.Generic;
using AdessoVR.Model.Conversation;

namespace AdessoVR
{
    namespace Interpretation
    {
        namespace API
        {
            public class CommandSearchOutput
            {
                public Int32? skip_step;

                public Int32? skip_stage;

                public bool skip;
            }

            public enum InterpretationStatus
            {
                RUNNING,

                INTERRUPTED,

                FINISHED
            }

            public class InterpretationEngineConfig
            {
                public bool answer_on_wrong_answer = false;

                public int wrong_answer_count = 0;

                public InterpretationStatus status = InterpretationStatus.RUNNING;
            }

            public interface IInterpretationEngine
            {
                ///

                InterpretationEngineConfig GetConfig();

                ///

                void SetPhrases(ref List<Phrase> phrases);

                ref List<Phrase> GetAllPhrases();

                void SetTasks(ref List<Task> tasks);

                ref List<Task> GetAllTasks();

                void SetTransitionEvents(ref List<TransitionEvent> transition_events);

                ref List<TransitionEvent> GetAllTransitionEvents();

                void SetStages(ref List<Stage> stages);

                ref List<Stage> GetAllStages();

                void SetLevels(ref List<Level> levels);

                ref List<Level> GetAllLevels();

                ///

                void SetTranscriptionSequence(string transcription);

                ref string GetTranscriptionSequence();

                void SetTensors();

                ref int[,,,] GetEstimationTensor();

                ref int[,,,] GetStageTensor();

                ///

                InterpretationResult Interpret(string transcription_sequence);

                /// 1.
                List<Task> CrossTasksWithTranscriptionSequence();

                /// 2.
                CommandSearchOutput CrossCommandsWithMatchedTasks(ref List<Task> matched_tasks);

                /// 3.
                void CrossLevelsWithMatchedTasks(ref List<Task> matched_tasks, CommandSearchOutput delta);

                /// 4.
                InterpretationResult BuildInterpretationResult(CommandSearchOutput delta);

                /// 5.
                void ClearWorkspace(ref List<Task> matched_tasks);


                ///

                Feedback GetFeedback();
            }
        }
    }
}
