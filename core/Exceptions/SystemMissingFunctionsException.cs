using Simulation.Functions;
using System;

namespace Simulation.Exceptions
{
    public class SystemMissingFunctionsException : Exception
    {
        public SystemMissingFunctionsException(Type systemType, StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose)
            : base(GetMessage(systemType, start, update, finish, dispose))
        {
        }

        private static string GetMessage(Type systemType, StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose)
        {
            if (start == default && update == default && finish == default && dispose == default)
            {
                return $"The system `{systemType.Name}` is missing all required functions";
            }
            else
            {
                string message = $"The system `{systemType.Name}` is missing the required function(s): ";
                if (start == default)
                {
                    message += $"`{nameof(StartSystem)}`, ";
                }

                if (update == default)
                {
                    message += $"`{nameof(UpdateSystem)}`, ";
                }

                if (finish == default)
                {
                    message += $"`{nameof(FinishSystem)}`, ";
                }

                if (dispose == default)
                {
                    message += $"`{nameof(DisposeSystem)}`, ";
                }

                return message.TrimEnd(',', ' ');
            }
        }
    }
}