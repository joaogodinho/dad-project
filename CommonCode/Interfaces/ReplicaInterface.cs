using dad_project.Comms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dad_project.Interfaces
{
    public interface ReplicaInterface
    {
        //type and return of Tasks to be used in process request and response should be refined.

        Task<bool> processRequest(DTO blob);

        Task<bool> processResponse(DTO blob);


    }
}
