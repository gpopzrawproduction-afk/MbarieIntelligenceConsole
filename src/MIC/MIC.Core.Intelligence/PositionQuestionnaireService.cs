using MIC.Core.Domain.Entities;

namespace MIC.Core.Intelligence
{
    /// <summary>
    /// Service for managing position-based questionnaires and recommendations
    /// based on the organizational structure
    /// </summary>
    public class PositionQuestionnaireService
    {
        /// <summary>
        /// Gets the appropriate questionnaire based on the user's position
        /// </summary>
        /// <param name="position">Job position from the organogram</param>
        /// <returns>List of questions relevant to the position</returns>
        public async Task<List<Question>> GetQuestionnaireForPositionAsync(string position)
        {
            var questions = new List<Question>();
            
            // Define position categories based on the organogram
            var positionCategory = GetPositionCategory(position);
            
            switch (positionCategory)
            {
                case PositionCategory.Executive:
                    questions.AddRange(GetExecutiveQuestions());
                    break;
                    
                case PositionCategory.Management:
                    questions.AddRange(GetManagementQuestions());
                    break;
                    
                case PositionCategory.Technical:
                    questions.AddRange(GetTechnicalQuestions());
                    break;
                    
                case PositionCategory.Operational:
                    questions.AddRange(GetOperationalQuestions());
                    break;
                    
                case PositionCategory.Support:
                    questions.AddRange(GetSupportQuestions());
                    break;
                    
                default:
                    questions.AddRange(GetGeneralQuestions());
                    break;
            }
            
            // Add position-specific questions
            questions.AddRange(GetPositionSpecificQuestions(position));
            
            return await Task.FromResult(questions);
        }
        
        /// <summary>
        /// Generates efficiency recommendations based on position and answers
        /// </summary>
        /// <param name="position">Job position</param>
        /// <param name="answers">Answers to the questionnaire</param>
        /// <returns>List of efficiency recommendations</returns>
        public async Task<List<string>> GenerateRecommendationsAsync(string position, Dictionary<string, string> answers)
        {
            var recommendations = new List<string>();
            var processor = new IntelligenceProcessor();
            
            // Generate recommendations based on position
            var positionRecommendations = await processor.GenerateEfficiencyRecommendationsAsync(
                GetDepartmentForPosition(position), position, new List<string>());
                
            recommendations.AddRange(positionRecommendations);
            
            // Add additional recommendations based on questionnaire answers
            foreach (var answer in answers)
            {
                // Process each answer to generate specific recommendations
                var answerRecommendations = ProcessAnswerForRecommendations(answer.Key, answer.Value, position);
                recommendations.AddRange(answerRecommendations);
            }
            
            return await Task.FromResult(recommendations);
        }
        
        /// <summary>
        /// Determines the category of a position based on the organogram
        /// </summary>
        private PositionCategory GetPositionCategory(string position)
        {
            var normalizedPosition = position.ToLower();
            
            // Executive positions
            if (normalizedPosition.Contains("managing director") || 
                normalizedPosition.Contains("general manager"))
            {
                return PositionCategory.Executive;
            }
            
            // Management positions
            if (normalizedPosition.Contains("manager") || 
                normalizedPosition.Contains("supervisor") ||
                normalizedPosition.Contains("coordinator") ||
                normalizedPosition.Contains("lead"))
            {
                return PositionCategory.Management;
            }
            
            // Technical positions
            if (normalizedPosition.Contains("engineer") ||
                normalizedPosition.Contains("technician") ||
                normalizedPosition.Contains("qaqc") ||
                normalizedPosition.Contains("electrical"))
            {
                return PositionCategory.Technical;
            }
            
            // Operational positions
            if (normalizedPosition.Contains("operator") ||
                normalizedPosition.Contains("fitter") ||
                normalizedPosition.Contains("welder") ||
                normalizedPosition.Contains("driver") ||
                normalizedPosition.Contains("helper"))
            {
                return PositionCategory.Operational;
            }
            
            // Support positions
            if (normalizedPosition.Contains("hr") ||
                normalizedPosition.Contains("logistics") ||
                normalizedPosition.Contains("material") ||
                normalizedPosition.Contains("document control") ||
                normalizedPosition.Contains("store personnel"))
            {
                return PositionCategory.Support;
            }
            
            return PositionCategory.General;
        }
        
        /// <summary>
        /// Gets the department for a given position
        /// </summary>
        private string GetDepartmentForPosition(string position)
        {
            var normalizedPosition = position.ToLower();
            
            if (normalizedPosition.Contains("engineer") || normalizedPosition.Contains("technician"))
                return "Engineering";
            if (normalizedPosition.Contains("safety"))
                return "Safety";
            if (normalizedPosition.Contains("logistics"))
                return "Logistics";
            if (normalizedPosition.Contains("hr"))
                return "Human Resources";
            if (normalizedPosition.Contains("workshop"))
                return "Workshop Operations";
            if (normalizedPosition.Contains("operations"))
                return "Operations";
            if (normalizedPosition.Contains("project"))
                return "Project Management";
            if (normalizedPosition.Contains("quality") || normalizedPosition.Contains("qaqc"))
                return "Quality Assurance";
            if (normalizedPosition.Contains("material"))
                return "Materials Management";
            
            return "General";
        }
        
        /// <summary>
        /// Gets executive-level questions
        /// </summary>
        private List<Question> GetExecutiveQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "exec-strategy-focus",
                    Text = "What is your primary strategic focus area?",
                    Options = new List<string> { "Growth", "Efficiency", "Innovation", "Risk Management" },
                    Category = "Strategy"
                },
                new Question
                {
                    Id = "exec-decision-making",
                    Text = "How do you prefer to receive critical information?",
                    Options = new List<string> { "Daily Reports", "Weekly Summaries", "Ad-hoc Alerts", "Dashboard Views" },
                    Category = "Information"
                }
            };
        }
        
        /// <summary>
        /// Gets management-level questions
        /// </summary>
        private List<Question> GetManagementQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "mgmt-team-size",
                    Text = "How many people do you directly manage?",
                    Options = new List<string> { "1-5", "6-10", "11-20", "21+" },
                    Category = "Team"
                },
                new Question
                {
                    Id = "mgmt-communication-preference",
                    Text = "What type of communication takes up most of your time?",
                    Options = new List<string> { "Team Meetings", "Email Coordination", "Field Supervision", "Report Preparation" },
                    Category = "Communication"
                }
            };
        }
        
        /// <summary>
        /// Gets technical-level questions
        /// </summary>
        private List<Question> GetTechnicalQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "tech-documentation",
                    Text = "How frequently do you create or review technical documentation?",
                    Options = new List<string> { "Daily", "Weekly", "Monthly", "As Needed" },
                    Category = "Documentation"
                },
                new Question
                {
                    Id = "tech-collaboration",
                    Text = "Which teams do you collaborate with most frequently?",
                    Options = new List<string> { "Design Team", "Field Operations", "Quality Assurance", "Procurement" },
                    Category = "Collaboration"
                }
            };
        }
        
        /// <summary>
        /// Gets operational-level questions
        /// </summary>
        private List<Question> GetOperationalQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "op-scheduling",
                    Text = "How do you currently manage your daily schedule?",
                    Options = new List<string> { "Email Based", "Paper Forms", "Digital App", "Verbal Instructions" },
                    Category = "Scheduling"
                },
                new Question
                {
                    Id = "op-reporting",
                    Text = "What type of operational reports do you submit?",
                    Options = new List<string> { "Daily", "Weekly", "Per Shift", "Incident Based" },
                    Category = "Reporting"
                }
            };
        }
        
        /// <summary>
        /// Gets support-level questions
        /// </summary>
        private List<Question> GetSupportQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "support-coordination",
                    Text = "How do you coordinate with operational teams?",
                    Options = new List<string> { "Direct Communication", "Scheduled Meetings", "Email Updates", "Ticket System" },
                    Category = "Coordination"
                },
                new Question
                {
                    Id = "support-tools",
                    Text = "Which support tools do you use most frequently?",
                    Options = new List<string> { "Inventory Systems", "HR Platforms", "Logistics Software", "Document Control" },
                    Category = "Tools"
                }
            };
        }
        
        /// <summary>
        /// Gets general questions applicable to all positions
        /// </summary>
        private List<Question> GetGeneralQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = "gen-email-volume",
                    Text = "Approximately how many emails do you send/receive per day?",
                    Options = new List<string> { "Less than 10", "10-25", "26-50", "More than 50" },
                    Category = "Communication"
                },
                new Question
                {
                    Id = "gen-peak-time",
                    Text = "When are you most active on email?",
                    Options = new List<string> { "Morning (6am-10am)", "Mid-day (10am-2pm)", "Afternoon (2pm-6pm)", "Evening (6pm-10pm)" },
                    Category = "Productivity"
                }
            };
        }
        
        /// <summary>
        /// Gets position-specific questions
        /// </summary>
        private List<Question> GetPositionSpecificQuestions(string position)
        {
            var questions = new List<Question>();
            var normalizedPosition = position.ToLower();
            
            if (normalizedPosition.Contains("workshop manager"))
            {
                questions.Add(new Question
                {
                    Id = "ws-equipment-focus",
                    Text = "Which type of workshop equipment do you manage most?",
                    Options = new List<string> { "Machining Equipment", "Welding Equipment", "Testing Equipment", "All of the Above" },
                    Category = "Workshop Operations"
                });
            }
            else if (normalizedPosition.Contains("engineer"))
            {
                questions.Add(new Question
                {
                    Id = "eng-discipline",
                    Text = "What is your primary engineering discipline?",
                    Options = new List<string> { "Mechanical", "Electrical", "Civil", "Process", "Other" },
                    Category = "Engineering"
                });
            }
            else if (normalizedPosition.Contains("safety"))
            {
                questions.Add(new Question
                {
                    Id = "safety-focus",
                    Text = "What is your primary safety responsibility?",
                    Options = new List<string> { "Training", "Auditing", "Incident Investigation", "Policy Development" },
                    Category = "Safety"
                });
            }
            else if (normalizedPosition.Contains("logistics"))
            {
                questions.Add(new Question
                {
                    Id = "logistics-focus",
                    Text = "What is your primary logistics function?",
                    Options = new List<string> { "Transportation", "Warehousing", "Supply Chain", "Procurement" },
                    Category = "Logistics"
                });
            }
            
            return questions;
        }
        
        /// <summary>
        /// Processes an answer to generate specific recommendations
        /// </summary>
        private List<string> ProcessAnswerForRecommendations(string questionId, string answer, string position)
        {
            var recommendations = new List<string>();
            
            switch (questionId)
            {
                case "gen-email-volume":
                    if (answer.Contains("More than 50"))
                    {
                        recommendations.Add("With high email volume, consider setting up filters and automated sorting for efficiency");
                        recommendations.Add("Use scheduled email checking times to maintain focus on core tasks");
                    }
                    break;
                    
                case "op-reporting":
                    if (answer.Contains("Daily"))
                    {
                        recommendations.Add("For daily reporting, consider automating data collection from email communications");
                        recommendations.Add("Set up email templates to streamline daily report creation");
                    }
                    break;
                    
                case "mgmt-team-size":
                    if (answer.Contains("21+"))
                    {
                        recommendations.Add("With a large team, implement email distribution lists for efficient communication");
                        recommendations.Add("Use email tracking to ensure important messages are read by all team members");
                    }
                    break;
            }
            
            return recommendations;
        }
    }
    
    /// <summary>
    /// Represents a questionnaire question
    /// </summary>
    public class Question
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string Category { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Categories of positions in the organogram
    /// </summary>
    public enum PositionCategory
    {
        Executive,
        Management,
        Technical,
        Operational,
        Support,
        General
    }
}