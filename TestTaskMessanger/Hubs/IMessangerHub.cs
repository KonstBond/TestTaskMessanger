﻿namespace TestTaskMessanger.Hubs
{
    public interface IMessangerHub
    {
        Task ReceiveMessage(string username, string message);
        Task ReceiveErrorMessage(string message);
    }
}
