// Guids.cs
// MUST match guids.h

using System;

namespace LookHub
{
    static class GuidList
    {
        public const string guidLookHubPkgString = "34cbc46c-355b-4fb7-98cb-75d76a6983d7";
        public const string guidLookHubCmdSetString = "9a61784d-d299-43e7-9d11-76fa20bbc532";

        public static readonly Guid guidLookHubCmdSet = new Guid(guidLookHubCmdSetString);
    }
}