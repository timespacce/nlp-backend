using System;
using System.Collections.Generic;

namespace AdessoVR
{
    namespace Model
    {
        namespace Conversation
        {
            [Serializable]
            public class Feedback
            {
                [NonSerialized]
                public List<Level> levels;

                ///

                public List<Task> GetTasks()
                {
                    List<Task> tasks = new List<Task>();

                    void CheckStepForCorrectness(Step step)
                    {
                        /// 1st option
                        Option option = step.options[0];

                        Task task = option.task_to_transition_event.Item1;

                        /// skip non relevant tips
                        if (task.header == null || tasks.Contains(task)) return;

                        tasks.Add(task);
                    }

                    void CheckTaskForCorrectness(Stage stage)
                    {
                        stage.steps.ForEach(r => CheckStepForCorrectness(r));
                    }

                    void CheckLevelForCorrectness(Level level) { level.stages.ForEach(w => CheckTaskForCorrectness(w)); }

                    // Q x W x R x O
                    levels.ForEach(CheckLevelForCorrectness);

                    return tasks;
                }


                public List<Stage> GetStages()
                {
                    List<Stage> stages = new List<Stage>();

                    void CheckLevelForCorrectness(Level level) { level.stages.ForEach(e => stages.Add(e)); }

                    // Q x W x R x O
                    levels.ForEach(CheckLevelForCorrectness);

                    return stages;
                }

                public List<Task> GetAllCorrectTasks()
                {
                    List<Task> tasks = new List<Task>();

                    void CheckOptionForCorrectness(Option option)
                    {
                        Task task = option.task_to_transition_event.Item1;

                        bool task_correct = task.correct;

                        if (task_correct == false) return;

                        tasks.Add(task);
                    }

                    void CheckStepForCorrectness(Step step)
                    {
                        step.options.ForEach(e => CheckOptionForCorrectness(e));
                    }

                    void CheckStageForCorrectness(Stage stage)
                    {
                        stage.steps.ForEach(e => CheckStepForCorrectness(e));
                    }

                    void CheckSectionForCorrectness(Level level)
                    {
                        level.stages.ForEach(e => CheckStageForCorrectness(e));
                    }

                    /// Q x W x R x O
                    levels.ForEach(e => CheckSectionForCorrectness(e));

                    return tasks;
                }
            }
        }
    }
}
