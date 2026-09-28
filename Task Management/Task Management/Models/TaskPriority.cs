using System.ComponentModel.DataAnnotations;

namespace Task_Management.Models
{
    public class TaskPriority
    {
        [Key]
        public int IdPriority { get; set; }
        public string PriorityType { get; set; }  
        public ICollection<CurrentTask> CurrentTasks { get; set; }
    }
}
