﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Atlas_Web.Models
{
    public partial class DpMilestoneTasksCompleted
    {
        public int MilestoneTaskCompletedId { get; set; }
        public int? DataProjectId { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string CompletionUser { get; set; }
        public string Comments { get; set; }
        public string Owner { get; set; }
        public DateTime? DueDate { get; set; }

        public virtual DpDataProject DataProject { get; set; }
    }
}