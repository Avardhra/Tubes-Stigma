using System;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

namespace MyBot
{
    public class MyGreedyBot : Bot
    {
        static void Main(string[] args)
        {
            new MyGreedyBot().Start();
        }

        public MyGreedyBot() : base(BotInfo.FromFile("TemplateBot.json")) { }

        public override void Run()
        {
            while (IsRunning)
            {
                TurnGunLeft(10);
            }
        }

        public override void OnScannedBot(ScannedBotEvent e)
        {
            Fire(1);
        }
    }
}