using CoreNetCore.Models;
using CoreNetCore.MQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace CoreNetCore
{
    public static class CoreHostExtension
    {
        public static T GetService<T>(this IHost host) where T : class
        {
            return host.Services.GetService<T>();
        }

        public static bool DeclareQueryHandler(this IHost host, string actionName, Action<MessageEntry> handler)
        {
            var dispatcher = host.GetService<ICoreDispatcher>();
            if (dispatcher == null)
            {
                throw new CoreException("CoreDispatcher not defined");
            }
            return dispatcher.DeclareQueryHandler(actionName, handler);
        }


        public static bool DeclareResponseHandler(this IHost host, string actionName, Action<MessageEntry, string> handler)
        {
            var dispatcher = host.GetService<ICoreDispatcher>();
            if (dispatcher == null)
            {
                throw new CoreException("CoreDispatcher not defined");
            }
            return dispatcher.DeclareResponseHandler(actionName, handler);
        }


        public static void AddHealthcheckHandler(this IHost host,Func<bool> healthcheckHandler)
        {
            var healthck = host.GetService<IHealthcheck>();
            if (healthck == null)
            {
                throw new CoreException("Healthcheck not defined");
            }
            healthck.AddCheck(healthcheckHandler);
        }

        public static MessageEntry CreateMessage(this IHost host, ReceivedMessageEventArgs receivedMessage =null)
        {
            var dispatcher = host.GetService<ICoreDispatcher>();
            if (dispatcher == null)
            {
                throw new CoreException("CoreDispatcher not defined");
            }
            return new MessageEntry(dispatcher, receivedMessage);
        }

       



        
    }
}