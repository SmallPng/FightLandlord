using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace BetGame.DDZ
{
    public class GamePlay
    {
        public static   MemoryCache Cache;

        //private GamePlay()
        //{
        //    if (cache == null)
        //    {
        //        cache = new MemoryCache(new MemoryCacheOptions());
        //    }

        //}

        

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; }
        public GameInfo Data { get; }

        public static Action<string, GameInfo> OnSaveData;
        public static Func<string, GameInfo> OnGetData;
        /// <summary>
        /// 洗牌后作二次分析，在这里可以重新洗牌、重新定庄家
        /// </summary>
        public static Action<GamePlay> OnShuffle;
        /// <summary>
        /// 叫地主阶段，下一位，在这里可以处理机器人自动叫地主、选择农民
        /// </summary>
        public static Action<GamePlay> OnNextSelect;
        /// <summary>
        /// 斗地主阶段，下一位，在这里可以处理机器人自动出牌
        /// </summary>
        public static Action<GamePlay> OnNextPlay;
        /// <summary>
        /// 游戏结束，通知前端
        /// </summary>
        public static Action<GamePlay> OnGameOver;
        /// <summary>
        /// 玩家超时未操作，自动托管，并且已经执行了操作
        /// </summary>
        public static Action<GamePlay> OnOperatorTimeout;

        private static readonly ThreadLocal<Random> Rnd = new ThreadLocal<Random>(() => new Random());
        private static readonly ConcurrentDictionary<string, GamePlay> OperatorTimeoutDic = new ConcurrentDictionary<string, GamePlay>();
        private static readonly Timer Timer2S = new Timer(Timer2sCallback, null, 2000, 2000);
        private static void Timer2sCallback(object state)
        {
            Timer2S.Change(60000, 60000);
            try
            {
                foreach (var k in OperatorTimeoutDic.Keys)
                {
                    if (OperatorTimeoutDic.TryGetValue(k, out var val) == false) continue;
                    if (val.Data.Stage == GameStage.游戏结束)
                    {
                        OperatorTimeoutDic.TryRemove(k, out var old);
                        continue;
                    }

                    if (DateTime.UtcNow.Subtract(val.Data.OperationTimeout).TotalSeconds > 1)
                    {
                        OperatorTimeoutDic.TryRemove(k, out var old);
                        val.Data.Players[val.Data.PlayerIndex].Status = GamePlayerStatus.托管;
                        NextAutoOperator(val);
                        OnOperatorTimeout?.Invoke(val);
                        continue;
                    }
                }
            }
            catch { }
            Timer2S.Change(2000, 2000);
        }
        /// <summary>
        /// 托管后自动处理
        /// </summary>
        /// <param name="game"></param>
        private static void NextAutoOperator(GamePlay game)
        {
            if (game.Data.Stage == GameStage.游戏结束) return;
            var player = game.Data.Players[game.Data.PlayerIndex];
            if (player.Status != GamePlayerStatus.托管) return;
            switch (game.Data.Stage)
            {
                case GameStage.叫地主:
                    game.SelectFarmer(player.Id);
                    break;
                case GameStage.斗地主:
                    var pks = game.PlayTips(player.Id);
                    if (pks.Any() == false)
                        game.Pass(player.Id);
                    else
                        game.Play(player.Id, pks[0]);
                    break;
            }
        }

        private GamePlay(string id)
        {
            if (string.IsNullOrEmpty(id) == false)
            {
                this.Data = this.EventGetData(id);
                if (this.Data == null) throw new ArgumentException("根据 id 参数找不到斗地主数据");
                this.Id = id;
            }
            else
            {
                this.Data = new GameInfo();
                this.Id = Guid.NewGuid().ToString();
            }
        }
        public void SaveData()
        {
            if (this.Data.Stage == GameStage.游戏结束)
            {
                OperatorTimeoutDic.TryRemove(this.Id, out var old);
                OnGameOver?.Invoke(this);
            }
            else
                OperatorTimeoutDic.AddOrUpdate(this.Id, this, (k, old) => this);
            if (OnSaveData != null)
            {
                OnSaveData(this.Id, this.Data);
                return;
            }

            if (Cache != null)
            {
                Cache.Set($"DDZrdb{Id}", Data);

            }
            else
            {
                Cache = new MemoryCache(new MemoryCacheOptions());
                Cache.Set($"DDZrdb{Id}", Data);
            }
            //RedisHelper.HSet($"DDZrdb", this.Id, this.Data);
        }
        private GameInfo EventGetData(string id)
        {
            if (OnGetData != null)
            {
                return OnGetData(id);
            }
            if (Cache != null)
            {
                return Cache.Get<GameInfo>($"DDZrdb{Id}");

            }
            else
            {
                Cache = new MemoryCache(new MemoryCacheOptions());
                return null;
            }
            //return RedisHelper.HGet<GameInfo>("DDZrdb", id);
        }

        /// <summary>
        /// 创建一局游戏
        /// </summary>
        /// <param name="playerIds"></param>
        /// <param name="multiple"></param>
        /// <param name="multipleAdditionMax"></param>
        /// <returns></returns>
        public static GamePlay Create(string[] playerIds, decimal multiple = 1, decimal multipleAdditionMax = 3)
        {
            if (playerIds == null) throw new ArgumentException("players 参数不能为空");
            if (playerIds.Length != 3) throw new ArgumentException("players 参数长度必须 3");

            var fl = new GamePlay(null);
            fl.Data.Multiple = multiple;
            fl.Data.MultipleAdditionMax = multipleAdditionMax;
            fl.Data.Dipai = new int[3];
            fl.Data.Chupai = new List<HandPokerInfo>();
            fl.Data.Stage = GameStage.未开始;
            fl.Data.Players = new List<GamePlayer>();

            for (var a = 0; a < playerIds.Length; a++)
                fl.Data.Players.Add(new GamePlayer { Id = playerIds[a], Poker = new List<int>(), PokerInit = new List<int>(), Role = GamePlayerRole.未知 });

            fl.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(60);
            fl.SaveData();
            return fl;
        }
        /// <summary>
        /// 查找一局游戏
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static GamePlay GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id 参数不能为空");
            return new GamePlay(id);
        }

        /// <summary>
        /// 洗牌
        /// </summary>
        public void Shuffle()
        {
            if (this.Data.Stage != GameStage.未开始) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");

            this.Data.MultipleAddition = 0;
            this.Data.Bong = 0;
            this.Data.Stage = GameStage.叫地主;

            //洗牌
            var tmppks = Utils.GetNewPoker();
            var pks = new byte[tmppks.Count];
            for (var a = 0; a < pks.Length; a++)
            {
                pks[a] = (byte)tmppks[Rnd.Value.Next(tmppks.Count)];
                tmppks.Remove(pks[a]);
            }
            //确定庄家，谁先拿牌
            this.Data.PlayerIndex = Rnd.Value.Next(this.Data.Players.Count);
            ///分牌
            this.Data.Dipai[0] = pks[51];
            this.Data.Dipai[1] = pks[52];
            this.Data.Dipai[2] = pks[53];
            for (int a = 0, b = this.Data.PlayerIndex; a < 51; a++)
            {
                this.Data.Players[b].Poker.Add(pks[a]);
                this.Data.Players[b].PokerInit.Add(pks[a]);
                if (++b >= this.Data.Players.Count) b = 0;
            }
            OnShuffle?.Invoke(this); //在此做AI分析
            for (var a = 0; a < this.Data.Players.Count; a++)
            {
                this.Data.Players[a].Poker.Sort((x, y) => y.CompareTo(x));
            }
            this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(15);
            this.SaveData();
            WriteLog($"【洗牌分牌】完毕，进入【叫地主】环节，轮到庄家 {this.Data.Players[this.Data.PlayerIndex].Id} 先叫");
            OnNextSelect?.Invoke(this);
        }
        void WriteLog(object obj)
        {
            Trace.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {JsonConvert.SerializeObject(obj).Trim('"')}\r\n{this.Id}: {JsonConvert.SerializeObject(this.Data)}");
        }

        /// <summary>
        /// 叫地主
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="multiple"></param>
        public void SelectLandlord(string playerId, decimal multiple)
        {
            if (this.Data.Stage != GameStage.叫地主) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");
            var playerIndex = this.Data.Players.FindIndex(a => a.Id == playerId);
            if (playerIndex == -1) throw new ArgumentException($"{playerId} 不在本局游戏");
            if (playerIndex != this.Data.PlayerIndex) throw new ArgumentException($"还没有轮到 {playerId} 叫地主");
            if (multiple <= this.Data.MultipleAddition) throw new ArgumentException($"multiple 参数应该 > 当前附加倍数 {this.Data.MultipleAddition}");
            if (multiple > this.Data.MultipleAdditionMax) throw new ArgumentException($"multiple 参数应该 <= 设定最大附加倍数 {this.Data.MultipleAdditionMax}");
            this.Data.MultipleAddition = multiple;
            if (this.Data.MultipleAddition == this.Data.MultipleAdditionMax)
            {
                this.Data.Players[this.Data.PlayerIndex].Role = GamePlayerRole.地主;
                this.Data.Players[this.Data.PlayerIndex].Poker.AddRange(this.Data.Dipai);
                this.Data.Players[this.Data.PlayerIndex].Poker.Sort((x, y) => y.CompareTo(x));
                for (var a = 0; a < this.Data.Players.Count; a++) if (this.Data.Players[a].Role == GamePlayerRole.未知) this.Data.Players[a].Role = GamePlayerRole.农民;
                this.Data.Stage = GameStage.斗地主;
                this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(30);
                this.SaveData();
                WriteLog($"{this.Data.Players[this.Data.PlayerIndex].Id} 以设定最大附加倍数【叫地主】成功，进入【斗地主】环节，轮到庄家 {this.Data.Players[this.Data.PlayerIndex].Id} 出牌");
                OnNextPlay?.Invoke(this);
            }
            else
            {
                while (true)
                {
                    if (++this.Data.PlayerIndex >= this.Data.Players.Count) this.Data.PlayerIndex = 0;
                    if (this.Data.Players[this.Data.PlayerIndex].Role == GamePlayerRole.未知) break; //跳过已确定的农民
                }
                if (this.Data.PlayerIndex == playerIndex)
                {
                    this.Data.Players[this.Data.PlayerIndex].Role = GamePlayerRole.地主;
                    this.Data.Players[this.Data.PlayerIndex].Poker.AddRange(this.Data.Dipai);
                    this.Data.Players[this.Data.PlayerIndex].Poker.Sort((x, y) => y.CompareTo(x));
                    this.Data.Stage = GameStage.斗地主;
                    this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(30);
                    this.SaveData();
                    WriteLog($"{this.Data.Players[this.Data.PlayerIndex].Id} 附加倍数{multiple}【叫地主】成功，进入【斗地主】环节，轮到庄家 {this.Data.Players[this.Data.PlayerIndex].Id} 出牌");
                    OnNextSelect?.Invoke(this);
                }
                else
                {
                    this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(15);
                    this.SaveData();
                    WriteLog($"{this.Data.Players[playerIndex].Id} 【叫地主】 +{this.Data.MultipleAddition}倍，轮到 {this.Data.Players[this.Data.PlayerIndex].Id} 叫地主");
                    OnNextSelect?.Invoke(this);
                }
            }
            NextAutoOperator(this);
        }
        /// <summary>
        /// 不叫地主，选择农民
        /// </summary>
        /// <param name="playerId"></param>
        public void SelectFarmer(string playerId)
        {
            if (this.Data.Stage != GameStage.叫地主) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");
            var playerIndex = this.Data.Players.FindIndex(a => a.Id == playerId);
            if (playerIndex == -1) throw new ArgumentException($"{playerId} 不在本局游戏");
            if (playerIndex != this.Data.PlayerIndex) throw new ArgumentException($"还没有轮到 {playerId} 操作");
            this.Data.Players[playerIndex].Role = GamePlayerRole.农民;
            var unkonws = this.Data.Players.Where(a => a.Role == GamePlayerRole.未知).Count();
            if (unkonws == 1 && this.Data.MultipleAddition > 0)
            {
                this.Data.PlayerIndex = this.Data.Players.FindIndex(a => a.Role == GamePlayerRole.未知);
                this.Data.Players[this.Data.PlayerIndex].Role = GamePlayerRole.地主;
                this.Data.Players[this.Data.PlayerIndex].Poker.AddRange(this.Data.Dipai);
                this.Data.Players[this.Data.PlayerIndex].Poker.Sort((x, y) => y.CompareTo(x));
                for (var a = 0; a < this.Data.Players.Count; a++) if (this.Data.Players[a].Role == GamePlayerRole.未知) this.Data.Players[a].Role = GamePlayerRole.农民;
                this.Data.Stage = GameStage.斗地主;
                this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(30);
                this.SaveData();
                WriteLog($"{this.Data.Players[playerIndex].Id} 选择农民，{this.Data.Players[this.Data.PlayerIndex].Id} 【叫地主】成功，进入【斗地主】环节，轮到庄家 {this.Data.Players[this.Data.PlayerIndex].Id} 出牌");
                OnNextPlay?.Invoke(this);
            }
            else if (unkonws == 0)
            {
                this.Data.Stage = GameStage.游戏结束;
                this.SaveData();
                WriteLog($"所有玩家选择农民，【游戏结束】");
            }
            else
            {
                while (true)
                {
                    if (++this.Data.PlayerIndex >= this.Data.Players.Count) this.Data.PlayerIndex = 0;
                    if (this.Data.Players[this.Data.PlayerIndex].Role == GamePlayerRole.未知) break; //跳过已确定的农民
                }
                this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(15);
                this.SaveData();
                WriteLog($"{this.Data.Players[playerIndex].Id} 选择农民，轮到 {this.Data.Players[this.Data.PlayerIndex].Id} 叫地主");
                OnNextSelect?.Invoke(this);
            }
            NextAutoOperator(this);
        }

        /// <summary>
        /// 提示出牌
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public List<int[]> PlayTips(string playerId)
        {
            if (this.Data.Stage != GameStage.斗地主) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");
            var playerIndex = this.Data.Players.FindIndex(a => a.Id == playerId);
            if (playerIndex == -1) throw new ArgumentException($"{playerId} 不在本局游戏");
            if (playerIndex != this.Data.PlayerIndex) throw new ArgumentException($"还没有轮到 {playerId} 出牌");
            var uphand = this.Data.Chupai.LastOrDefault();
            if (uphand?.PlayerIndex == this.Data.PlayerIndex) uphand = null;
            return Utils.GetAllTips(this.Data.Players[this.Data.PlayerIndex].Poker, uphand);
        }

        /// <summary>
        /// 出牌
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="poker"></param>
        public void Play(string playerId, int[] poker)
        {
            if (this.Data.Stage != GameStage.斗地主) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");
            var playerIndex = this.Data.Players.FindIndex(a => a.Id == playerId);
            if (playerIndex == -1) throw new ArgumentException($"{playerId} 不在本局游戏");
            if (playerIndex != this.Data.PlayerIndex) throw new ArgumentException($"还没有轮到 {playerId} 出牌");
            if (poker == null || poker.Length == 0) throw new ArgumentException("poker 不能为空");
            foreach (var pk in poker) if (this.Data.Players[this.Data.PlayerIndex].Poker.Contains(pk) == false) throw new ArgumentException($"{playerId} 手上没有这手牌");
            var hand = new HandPokerInfo { Time = DateTime.Now, PlayerIndex = this.Data.PlayerIndex, Result = Utils.ComplierHandPoker(Utils.GroupByPoker(poker)) };
            if (hand.Result == null) throw new ArgumentException("poker 不是有效的一手牌");
            var uphand = this.Data.Chupai.LastOrDefault();
            if (uphand != null && uphand.PlayerIndex != this.Data.PlayerIndex && Utils.CompareHandPoker(hand, uphand) <= 0) throw new ArgumentException("poker 打不过上一手牌");
            this.Data.Chupai.Add(hand);
            foreach (var pk in poker) this.Data.Players[this.Data.PlayerIndex].Poker.Remove(pk);

            if (hand.Result.Type == HandPokerType.四条炸 || hand.Result.Type == HandPokerType.王炸) this.Data.Bong += 1;

            if (this.Data.Players[this.Data.PlayerIndex].Poker.Count == 0)
            {
                var wealth = this.Data.Multiple * (this.Data.MultipleAddition + this.Data.Bong);
                var dizhuWinner = this.Data.Players[this.Data.PlayerIndex].Role == GamePlayerRole.地主;
                this.Data.Stage = GameStage.游戏结束;
                foreach (var player in this.Data.Players)
                {
                    if (dizhuWinner) player.Score = player.Role == GamePlayerRole.地主 ? 2 * wealth : -wealth;
                    else player.Score = player.Role == GamePlayerRole.地主 ? 2 * -wealth : wealth;
                }
                this.SaveData();
                WriteLog($"{this.Data.Players[playerIndex].Id} 出牌 {hand.Result.Text}，【游戏结束】，{(dizhuWinner ? GamePlayerRole.地主 : GamePlayerRole.农民)} 获得了胜利，本局炸弹 {this.Data.Bong}个，结算金额 {wealth}");
            }
            else
            {
                if (++this.Data.PlayerIndex >= this.Data.Players.Count) this.Data.PlayerIndex = 0;
                this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(30);
                this.SaveData();
                WriteLog($"{this.Data.Players[playerIndex].Id} 出牌 {hand.Result.Text}，轮到 {this.Data.Players[this.Data.PlayerIndex].Id} 出牌");
                OnNextPlay?.Invoke(this);
            }
            NextAutoOperator(this);
        }

        /// <summary>
        /// 不要
        /// </summary>
        /// <param name="playerId"></param>
        public void Pass(string playerId)
        {
            if (this.Data.Stage != GameStage.斗地主) throw new ArgumentException($"游戏阶段错误，当前阶段：{this.Data.Stage}");
            var playerIndex = this.Data.Players.FindIndex(a => a.Id == playerId);
            if (playerIndex == -1) throw new ArgumentException($"{playerId} 不在本局游戏");
            if (playerIndex != this.Data.PlayerIndex) throw new ArgumentException($"还没有轮到 {playerId} 出牌");
            var uphand = this.Data.Chupai.LastOrDefault();
            if (uphand == null) throw new ArgumentException("第一手牌不能 Pass");
            if (uphand.PlayerIndex == this.Data.PlayerIndex) throw new ArgumentException("此时应该出牌，不能 Pass");
            if (++this.Data.PlayerIndex >= this.Data.Players.Count) this.Data.PlayerIndex = 0;
            this.Data.OperationTimeout = DateTime.UtcNow.AddSeconds(30);
            this.SaveData();
            WriteLog($"{this.Data.Players[playerIndex].Id} 不要，轮到 {this.Data.Players[this.Data.PlayerIndex].Id} 出牌");
            OnNextPlay?.Invoke(this);
            NextAutoOperator(this);
        }
    }
}
