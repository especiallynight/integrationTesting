using System.ComponentModel.DataAnnotations;

namespace Task_Management.Models
{
    public class StatusTask
    {
        [Key]
        public int IdTaskStatus { get; set; }
        public string StatusName { get; set; } 
        public ICollection<CurrentTask> CurrentTasks { get; set; }
    }
}
