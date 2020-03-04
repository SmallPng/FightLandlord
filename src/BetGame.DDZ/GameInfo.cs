using System;
using System.Collections.Generic;
using System.Linq;

namespace BetGame.DDZ {
	/// <summary>
	/// 对局详情
	/// </summary>
	public class GameInfo {
		/// <summary>
		/// 打多大（普通基数）结算：multiple * (multipleAddition + Bong)
		/// </summary>
		public decimal Multiple { get; set; }
		/// <summary>
		/// 附加倍数，抢地主环节
		/// </summary>
		public decimal MultipleAddition { get; set; }
		/// <summary>
		/// 设定最大附加倍数
		/// </summary>
		public decimal MultipleAdditionMax { get; set; }
		/// <summary>
		/// 炸弹次数
		/// </summary>
		public decimal Bong { get; set; }
		/// <summary>
		/// 游戏玩家
		/// </summary>
		public List<GamePlayer> Players { get; set; }
		/// <summary>
		/// 轮到哪位玩家操作
		/// </summary>
		public int PlayerIndex { get; set; }
		/// <summary>
		/// 底牌
		/// </summary>
		public int[] Dipai { get; set; }
		public string[] DipaiText => Utils.GetPokerText(this.Dipai);
		/// <summary>
		/// 出牌历史
		/// </summary>
		public List<HandPokerInfo> Chupai { get; set; }
		/// <summary>
		/// 当前游戏阶段
		/// </summary>
		public GameStage Stage { get; set; }

        /// <summary>
        /// 超时未操作，使用它与当前时间(utc)作对比判断，可惩罚 playerIndex
        /// </summary>
        public DateTime OperationTimeout { get; set; }
        public int OperationTimeoutSeconds => (int)OperationTimeout.Subtract(DateTime.UtcNow).TotalSeconds;

        public GameInfo CloneToPlayer(string playerId)
        {
            var game = new GameInfo
            {
                Multiple = Multiple,
                MultipleAddition = MultipleAddition,
                MultipleAdditionMax = MultipleAdditionMax,
                Bong = Bong,
                PlayerIndex = PlayerIndex,
                Chupai = Chupai,
                Stage = Stage,
                OperationTimeout = OperationTimeout,
                Players = new List<GamePlayer>()
            };
            for (var a = 0; a < Players.Count; a++)
            {
                var gp = new GamePlayer
                {
                    Id = Players[a].Id,
                    Poker = Players[a].Poker,
                    PokerInit = Players[a].PokerInit,
                    Role = Players[a].Role,
                    Score = Players[a].Score,
                    Status = Players[a].Status
                };
                game.Players.Add(gp);
                if (Players[a].Id == playerId) continue;
                gp.Poker = gp.Poker.Select(x => 54).ToList();
                gp.PokerInit = gp.PokerInit.Select(x => 54).ToList();
            }
            game.Dipai = Dipai;
            switch (Stage)
            {
                case GameStage.未开始:
                case GameStage.叫地主:
                    game.Dipai = game.Dipai.Select(a => 54).ToArray();
                    break;
                case GameStage.斗地主:
                    break;
                case GameStage.游戏结束:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return game;
        }
	}

	public enum GameStage { 未开始, 叫地主, 斗地主, 游戏结束 }

	public class GamePlayer {
		/// <summary>
		/// 玩家
		/// </summary>
		public string Id { get; set; }
		/// <summary>
		/// 玩家手上的牌
		/// </summary>
		public List<int> Poker { get; set; }
		public string[] PokerText => Utils.GetPokerText(this.Poker);
		/// <summary>
		/// 玩家最初的牌
		/// </summary>
		public List<int> PokerInit { get; set; }
		/// <summary>
		/// 玩家角色
		/// </summary>
		public GamePlayerRole Role { get; set; }

        /// <summary>
        /// 计算结果
        /// </summary>
        public decimal Score { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public GamePlayerStatus Status { get; set; }
	}

	public enum GamePlayerRole { 未知, 地主, 农民 }
    public enum GamePlayerStatus { 正常, 托管, 逃跑 }
}
