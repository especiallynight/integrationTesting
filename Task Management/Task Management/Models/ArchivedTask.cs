using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;

namespace Task_Management.Models
{
    public class ArchivedTask
    {
        [Key]
        public int IdArchivedTask { get; set; }
        public int TaskID { get; set; }
        public string TaskName{ get; set; }
        public virtual CurrentTask CurrentTask { get; set; }
        public DateOnly CompletionDate { get; set; }

    }
}
