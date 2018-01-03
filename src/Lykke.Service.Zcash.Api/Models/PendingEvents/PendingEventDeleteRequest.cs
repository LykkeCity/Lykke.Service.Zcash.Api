using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Lykke.Service.Zcash.Api.Models.PendingEvents
{
    public class PendingEventDeleteRequest
    {
        [MinLength(1)]
        [Required]
        public Guid[] OperationIds { get; set; }
    }
}
