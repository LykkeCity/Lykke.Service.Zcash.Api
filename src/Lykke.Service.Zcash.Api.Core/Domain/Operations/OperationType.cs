using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Zcash.Api.Core.Domain.Operations
{
    public enum OperationType
    {
        /// <summary>
        /// Single Input to Single Output
        /// </summary>
        SISO,

        /// <summary>
        /// Single Input to Multiple Outputs
        /// </summary>
        SIMO,

        /// <summary>
        /// Multiple Inputs to Single Output
        /// </summary>
        MISO,

        /// <summary>
        /// Multiple Inputs to Multiple Outputs (not supported)
        /// </summary>
        MIMO
    }
}
