﻿using System;
using Model;
using OCUnion;
using OCUnion.Transfer.Model;
using ServerOnlineCity.Model;
using Transfer;

namespace ServerOnlineCity.Services
{
    internal sealed class AttackOnlineHost : IGenerateResponseContainer
    {
        public int RequestTypePackage => (int)PackageType.Request29;

        public int ResponseTypePackage => (int)PackageType.Response30;

        public ModelContainer GenerateModelContainer(ModelContainer request, ServiceContext context)
        {
            if (context.Player == null) return null;
            var result = new ModelContainer() { TypePacket = ResponseTypePackage };
            result.Packet = attackOnlineHost((AttackHostToSrv)request.Packet, context);
            return result;
        }

        private AttackHostFromSrv attackOnlineHost(AttackHostToSrv fromClient, ServiceContext context)
        {
            lock (context.Player)
            {
                var timeNow = DateTime.UtcNow;
                var data = Repository.GetData;
                var res = new AttackHostFromSrv()
                {
                };

                if (context.Player.AttackData == null)
                {
                    Loger.Log("Server AttackOnlineHost Unexpected error, no data");
                    res.ErrorText = "Unexpected error, no data";
                    return res;
                }

                //передаем управление общему объекту
                res = context.Player.AttackData.RequestHost(fromClient);

                return res;
            }
        }
    }
}