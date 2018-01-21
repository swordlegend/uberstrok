﻿using log4net;
using UberStrok.Core.Common;
using UberStrok.Core.Views;

namespace UberStrok.Realtime.Server.Game
{
    public class TeamDeathMatchGameRoom : BaseGameRoom
    {
        private readonly static ILog s_log = LogManager.GetLogger(nameof(TeamDeathMatchGameRoom));

        public TeamDeathMatchGameRoom(GameRoomDataView data) : base(data)
        {
            // Space
        }

        public int BlueTeamScore { get; set; }
        public int RedTeamScore { get; set; }

        protected override void OnPlayerKilled(PlayerKilledEventArgs args)
        {
            base.OnPlayerKilled(args);

            foreach (var player in Players)
            {
                if (player.Actor.Cmid != args.AttackerCmid)
                    continue;

                if (player.Actor.Team == TeamID.BLUE)
                    BlueTeamScore++;
                else if (player.Actor.Team == TeamID.RED)
                    RedTeamScore++;
            }

            foreach (var player in Players)
                player.Events.Game.SendUpdateRoundScore(RoundNumber, (short)BlueTeamScore, (short)RedTeamScore);
        }
    }
}