using OCUnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transfer.ModelMails;

namespace ServerOnlineCity.Model
{
    [Serializable]
    public class FMailIncident : IFunctionMail
    {
        /// <summary>
        /// Письмо с событием
        /// </summary>
        public ModelMailStartIncident Mail { get; set; }

        /// <summary>
        /// Номер очереди, зависит от типа события
        /// </summary>
        public int NumberOrder { get; set; }

        /// <summary>
        /// Началто ли уже событие. На игроке на каждую очередь может быть только одно такое.
        /// </summary>
        public bool AlreadyStart { get { return SendTick > 0; } }

        /// <summary>
        /// Инциндент был запущен
        /// </summary>
        private bool MailSended { get; set; }

        /// <summary>
        /// Игровое время, когда событие должно быть отправлено игроку
        /// </summary>
        private long SendTick { get; set; }

        /// <summary>
        /// Игровое время, когда событие перестанет блокировать очередь, будет удалено и новое событие из очереди может быть отправлено.
        /// Рассчитывается после игровых суток от сабатывания события.
        /// </summary>
        private long EndTick { get; set; }

        private float CostBefore { get; set; }

        private float CostAfter { get; set; }

        public FMailIncident(ModelMailStartIncident mail)
        {
            Mail = mail;
            switch (Mail.IncidentType)
            {
                case IncidentTypes.Caravan:
                case IncidentTypes.ChunkDrop:
                case IncidentTypes.Quest:
                    NumberOrder = 1;
                    break;
                case IncidentTypes.Acid:
                    NumberOrder = 2;
                    break;
                case IncidentTypes.Bombing:
                case IncidentTypes.Infistation:
                case IncidentTypes.Raid:
                    NumberOrder = 3;
                    break;
                default:
                    NumberOrder = 0;
                    break;
            }
        }

        public bool Run(ServiceContext context)
        {
            ///Определяем прошел ли срок с момента запуска последнего события
            if (!AlreadyStart)
            {
                var currentIncident = context.Player.FunctionMails
                    .Any(m => (m as FMailIncident)?.AlreadyStart ?? false 
                        && (m as FMailIncident)?.NumberOrder == NumberOrder);
                if (currentIncident) return false;

                ///Перед нами в очереди никого. Начинает операцию!
                ///Проверяем нужно ли перед наподением предупредить
                var delay = CalcDelayStart();
                SendTick = context.Player.Public.LastTick + delay;
                if (delay > 0)
                {
                    Loger.Log($"IncidentLod FMailIncident.Run 1 SendTick={SendTick} MailSended={MailSended} EndTick={EndTick}");
                    context.Player.Mails.Add(GetWarningMail(context));
                    return false;
                }
            }

            ///Если ожидание перед атакой прошло, то отправляем основное письмо с инциндентом
            if (context.Player.Public.LastTick < SendTick) return false;

            if (!MailSended)
            {
                Loger.Log($"IncidentLod FMailIncident.Run 2 SendTick={SendTick} MailSended={MailSended} EndTick={EndTick}");
                context.Player.Mails.Add(Mail);
                SendTick = context.Player.Public.LastTick;
                MailSended = true;
                CostBefore = GetCostTarget(context.Player);
                return false;
            }

            ///Уже отправили письмо. Проверяем прошла ли минимальная задержка.
            if (context.Player.Public.LastTick - SendTick < ServerManager.ServerSettings.GeneralSettings.IncidentTickDelayBetween) return false;

            ///После суток оцениваем задержку и устанавливаем поле EndTick.
            if (EndTick == 0)
            {
                Loger.Log($"IncidentLod FMailIncident.Run 3 SendTick={SendTick} MailSended={MailSended} EndTick={EndTick}");
                CostAfter = GetCostTarget(context.Player);
                EndTick = SendTick + CalcDelayEnd();
                if (MainHelper.DebugMode)
                {
                    context.Player.Mails.Add(new ModelMailMessadge()
                    {
                        From = Repository.GetData.PlayerSystem.Public,
                        To = context.Player.Public,
                        type = ModelMailMessadge.MessadgeTypes.Neutral,
                        label = "Dev: Сутки после инциндента",
                        text = "Прошли сутки после инциндента. Начато ожидание на восстановление перед разблокированием очереди №"
                            + NumberOrder.ToString()
                            + " дней: "
                            + (CalcDelayEnd() / 60000f).ToString("N2")
                            + ". Всего ещё в этой очереди: "
                            + context.Player.FunctionMails
                                .Count(m => m != this && (m as FMailIncident)?.NumberOrder == NumberOrder).ToString()
                    });
                }
            }

            ///Просто ждем окончания EndTick и убираем себя, чтобы очистить очередь ожидания.
            if (context.Player.Public.LastTick < EndTick) return false;

            Loger.Log($"IncidentLod FMailIncident.Run 4 SendTick={SendTick} MailSended={MailSended} EndTick={EndTick}");
            if (MainHelper.DebugMode)
            {
                context.Player.Mails.Add(new ModelMailMessadge()
                {
                    From = Repository.GetData.PlayerSystem.Public,
                    To = context.Player.Public,
                    type = ModelMailMessadge.MessadgeTypes.Neutral,
                    label = "Dev: Инциндент разблокировал очередь",
                    text = "Инциндент разблокировал очередь №"
                        + NumberOrder.ToString()
                        + " дней: "
                        + (CalcDelayEnd() / 60000f).ToString("N2")
                        + ". Всего ещё в этой очереди: "
                        + context.Player.FunctionMails
                            .Count(m => m != this && (m as FMailIncident)?.NumberOrder == NumberOrder).ToString()
                });
            }
            return true;
        }

        private ModelMailMessadge GetWarningMail(ServiceContext context)
        {
            return new ModelMailMessadge()
            {
                From = Repository.GetData.PlayerSystem.Public,
                To = context.Player.Public,
                /*
                Negative - это жёлтый промоугольник. 
                Красный - treatsmall, красный со звуком - treatbig
                Positive - синий со звуком. Visitor - просто синий
                Death - серый со звуком, если не перепутал
                Neutral - серый без звукового сигнала
                */
                type = NumberOrder == 1
                    ? ModelMailMessadge.MessadgeTypes.Positive
                    : ModelMailMessadge.MessadgeTypes.ThreatBig,
                label = "Приближается!".NeedTranslate(),
                text = Mail.IncidentType == IncidentTypes.Caravan
                    ? "Ваши колонисты видят, что что-то приближается. Кажется какой-то игрок захотел вам помочь".NeedTranslate()
                    : Mail.IncidentType == IncidentTypes.Raid
                    ? "Ваши колонисты взволнованы! Они видят приближение большого количества вооруженных людей от другого игрока! Это будет здесь уже через пол дня".NeedTranslate()
                    : "Ваши колонисты взволнованы! Они видят приближение чего-то ужасного от другого игрока! Это будет здесь уже через пол дня".NeedTranslate()
            };
        }


        private float GetCostTarget(PlayerServer player)
        {
            var attCosts = player.CostWorldObjects(Mail.PlaceServerId);
            var attCost = attCosts.MarketValue + attCosts.MarketValuePawn;
            return attCost;
        }

        private long CalcDelayStart()
        {
            return NumberOrder == 3 ? 30000 /*ServerManager.ServerSettings.GeneralSettings.IncidentTickDelayBetween / 2f*/ : 0;
        }

        private long CalcDelayEnd()
        {
            /* Формула КД событий. (1+ lvl * 0.16666) + 0.03333 * (цена поселения/100 000) суток КД после инцидента
Другой вариант:
Дельту стоимости поселения атакуемого до атаки и после. Берем её в % от начальной. 
К этому % прибавляем по 2% за каждую лежащую/мертвую пешку которые до атаки были живыми.
Если итоговое значение упало меньше чем на 4.5% (2 пешки и коцнули стены), то расчет по формуле, но не больше 5 дней.
Если дельта от 4.5% до 10% то по формуле.
Если больше 10% то удваивается.
Измеряется после суток.
            */

            return (long)(ServerManager.ServerSettings.GeneralSettings.IncidentTickDelayBetween
                * ((1f + Mail.IncidentMult * 0.16666f) + 0.03333f * (CostBefore / 100000f)));
        }

    }
}
