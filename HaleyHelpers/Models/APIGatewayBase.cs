using Haley.Abstractions;
using Haley.Enums;
using Haley.Services;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;

namespace Haley.Models
{
    public abstract class APIGatewayBase<T> : IAPIGateway, IGatewaySessionProvider {
        protected abstract T Parameter { get; }
        protected IClient CLIENT;
        public IAPIGatewaySession? Session { get; set; }
        public async Task<bool> HasValidSession() => (await this.GetStatus()).Status == GatewaySessionStatus.Valid;
        public virtual Task<IAPIGatewaySession?> TryLoadAsync(IAPIGateway gateway) {
            //Load from memory or cache or database. This is basically to avoid unnecessary calls to initialize if we have valid session already available.
            //For PMWeb, there is no DB Load.
            return Task.FromResult<IAPIGatewaySession?>(null); //Just return null.
        }

        //to initialize or refresh session if needed. This is to make sure we have valid session before making any call to the service.
        public abstract Task<GatewaySessionResult> EnsureAsync(IAPIGateway gateway);

        public virtual Task NotifyAsync(IAPIGateway gateway, GatewayNotifyReason reason, string? message) {
            //A way of letting the application or someone else know that there is some important event related to the session or gateway. For example, session is expiring soon, session has expired, user needs to login again, periodic reminder, or any custom reason. This can be used to trigger some action in the application like showing a message to the user, refreshing the session, logging out the user, etc.
            return Task.CompletedTask; //For PMWeb, we dont have anything.. 
        }
    }
}
