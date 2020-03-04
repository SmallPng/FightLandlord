using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BetGame.DDZ {
	public class GamePlayTest {

		[Fact]
		public void Create() {
			Dictionary<string, GameInfo> db = new Dictionary<string, GameInfo>();
			GamePlay.OnGetData = id => db.TryGetValue(id, out var tryout) ? tryout : null;
			GamePlay.OnSaveData = (id, d) => {
				db.TryAdd(id, d);
			};

			Assert.Throws<ArgumentException>(() => GamePlay.Create(null, 2, 5));
			Assert.Throws<ArgumentException>(() => GamePlay.Create(new[] { "玩家1", "玩家2", "玩家3", "玩家4" }, 2, 5));
			Assert.Throws<ArgumentException>(() => GamePlay.Create(new[] { "玩家1", "玩家2" }, 2, 5));

			var ddz = GamePlay.Create(new[] { "玩家1", "玩家2", "玩家3" }, 2, 5);
			var data = db[ddz.Id];
			//洗牌，发牌
			Assert.NotNull(ddz);
			Assert.NotNull(data);
			Assert.Equal(GameStage.未开始, data.Stage);

			ddz.Shuffle();
			Assert.Equal(0, data.Bong);
			Assert.Empty(data.Chupai);
			Assert.Equal(3, data.Dipai.Length);
			Assert.Equal(2, data.Multiple);
			Assert.Equal(0, data.MultipleAddition);
			Assert.Equal(5, data.MultipleAdditionMax);
			Assert.Equal(3, data.Players.Count);
			Assert.Equal(GamePlayerRole.未知, data.Players[0].Role);
			Assert.Equal(GamePlayerRole.未知, data.Players[1].Role);
			Assert.Equal(GamePlayerRole.未知, data.Players[2].Role);
			Assert.Equal(17, data.Players[0].PokerInit.Count);
			Assert.Equal(17, data.Players[1].PokerInit.Count);
			Assert.Equal(17, data.Players[2].PokerInit.Count);
			Assert.Equal(17, data.Players[0].Poker.Count);
			Assert.Equal(17, data.Players[1].Poker.Count);
			Assert.Equal(17, data.Players[2].Poker.Count);
			Assert.Equal("玩家1", data.Players[0].Id);
			Assert.Equal("玩家2", data.Players[1].Id);
			Assert.Equal("玩家3", data.Players[2].Id);
			Assert.Equal(GameStage.叫地主, data.Stage);

			//牌是否重复
			Assert.Equal(54, data.Players[0].Poker.Concat(data.Players[1].Poker).Concat(data.Players[2].Poker).Concat(data.Dipai).Distinct().Count());

			//GetById
			Assert.Equal(GamePlay.GetById(ddz.Id).Id, ddz.Id);
			Assert.Throws<ArgumentException>(() => GamePlay.GetById(null));
			Assert.Throws<ArgumentException>(() => GamePlay.GetById(""));
			Assert.Throws<ArgumentException>(() => GamePlay.GetById("slkdjglkjsdg"));

			//抢地主
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord("玩家10", 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectFarmer("玩家10"));
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[Math.Min(data.PlayerIndex + 1, data.Players.Count - 1)].Id, 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectFarmer(data.Players[Math.Min(data.PlayerIndex + 1, data.Players.Count - 1)].Id));
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 1);
			Assert.Equal(GameStage.叫地主, data.Stage);
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 100));
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 2);
			Assert.Equal(GameStage.叫地主, data.Stage);
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 3);
			Assert.Equal(GameStage.叫地主, data.Stage);

			ddz.SelectFarmer(data.Players[data.PlayerIndex].Id);
			Assert.Equal(GameStage.叫地主, data.Stage);
			Assert.Equal(2, data.Players.Where(a => a.Role == GamePlayerRole.未知).Count());

			//以封顶倍数抢得地主
			//ddz.SelectLandlord(data.players[data.playerIndex].player, 5);
			//两个农民都不抢，由报分的人抢得地主
			ddz.SelectFarmer(data.Players[data.PlayerIndex].Id);

			Assert.Equal(GameStage.斗地主, data.Stage);
			Assert.Equal(GamePlayerRole.地主, data.Players[data.PlayerIndex].Role);
			Assert.Equal(2, data.Players.Where(a => a.Role == GamePlayerRole.农民).Count());


		}
	}
}
