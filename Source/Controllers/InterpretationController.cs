using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ML_Interpretation_Engine.Source;
using ML_Interpretation_Engine.Source.Requests;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
namespace ML_Interpretation_Engine.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InterpretationController : ControllerBase
    {

        private readonly ILogger<InterpretationController> logger;

        private IUserService user_service;

        public static Dictionary<string, AdessoVR.Interpretation.API.IInterpretationEngine> username_to_engine = new Dictionary<string, AdessoVR.Interpretation.API.IInterpretationEngine>();

        public InterpretationController(ILogger<InterpretationController> logger, IUserService user_service)
        {
            this.logger = logger;
            this.user_service = user_service;
        }

        private void InitializeInterpretationEngine(string username)
        {
            AdessoVR.Global.Configuration c = AdessoVR.Global.Configuration.Get();

            c.SetLanguageModelPath(AppDomain.CurrentDomain.BaseDirectory + "/Resources/NLP/Insurance/");

            username_to_engine[username] = new AdessoVR.Interpretation.Core.InterpretationEngine();

            AdessoVR.Interpretation.API.IInterpretationEngine engine = username_to_engine[username];

            ///

            StreamReader phrase_reader = new StreamReader(c.phrases);

            string phrases_json = phrase_reader.ReadToEnd();

            List<AdessoVR.Model.Conversation.Phrase> phrases = JsonConvert.DeserializeObject<List<AdessoVR.Model.Conversation.Phrase>>(phrases_json);

            StreamReader task_reader = new StreamReader(c.tasks);

            string tasks_json = task_reader.ReadToEnd();

            List<AdessoVR.Model.Conversation.Task> tasks = JsonConvert.DeserializeObject<List<AdessoVR.Model.Conversation.Task>>(tasks_json);

            StreamReader transition_event_reader = new StreamReader(c.transition_events);

            string transition_events_json = transition_event_reader.ReadToEnd();

            List<AdessoVR.Model.Conversation.TransitionEvent> transition_events = JsonConvert.DeserializeObject<List<AdessoVR.Model.Conversation.TransitionEvent>>(transition_events_json);

            StreamReader stage_reader = new StreamReader(c.stages);

            string stages_json = stage_reader.ReadToEnd();

            List<AdessoVR.Model.Conversation.Stage> stages = JsonConvert.DeserializeObject<List<AdessoVR.Model.Conversation.Stage>>(stages_json);

            StreamReader level_reader = new StreamReader(c.levels);

            string levels_json = level_reader.ReadToEnd();

            List<AdessoVR.Model.Conversation.Level> levels = JsonConvert.DeserializeObject<List<AdessoVR.Model.Conversation.Level>>(levels_json);

            ///

            engine.SetPhrases(ref phrases);
            engine.SetTasks(ref tasks);
            engine.SetTransitionEvents(ref transition_events);
            engine.SetStages(ref stages);
            engine.SetLevels(ref levels);
            engine.SetTensors();

            engine.GetConfig().answer_on_wrong_answer = true;

            logger.LogInformation($"InterpretationEngine for {username} : Initialized");
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] User user)
        {
            logger.LogInformation($"POST : Login : {user.username} : INITIATE");

            var token = user_service.Login(user.username, user.password);

            if (token == null || token == String.Empty) return BadRequest(new { message = "username or password is incorrect" });

            logger.LogInformation($"POST : Login : {user.username} : SUCCESSFUL");

            InitializeInterpretationEngine(user.username);

            return Ok(token);
        }

        [Authorize(Roles = "admin,guest")]
        [HttpPost]
        [Route("interpretate")]
        public Object Interpretate([FromBody] Data.TranscriptionFragment transcription_fragment)
        {
            string username = User.Identity.Name;
            string transcription = transcription_fragment.transcription;
            ///
            logger.LogInformation($"POST - Interpretate - {username} - {transcription}");
            bool username_is_logged_in = username_to_engine.ContainsKey(username);
            if (username_is_logged_in == false)
            {
                return BadRequest(new { message = "username isn't logged in yet." });
            }
            if (transcription == null)
            {
                return BadRequest(new { message = "request isn't in accepted layout" });
            }
            AdessoVR.Interpretation.API.IInterpretationEngine engine = username_to_engine[username];
            ///
            AdessoVR.Model.Conversation.InterpretationResult result = engine.Interpret(transcription);
            return result;
        }
    }
}
