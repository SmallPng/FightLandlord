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
			Assert.Throws<ArgumentException>(() => GamePlay.Create(new[] { "���1", "���2", "���3", "���4" }, 2, 5));
			Assert.Throws<ArgumentException>(() => GamePlay.Create(new[] { "���1", "���2" }, 2, 5));

			var ddz = GamePlay.Create(new[] { "���1", "���2", "���3" }, 2, 5);
			var data = db[ddz.Id];
			//ϴ�ƣ�����
			Assert.NotNull(ddz);
			Assert.NotNull(data);
			Assert.Equal(GameStage.δ��ʼ, data.Stage);

			ddz.Shuffle();
			Assert.Equal(0, data.Bong);
			Assert.Empty(data.Chupai);
			Assert.Equal(3, data.Dipai.Length);
			Assert.Equal(2, data.Multiple);
			Assert.Equal(0, data.MultipleAddition);
			Assert.Equal(5, data.MultipleAdditionMax);
			Assert.Equal(3, data.Players.Count);
			Assert.Equal(GamePlayerRole.δ֪, data.Players[0].Role);
			Assert.Equal(GamePlayerRole.δ֪, data.Players[1].Role);
			Assert.Equal(GamePlayerRole.δ֪, data.Players[2].Role);
			Assert.Equal(17, data.Players[0].PokerInit.Count);
			Assert.Equal(17, data.Players[1].PokerInit.Count);
			Assert.Equal(17, data.Players[2].PokerInit.Count);
			Assert.Equal(17, data.Players[0].Poker.Count);
			Assert.Equal(17, data.Players[1].Poker.Count);
			Assert.Equal(17, data.Players[2].Poker.Count);
			Assert.Equal("���1", data.Players[0].Id);
			Assert.Equal("���2", data.Players[1].Id);
			Assert.Equal("���3", data.Players[2].Id);
			Assert.Equal(GameStage.�е���, data.Stage);

			//���Ƿ��ظ�
			Assert.Equal(54, data.Players[0].Poker.Concat(data.Players[1].Poker).Concat(data.Players[2].Poker).Concat(data.Dipai).Distinct().Count());

			//GetById
			Assert.Equal(GamePlay.GetById(ddz.Id).Id, ddz.Id);
			Assert.Throws<ArgumentException>(() => GamePlay.GetById(null));
			Assert.Throws<ArgumentException>(() => GamePlay.GetById(""));
			Assert.Throws<ArgumentException>(() => GamePlay.GetById("slkdjglkjsdg"));

			//������
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord("���10", 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectFarmer("���10"));
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[Math.Min(data.PlayerIndex + 1, data.Players.Count - 1)].Id, 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectFarmer(data.Players[Math.Min(data.PlayerIndex + 1, data.Players.Count - 1)].Id));
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 1);
			Assert.Equal(GameStage.�е���, data.Stage);
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 1));
			Assert.Throws<ArgumentException>(() => ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 100));
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 2);
			Assert.Equal(GameStage.�е���, data.Stage);
			ddz.SelectLandlord(data.Players[data.PlayerIndex].Id, 3);
			Assert.Equal(GameStage.�е���, data.Stage);

			ddz.SelectFarmer(data.Players[data.PlayerIndex].Id);
			Assert.Equal(GameStage.�е���, data.Stage);
			Assert.Equal(2, data.Players.Where(a => a.Role == GamePlayerRole.δ֪).Count());

			//�Էⶥ�������õ���
			//ddz.SelectLandlord(data.players[data.playerIndex].player, 5);
			//����ũ�񶼲������ɱ��ֵ������õ���
			ddz.SelectFarmer(data.Players[data.PlayerIndex].Id);

			Assert.Equal(GameStage.������, data.Stage);
			Assert.Equal(GamePlayerRole.����, data.Players[data.PlayerIndex].Role);
			Assert.Equal(2, data.Players.Where(a => a.Role == GamePlayerRole.ũ��).Count());


		}
	}
}
