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

        bool processRequest(DTO blob);
        void ReadFile();

        void Start();
        void Status();
        void Interval(int time);
        void Crash();
        void Freeze();
        void Unfreeze();
        // TODO Just use the ID, smaller object
        bool TupleProcessed(DTO req);
        bool BeingProcessed(string id);
        void SetAsProcessed(string id);
    }
}
