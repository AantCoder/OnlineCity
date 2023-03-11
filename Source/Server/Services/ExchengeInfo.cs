using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Common;
using ServerOnlineCity.Model;
using Transfer;
using Transfer.ModelMails;

namespace ServerOnlineCity.Services
{
    internal sealed class ExchengeInfo : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request53ExchengeInfo;

        public int ResponseTypePackage => (int)PackageType.Response54ExchengeInfo;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = exchengeInfo((ModelExchengeInfo)request.Packet, context);
            return result;
        }

        private ModelExchengeInfo exchengeInfo(ModelExchengeInfo request, ServiceContext context)
        {
            try
            {
                lock (context.Player)
                {
                    var data = Repository.GetData;
                    lock (data)
                    {
                        switch (request.Request)
                        {
                            case ModelExchengeInfoRequest.GetCountThing:
                                {
                                    var count = data.OrderOperator.CountThingDef(context.Player, request.Thing);

                                    return new ModelExchengeInfo()
                                    {
                                        Result = count,
                                        Status = 0,
                                        Message = null
                                    };
                                }
                            default:
                                {
                                    return new ModelExchengeInfo()
                                    {
                                        Status = 1,
                                        Message = null
                                    };
                                }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                ExceptionUtil.ExceptionLog(exp, "Server ExchengeInfo login=" + context?.Player?.Public?.Login);

                return new ModelExchengeInfo()
                {
                    Status = 1,
                    Message = null
                };
            }
        }
    }
}