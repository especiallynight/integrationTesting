using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Management.Models
{
    public class CurrentTask
    {
        [Key]
        public int task_id { get; set; }
        public string? task_name { get; set; }
        public string? task_description { get; set; }
        public DateOnly dateadded { get; set; }
        public DateOnly deadlinedate { get; set; }
        public bool iscompleted { get; set; }

        public int statusid { get; set; }
        public int priorityid { get; set; }

        public int? project_id { get; set; }
        public int? assigned_user_id { get; set; }

        [ForeignKey(nameof(statusid))]
        public StatusTask? StatusTask { get; set; }

        [ForeignKey(nameof(priorityid))]
        public TaskPriority? TaskPriority { get; set; }
    }
}