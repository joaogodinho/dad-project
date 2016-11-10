﻿using CommonCode.Comms;
using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Interfaces
{
    public interface IReplica : IRemoteObject
    {
        //type and return of Tasks to be used in process request and response should be refined.

        Task<bool> processRequest(DTO blob);

        Task<bool> processResponse(DTO blob);
        

    }
}