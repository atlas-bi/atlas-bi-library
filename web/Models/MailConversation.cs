﻿using System;
using System.Collections.Generic;

namespace Atlas_Web.Models
{
    public partial class MailConversation
    {
        public int ConversationId { get; set; }
        public int MessageId { get; set; }

        public virtual MailMessage Message { get; set; }
    }
}
