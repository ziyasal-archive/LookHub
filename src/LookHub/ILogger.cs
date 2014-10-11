using System;

namespace LookHub
{
    public interface ILogger
    {
        void Log(string message);
        void Log(string message, Exception ex);
    }
}