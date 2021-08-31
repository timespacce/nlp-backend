using AdessoVR.Model.Conversation;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using AdessoVR.Interpretation.API;

namespace AdessoVR
{
    namespace Interpretation
    {
        namespace Core
        {
            public class InterpretationEngine : AdessoVR.Interpretation.API.IInterpretationEngine
            {
                ///

                private List<Phrase> all_phrases;

                private Dictionary<string, Phrase> all_phrases_map;

                private List<Task> all_tasks;

                private Dictionary<string, Task> all_tasks_map;

                private List<TransitionEvent> all_transition_events;

                private Dictionary<string, TransitionEvent> all_transition_events_map;

                private List<Stage> all_stages;

                private Dictionary<string, Stage> all_stages_map;

                private List<Level> all_levels;

                private int task_dim;

                private int phrase_dim;

                private int keyword_dim;

                private int token_dim;

                private int level_dim;

                private int stage_dim;

                private int steps_dim;

                private int options_dim;

                ///

                // [: , : , 0 , 0] -> TRANSITION EVENTS FOUND
                private const int EF = 0;

                // [: , : , 0 , 1] -> GLOBAL STEP INDEX
                private const int GI = 1;

                // [: , : , 0 , 2] -> GLOBAL STEP INDEX STATUS
                private const int GS = 2;

                // [: , : , 0 , 3] -> LOCAL STEP INDEX
                private const int LI = 3;

                // [: , : , 0 , 4] -> LOCAL OPTION INDEX
                private const int OI = 4;

                // [: , : , 0 , 5] -> STAGE COMPLETION STATUS
                private const int CS = 5;

                ///

                private string transcription_sequence = "";

                private List<string> tokenized_transcription_sequence = new List<string>();

                /// [Y x X x Z x V]
                private int[,,,] estimation_tensor;

                /// [Q x W x R x O]
                private int[,,,] stage_tensor;

                ///

                private InterpretationEngineConfig config = new InterpretationEngineConfig();

                public InterpretationEngineConfig GetConfig() { return config; }

                ///

                public void SetPhrases(ref List<Phrase> phrases)
                {
                    this.all_phrases = phrases;
                    int phrase_index = 0;
                    this.all_phrases.ForEach(x => x.index = phrase_index++);
                    this.all_phrases_map = this.all_phrases.ToDictionary(x => x.name);
                }

                public ref List<Phrase> GetAllPhrases()
                {
                    return ref this.all_phrases;
                }

                public void SetTasks(ref List<Task> tasks)
                {
                    all_tasks = tasks;
                    all_tasks.ForEach(x => x.phrases_names.ForEach(y => x.phrases.Add(all_phrases_map[y])));
                    int task_index = 0;
                    all_tasks.ForEach(x => x.index = task_index++);
                    all_tasks_map = all_tasks.ToDictionary(x => x.name);
                }

                public ref List<Task> GetAllTasks()
                {
                    return ref this.all_tasks;
                }

                public void SetTransitionEvents(ref List<TransitionEvent> transition_events)
                {
                    this.all_transition_events = transition_events;
                    this.all_transition_events_map = this.all_transition_events.ToDictionary(x => x.name);
                }

                public ref List<TransitionEvent> GetAllTransitionEvents()
                {
                    return ref this.all_transition_events;
                }

                public void SetStages(ref List<Stage> stages)
                {
                    this.all_stages = stages;

                    void InitializeStep(ref Stage stage, string descriptor)
                    {
                        MatchCollection regex_match = Regex.Matches(descriptor, "[\\w_-]* => [\\w_-]*");
                        List<string> actions = regex_match.Cast<Match>().Select(e => e.Value).ToList();

                        Step step = new Step();
                        List<Option> options = new List<Option>();

                        void map_task_to_transition_event(string action)
                        {
                            string[] match = Regex.Split(action, " => ");
                            string task_name = match[0];
                            string transition_event_name = match[1];
                            Task task = this.all_tasks_map[task_name];
                            TransitionEvent transition_event = this.all_transition_events_map[transition_event_name];
                            Tuple<Task, TransitionEvent> tuple = new Tuple<Task, TransitionEvent>(task, transition_event);
                            Option option = new Option
                            {
                                task_to_transition_event = tuple
                            };
                            step.options.Add(option);
                        }
                        actions.ForEach(e => map_task_to_transition_event(e));

                        stage.steps.Add(step);
                    }

                    void InitializeStage(Stage stage)
                    {
                        List<string> descriptor = stage.descriptor;
                        descriptor.ForEach(e => InitializeStep(ref stage, e));
                    }

                    this.all_stages.ForEach(e => InitializeStage(e));

                    this.all_stages_map = this.all_stages.ToDictionary(x => x.name);
                }

                public ref List<Stage> GetAllStages()
                {
                    return ref this.all_stages;
                }

                public void SetLevels(ref List<Level> levels)
                {
                    this.all_levels = levels;

                    void InitializeLevel(Level level)
                    {
                        level.stages_names.ForEach(e => level.stages.Add(all_stages_map[e]));
                    }

                    this.all_levels.ForEach(e => InitializeLevel(e));
                }

                public ref List<Level> GetAllLevels()
                {
                    return ref this.all_levels;
                }

                ///

                public void SetTranscriptionSequence(string transcription_sequence)
                {
                    this.transcription_sequence = transcription_sequence;
                    tokenized_transcription_sequence = this.transcription_sequence.Split(' ').ToList();
                }

                public ref string GetTranscriptionSequence()
                {
                    return ref this.transcription_sequence;
                }

                public void SetTensors()
                {
                    ///

                    task_dim = all_tasks.Count;

                    phrase_dim = all_tasks.Aggregate(-1, (agg, x) => Math.Max(agg, x.phrases.Count));

                    /// keyword_dim[0] ~ keyword ? match
                    keyword_dim = 1 + all_phrases.Aggregate(-1, (agg, x) => Math.Max(agg, x.keywords.Count));

                    int string_agg(List<string> seq) => seq.Aggregate(-1, (agg, x) => Math.Max(agg, x.Split(' ').Length));

                    /// token_dim[0] ~ token ? match
                    token_dim = 1 + all_phrases.Aggregate(-1, (agg_x, x) => Math.Max(agg_x, string_agg(x.keywords.ToList())));

                    ///

                    int[] shape_dims = new int[] { phrase_dim, keyword_dim, token_dim };

                    int[] estimation_dims = new int[] { task_dim, phrase_dim, keyword_dim, token_dim };

                    Console.WriteLine("ESTIMATION SHAPE Y X Z V = ({0} {1} {2} {3})", task_dim, phrase_dim, keyword_dim, token_dim);

                    estimation_tensor = new int[task_dim, phrase_dim, keyword_dim, token_dim];

                    ///

                    /// [META] [1] [2] [3] [4]
                    level_dim = 1 + all_levels.Count;

                    stage_dim = all_stages.Count;

                    steps_dim = 1 + all_stages.Aggregate(-1, (agg, x) => Math.Max(agg, x.steps.Count));

                    int step_agg(List<Step> steps) => steps.Aggregate(-1, (agg, e) => Math.Max(agg, e.options.Count));

                    /// [EF] [GI] [GS] [LI] [OI] [CS]
                    options_dim = 6 + all_stages.Aggregate(-1, (agg, x) => Math.Max(agg, step_agg(x.steps)));
                    options_dim = Math.Max(options_dim, level_dim);

                    ///

                    int[] stage_dims = new int[] { level_dim, stage_dim, steps_dim, options_dim };

                    Console.WriteLine("STAGE SHAPE Q W R O = ({0} {1} {2} {3})", level_dim, stage_dim, steps_dim, options_dim);

                    stage_tensor = new int[level_dim, stage_dim, steps_dim, options_dim];

                    ///

                    stage_tensor[0, 0, 0, 1] = 1;
                }

                public ref int[,,,] GetEstimationTensor()
                {
                    return ref this.estimation_tensor;
                }

                public ref int[,,,] GetStageTensor()
                {
                    return ref this.stage_tensor;
                }

                ///

                private InterpretationResult interpretation_result = new InterpretationResult();

                public InterpretationResult Interpret(string transcription_sequence)
                {
                    interpretation_result.matched_transition_events = new List<TransitionEvent>();

                    if (GetConfig().status != InterpretationStatus.RUNNING) return interpretation_result;

                    SetTranscriptionSequence(transcription_sequence);

                    List<Task> matched_tasks = CrossTasksWithTranscriptionSequence();

                    CommandSearchOutput delta = CrossCommandsWithMatchedTasks(ref matched_tasks);

                    CrossLevelsWithMatchedTasks(ref matched_tasks, delta);

                    interpretation_result = BuildInterpretationResult(delta);

                    ClearWorkspace(ref matched_tasks);

                    return interpretation_result;
                }

                public List<Task> CrossTasksWithTranscriptionSequence()
                {
                    List<Task> matched_tasks = new List<Task>();

                    int CrossWordWithToken(string word, string token, int y, int x, int z, int v)
                    {
                        /// [y , x , z , v] - GLOBAL MATCH ([TASK, PHRASE, KEYWORD, WORD])

                        string accessor = y + "," + x + "," + z + "," + v;

                        /// @Invariant : In previous token matched.
                        if (estimation_tensor[y, x, z, v] == 1) return 1;

                        bool word_match = word.ToLower() == token.ToLower();

                        /// @Invariant : With this token for first time matched.
                        if (word_match)
                        {
                            estimation_tensor[y, x, z, v] = 1;
                            return 1;
                        }

                        /// @Invariant : Not matched yet.

                        return 0;
                    }

                    bool CrossKeywordWithToken(List<string> keyword, string token, int y, int x, int z)
                    {
                        /// @Invariant : phrase 'x' was matched in previous iteration => accumulated_score >= keyword.Count
                        int accumulated_score = estimation_tensor[y, x, 0, 0] * keyword.Count;

                        if (accumulated_score >= keyword.Count) return true;

                        int word_index = 1;

                        /// @Invariant : keyword 'z' is matched in current iteration => word_score >= keyword.Count
                        int word_score = keyword.Aggregate(0, (agg, e) => agg += CrossWordWithToken(e, token, y, x, z, word_index++));

                        bool keyword_match = word_score >= keyword.Count;

                        if (keyword_match == false) return false;

                        estimation_tensor[y, x, 0, 0] = 1;

                        return true;
                    }

                    bool CrossPhraseWithToken(ref Phrase phrase, string token, int y, int x)
                    {
                        int keyword_index = 1;

                        /// @Invariant : At least one keyword is matched => phrase_match.
                        bool phrase_match = phrase.words.Aggregate(false, (agg, e) => agg |= CrossKeywordWithToken(e, token, y, x, keyword_index++));

                        return phrase_match;
                    }

                    void CrossTaskWithToken(ref Task task, string token, int y)
                    {
                        /// [y , : , 0 , 0] - GLOBAL MATCH OF EACH PHRASE OF TASK 'y'

                        int p_index = 0;

                        /// @Invariant : Each phrase of 'task' is matched => task_match.
                        bool task_match = task.phrases.Aggregate(true, (agg, e) => agg &= CrossPhraseWithToken(ref e, token, y, p_index++));

                        if (task_match == false) return;

                        bool multiple_occurance = matched_tasks.Contains(task);

                        if (multiple_occurance) return;

                        /// @Invariant : No Repeatance 

                        matched_tasks.Add(task);

                        /// @Invariant : Order is same as in tokenized sequence.
                    }

                    void CrossTasksWithToken(string token)
                    {
                        this.all_tasks.ForEach(e => CrossTaskWithToken(ref e, token, e.index));
                    }

                    /// Y X Z V
                    this.tokenized_transcription_sequence.ForEach(e => CrossTasksWithToken(e));

                    return matched_tasks;
                }

                public CommandSearchOutput CrossCommandsWithMatchedTasks(ref List<Task> matched_tasks)
                {
                    CommandSearchOutput delta = new CommandSearchOutput();

                    Dictionary<string, Int32> s_map = new Dictionary<string, Int32>
                    {
                        ["ES-RETURN"] = -1,
                        ["ES-REPEAT"] = 0,
                        ["ES-PROCEED"] = 1
                    };

                    Dictionary<string, Int32> c_map = new Dictionary<string, Int32>
                    {
                        ["ES-FORWARD"] = 1
                    };

                    CommandSearchOutput aggregate(CommandSearchOutput acc, Task task)
                    {
                        string task_name = task.name;

                        if (s_map.ContainsKey(task_name)) acc.skip_step = s_map[task_name];

                        if (c_map.ContainsKey(task_name)) acc.skip_stage = c_map[task_name];

                        return acc;
                    }

                    delta = matched_tasks.Aggregate(delta, (acc, e) => aggregate(acc, e));

                    delta.skip = delta.skip_step != null || delta.skip_stage != null;

                    return delta;
                }

                public void CrossLevelsWithMatchedTasks(ref List<Task> matched_tasks, CommandSearchOutput delta)
                {
                    Tuple<bool, int> CrossOptionWithTask(Tuple<bool, int> aggregate, Option option, int o, int o_max, Task task, bool[] skip_mask)
                    {
                        bool found = aggregate.Item1;

                        if (found) return aggregate;

                        /// @Invariant: No option yet matches task. 

                        // bool option_skip = (o == o_max && skip_mask[2]) || skip_mask[1];
                        bool option_skip = skip_mask[1];

                        bool option_match = (option.task_to_transition_event.Item1.name == task.name) || option_skip;

                        /// @Invariant: 
                        ///     Select last option only if 'STAGE_TO_STAGE' is requested.
                        ///     Select next option only if 'STEP_TO_STEP' is requested.

                        if (option_match == false) return aggregate;

                        task.correct = true;

                        /// @Invariant: Option 'o' matches task. 

                        return new Tuple<bool, int>(true, o);
                    }

                    Tuple<bool, int, int> CrossStepWithTask(Tuple<bool, int, int> aggregate, Step step, int current_step_id, Task task, bool[] skip_mask)
                    {
                        bool found = aggregate.Item1;

                        if (found) return aggregate;

                        /// @Invariant : No step in current stage matched 'task' yet.

                        Tuple<bool, int> initial = new Tuple<bool, int>(false, 0);

                        int option_id = 0;

                        int option_id_max = step.options.Count - 1;

                        Tuple<bool, int> match_option = step.options.Aggregate(initial, (agg, e) => CrossOptionWithTask(agg, e, option_id++, option_id_max, task, skip_mask));

                        found = match_option.Item1;

                        if (found == false) return aggregate;

                        int current_option_id = match_option.Item2;

                        /// @Invariant : Task 'current_step_id' and option 'current_option_id' match 'task'

                        return new Tuple<bool, int, int>(true, current_step_id, current_option_id);
                    }

                    void CrossStageWithTask(int q, Stage stage, int w, Task task)
                    {
                        /// [Q x W x R x O] -> SHAPE
                        /// [: , : , 0 , 0] -> TRANSITION EVENTS FOUND
                        /// [: , : , 0 , 1] -> GLOBAL STEP INDEX
                        /// [: , : , 0 , 2] -> GLOBAL STEP INDEX STATUS
                        /// [: , : , 0 , 3] -> LOCAL STEP INDEX
                        /// [: , : , 0 , 4] -> LOCAL OPTION INDEX
                        /// [: , : , 0 , 5] -> STAGE COMPLETION STATUS

                        int glo_step_id = stage_tensor[q, w, 0, GI];
                        int glo_step_rd = stage_tensor[q, w, 0, GS];
                        int loc_step_id = stage_tensor[q, w, 0, LI];

                        /// [0 .. #steps - 1]
                        int iter = glo_step_id;

                        if (glo_step_rd > 0) return;

                        /// @Invariant : Stage->Step[iter] is not yet modified.

                        /// [SKIP_STEP, STEP_TO_STEP, STAGE_TO_STAGE, STATUS]
                        bool[] skip_mask = new bool[4];

                        skip_mask[0] = delta.skip_step != null;
                        skip_mask[1] = iter >= 1 && skip_mask[0];
                        skip_mask[2] = skip_mask[1] == false && (delta.skip_stage != null);
                        skip_mask[3] = false;

                        int shift = skip_mask[1] ? delta.skip_step.Value : 1;
                        iter += skip_mask[1] ? delta.skip_step.Value : 1;

                        List<Step> context = stage.steps.GetRange(0, iter);

                        /// @Invariant : Moves forward by only one step pro iteration.

                        context.Reverse();

                        /// @Invariant : Steps with highest index have highest priority

                        context = context.GetRange(0, 1);

                        /// @Invariant : Previous steps can't be answered.

                        Tuple<bool, int, int> initial = new Tuple<bool, int, int>(false, 0, 0);

                        Tuple<bool, int, int> coordinate = context.Aggregate(initial, (agg, e) => CrossStepWithTask(agg, e, --iter, task, skip_mask));

                        bool found = coordinate.Item1;

                        if (found == false) return;

                        stage_tensor[q, w, 0, EF] = 1;

                        /// @Invariant : At least one option in at least one step matches task

                        int current_step_id = coordinate.Item2;
                        int current_option_id = coordinate.Item3;

                        if (current_step_id < loc_step_id) return;

                        stage_tensor[q, w, 0, LI] = current_step_id;
                        stage_tensor[q, w, 0, OI] = current_option_id;

                        /// @Invariant : 'current_step_id' has the highest score in local context

                        bool forward_match = current_step_id >= glo_step_id;

                        if (forward_match == false) return;

                        bool stage_completed = (stage_tensor[q, w, 0, GI] + shift) == stage.steps.Count;

                        if (stage_completed) stage_tensor[q, w, 0, CS] = 1; stage.passed = true;

                        /// @Invariant : Moves forward only if highest step is matched.
                        stage_tensor[q, w, 0, GI] = (stage_tensor[q, w, 0, GI] + shift) % stage.steps.Count;

                        /// @Invariant : Mark stage as modified
                        stage_tensor[q, w, 0, GS] = 1;
                    }

                    void CrossLevelWithTask(Level level, int q, Task task)
                    {
                        /// [0 , 0 , 0 , 0] -> LEVEL INDEX
                        int stage_index = 0;
                        level.stages.ForEach(e => CrossStageWithTask(q, e, stage_index++, task));
                    }

                    void CrossLevelsWithTask(Task task)
                    {
                        List<Level> context = this.all_levels.FindAll(e => stage_tensor[0, 0, 0, e.order] >= 1);
                        /// Q W R O
                        context.ForEach(e => CrossLevelWithTask(e, e.order, task));
                    }

                    matched_tasks.ForEach(e => CrossLevelsWithTask(e));
                }

                public InterpretationResult BuildInterpretationResult(CommandSearchOutput delta)
                {
                    List<Tuple<Task, TransitionEvent>> matched_steps = new List<Tuple<Task, TransitionEvent>>();

                    List<TransitionEvent> matched_transition_events = new List<TransitionEvent>();

                    bool level_completed = false, conversation_completed = false;

                    int level_score = -1, highest_step_id = 0;

                    void CollectTransitionEventFromStage(int q, Stage stage, int w)
                    {
                        int event_found = stage_tensor[q, w, 0, EF];

                        int glo_step_id = stage_tensor[q, w, 0, GI];

                        highest_step_id = Math.Max(highest_step_id, glo_step_id);

                        if (event_found <= 0) return;

                        int loc_step_id = stage_tensor[q, w, 0, LI];
                        int step_option = stage_tensor[q, w, 0, OI];

                        Tuple<Task, TransitionEvent> match = stage.steps[loc_step_id].options[step_option].task_to_transition_event;

                        matched_steps.Add(match);

                    }

                    void CollectTransitionEventFromLevel(Level level, int q)
                    {
                        int w = 0;

                        level.stages.ForEach(e => CollectTransitionEventFromStage(q, e, w++));

                        w = 0;

                        level_score = level.stages.Aggregate(0, (acc, e) => acc += stage_tensor[q, w++, 0, CS]);

                        level_completed = level_score >= level.stages.Count || delta.skip_stage != null;
                    }

                    List<Level> context = this.all_levels.FindAll(e => stage_tensor[0, 0, 0, e.order] >= 1);

                    int current_level_id = context.First().order;
                    int next_level_id = current_level_id;

                    /// @Invariant : only currently activated levels are observed and only one level is activated.

                    /// Q W R O
                    context.ForEach(e => CollectTransitionEventFromLevel(e, e.order));

                    if (level_completed)
                    {
                        stage_tensor[0, 0, 0, current_level_id] = 0;
                        next_level_id = (current_level_id + 1) % level_dim;
                        stage_tensor[0, 0, 0, next_level_id] = 1;
                    }

                    conversation_completed = current_level_id == level_dim - 1 && next_level_id == 0;

                    if (conversation_completed) GetConfig().status = InterpretationStatus.FINISHED;

                    matched_transition_events = matched_steps.Select(e => e.Item2).ToList();

                    bool correct_answer = matched_transition_events.Count > 0;
                    bool level_transition = level_completed;

                    InterpretationResult interpretation_result = new InterpretationResult
                    {
                        correct_answer = correct_answer,
                        level_transition = level_transition,
                        matched_transition_events = matched_transition_events,
                        current_level = next_level_id,
                        current_level_score = level_score,
                        current_stage = 0,
                        highest_step_id = highest_step_id
                    };

                    bool dont_skip = delta.skip_stage == null && delta.skip_step == null;
                    bool react_on_wrong_answer = interpretation_result.correct_answer == false && config.answer_on_wrong_answer && dont_skip;
                    bool interrupt = config.wrong_answer_count >= 2 && react_on_wrong_answer;

                    /// @Invariant: 'ES-REPEAT'     - if no task solved and no skip.
                    ///             'ES-INTERRUPT'  - if 3x 'ES-REPEAT'

                    if (interrupt)
                    {
                        interpretation_result.matched_transition_events.Add(all_transition_events_map["ES-INTERRUPT"]);
                        GetConfig().status = InterpretationStatus.INTERRUPTED;
                        return interpretation_result;
                    }

                    if (react_on_wrong_answer)
                    {
                        interpretation_result.matched_transition_events.Add(all_transition_events_map["ES-REPEAT"]);
                        config.wrong_answer_count += 1;
                        return interpretation_result;
                    }

                    config.wrong_answer_count = 0;

                    return interpretation_result;
                }

                public void ClearWorkspace(ref List<Task> matched_tasks)
                {
                    for (int q = 1; q < level_dim; q++)
                    {
                        for (int w = 0; w < stage_dim; w++)
                        {
                            /// @Invariant : At the beginning of each iteration are no events found. Pro iteration 
                            /// is at most one step pro stage completed.
                            stage_tensor[q, w, 0, EF] = 0;
                            stage_tensor[q, w, 0, GS] = 0;
                            /// @Invariant : At the beginning of each iteration are no steps or options found.
                            stage_tensor[q, w, 0, LI] = 0;
                            stage_tensor[q, w, 0, OI] = 0;
                        }
                    }

                    int task_dim_3d = phrase_dim * keyword_dim * token_dim;

                    int[,,] zero_task = new int[phrase_dim, keyword_dim, token_dim];

                    /// @Invariant : Tasks are matched at least once in global context.
                    matched_tasks.ForEach(e => Buffer.BlockCopy(zero_task, 0, estimation_tensor, e.index * task_dim_3d * sizeof(int), task_dim_3d * sizeof(int)));

                    int phrase_dim_2d = keyword_dim * token_dim;

                    int[] zero_keyword = new int[token_dim - 1];

                    const int separable_phrases = 1;

                    /// @Invariant : Phrases are separable, but keywords are not.
                    foreach (Task task in all_tasks)
                    {
                        int task_offset = task.index * task_dim_3d;
                        for (int i = 0; i < task.phrases.Count; i++)
                        {
                            int phrase_offset = i * phrase_dim_2d;
                            for (int j = 1; j <= task.phrases[i].keywords.Count; j++)
                            {
                                int keyword_offset = j * token_dim;
                                int offset = task_offset + phrase_offset + keyword_offset + separable_phrases;
                                Buffer.BlockCopy(zero_keyword, 0, estimation_tensor, offset * sizeof(int), (token_dim - 1) * sizeof(int));
                            }
                        }
                    }
                }


                ///

                public Feedback GetFeedback()
                {
                    Feedback feedback = new Feedback();

                    Level[] levels = new Level[level_dim - 1];

                    List<Level> context = GetAllLevels().GetRange(0, level_dim - 1);

                    context.CopyTo(levels);

                    ///

                    void CheckOptionForCorrectness(Option option)
                    {
                        Task task = option.task_to_transition_event.Item1;

                        bool task_correct = task.correct;

                        if (task_correct == false) return;

                        option.task_to_transition_event.Item1.correct = task_correct;
                    }

                    void CheckStepForCorrectness(Step step)
                    {
                        step.options.ForEach(e => CheckOptionForCorrectness(e));
                    }

                    void CheckStageForCorrectness(Stage stage)
                    {
                        stage.steps.ForEach(e => CheckStepForCorrectness(e));
                    }

                    void CheckSectionForCorrectness(Level section)
                    {
                        section.stages.ForEach(e => CheckStageForCorrectness(e));
                    }

                    /// Q x W x R x O
                    levels.ToList().ForEach(e => CheckSectionForCorrectness(e));

                    feedback.levels = levels.ToList();

                    return feedback;
                }
            }
        }
    }
}
