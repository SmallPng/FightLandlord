using System;
using System.Collections.Generic;
using System.Linq;

namespace BetGame.DDZ {

	public class Utils {
		private static readonly string[] AllPokerTexts = "🃃,🃓,🂳,🂣,🃄,🃔,🂴,🂤,🃅,🃕,🂵,🂥,🃆,🃖,🂶,🂦,🃇,🃗,🂷,🂧,🃈,🃘,🂸,🂨,🃉,🃙,🂹,🂩,🃊,🃚,🂺,🂪,🃋,🃛,🂻,🂫,🃍,🃝,🂽,🂭,🃎,🃞,🂾,🂮,🃁,🃑,🂱,🂡,🃂,🃒,🂲,🂢,🃟,🂿,🂠".Split(',');

		/// <summary>
		/// 获取一副新牌
		/// </summary>
		/// <returns></returns>
		internal static List<int> GetNewPoker() {
			var list = new List<int>();
			for (int a = 0; a < 54; a++) list.Add(a);
			return list;
		}
		/// <summary>
		/// 判断是否连续
		/// </summary>
		/// <param name="poker">使用前请先排序</param>
		/// <returns></returns>
		public static bool IsSeries(IEnumerable<int> poker) {
			if (poker == null || poker.Any() == false) return false;
			poker = poker.OrderBy(a => a);
			if (poker.Last() >= 15) return false;
			int pp = 255;
			foreach (var p in poker) {
				if (pp != 255 && (
					p - pp != 1
					)) return false;
				pp = p;
			}
			return true;
		}
		/// <summary>
		/// 获取扑克牌文本
		/// </summary>
		/// <param name="poker"></param>
		/// <returns></returns>
		public static string[] GetPokerText(IEnumerable<int> poker) {
			var sb = new List<string>();
			foreach (var p in poker)
				sb.Add(AllPokerTexts[p < 0 || p > 53 ? AllPokerTexts.Length - 1 : p]);
			return sb.ToArray();
		}
		internal static HandPokerComplieResult GetHandPokerComplieResult(HandPokerType type, IEnumerable<GroupByPokerResult> gb) {
			if (type == HandPokerType.个 ||
				type == HandPokerType.对 ||
				type == HandPokerType.三条) {
				var pk = gb.First().poker.OrderByDescending(a => a).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gb.First().Key, Value = pk, Text = GetPokerText(pk) };
			}
			if (type == HandPokerType.三条带一个) {
				var gb3 = gb.Where(a => a.Count == 3).First();
				var gb1 = gb.Where(a => a.Count == 1).First();
				var value = gb3.poker.OrderByDescending(a => a).Concat(gb1.poker).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gb3.Key, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.三条带一对) {
				var gb3 = gb.Where(a => a.Count == 3).First();
				var gb2 = gb.Where(a => a.Count == 2).First();
				var value = gb3.poker.OrderByDescending(a => a).Concat(gb2.poker.OrderByDescending(a => a)).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gb3.Key, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.顺子) {
				var gbs = gb.OrderBy(a => a.Key);
				var value = gbs.Select(a => a.poker.First()).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gbs.Last().Key, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.连对 ||
				type == HandPokerType.飞机) {
				var gbs = gb.OrderBy(a => a.Key);
				var tmp = new List<int>();
				int cv = 0;
				foreach (var g in gb) {
					var gpk = g.poker.OrderByDescending(a => a);
					tmp.AddRange(gpk);
					cv = g.Key;
				}
				var value = tmp.ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = cv, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.飞机带N个) {
				var gb3 = gb.Where(a => a.Count == 3).OrderBy(a => a.Key);
				var gb1 = gb.Where(a => a.Count == 1).OrderBy(a => a.Key);
				var tmp3 = new List<int>();
				int cv = 0;
				foreach (var g in gb3) {
					var gpk = g.poker.OrderByDescending(a => a);
					tmp3.AddRange(gpk);
					cv = g.Key;
				}
				var tmp1 = new List<int>();
				foreach (var g in gb1) tmp1.Add(g.poker.First());
				var value = tmp3.Concat(tmp1).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = cv, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.飞机带N对) {
				var gb3 = gb.Where(a => a.Count == 3).OrderBy(a => a.Key);
				var gb2 = gb.Where(a => a.Count == 2).OrderBy(a => a.Key);
				var tmp3 = new List<int>();
				int cv = 0;
				foreach (var g in gb3) {
					var gpk = g.poker.OrderByDescending(a => a);
					tmp3.AddRange(g.poker.OrderByDescending(a => a));
					cv = g.Key;
				}
				var tmp2 = new List<int>();
				foreach (var g in gb2) tmp2.AddRange(g.poker.OrderByDescending(a => a));
				var value = tmp3.Concat(tmp2).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = cv, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.炸带二个) {
				int cv = 0;
				var gb4 = gb.Where(a => a.Count == 4);
				var gb4len2 = gb4.Any() == false;
				if (gb4len2) gb4 = gb.Where(a => a.Key == 16 || a.Key == 17);
				cv = gb4.First().Key;
				var gb1 = gb4len2 ? gb.Where(a => a.Count == 1 && a.Key < 16) : gb.Where(a => a.Count == 1);
				var tmp4 = new List<int>();
				foreach (var g in gb4) tmp4.AddRange(g.poker.OrderByDescending(a => a));
				var tmp1 = new List<int>();
				foreach (var g in gb1) tmp1.AddRange(g.poker);
				var value = tmp4.Concat(tmp1).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = cv, Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.炸带二对) {
				int cv = 0;
				var gb4 = gb.Where(a => a.Count == 4);
				var gb4len2 = gb4.Any() == false;
				if (gb4len2) gb4 = gb.Where(a => a.Key == 16 || a.Key == 17);
				cv = gb4.First().Key;
				var gb2 = gb4len2 ? gb.Where(a => a.Count == 1 && a.Key < 16) : gb.Where(a => a.Count == 2);
				var tmp4 = new List<int>();
				foreach (var g in gb4) tmp4.AddRange(g.poker.OrderByDescending(a => a));
				var tmp2 = new List<int>();
				foreach (var g in gb2) tmp2.AddRange(g.poker.OrderByDescending(a => a));
				var value = tmp4.Concat(tmp2).ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gb4len2 ? tmp4.First() : tmp4.Last(), Value = value, Text = GetPokerText(value) };
			}
			if (type == HandPokerType.四条炸 ||
				type == HandPokerType.王炸) {
				var tmp = new List<int>();
				foreach (var g in gb) tmp.AddRange(g.poker.OrderByDescending(a => a));
				var value = tmp.ToArray();
				return new HandPokerComplieResult { Type = type, CompareValue = gb.First().Key, Value = value, Text = GetPokerText(value) };
			}
			throw new ArgumentException("GetHandPokerComplieResult 参数错误");
		}
		/// <summary>
		/// 分组扑克牌
		/// </summary>
		/// <param name="poker"></param>
		/// <returns></returns>
		public static IEnumerable<GroupByPokerResult> GroupByPoker(int[] poker) {
			if (poker == null || poker.Length == 0) return null;
			var dic = new Dictionary<int, GroupByPokerTmpResult>();
			for (var a = 0; a < poker.Length; a++) {
				int key = 0;
				if (poker[a] >= 0 && poker[a] < 52) key = (int)(poker[a] / 4 + 3);
				else if (poker[a] == 52) key = 16;
				else if (poker[a] == 53) key = 17;
				if (key == 0) throw new ArgumentException("poker 参数值错误");

				if (dic.ContainsKey(key) == false) dic.Add(key, new GroupByPokerTmpResult());
				dic[key].count++;
				dic[key].poker.Add(poker[a]);
			}
			return dic.Select(a => new GroupByPokerResult { Key = a.Key, Count = a.Value.count, poker = a.Value.poker }).OrderByDescending(a => a.Count);
		}

		/// <summary>
		/// 编译一出牌
		/// </summary>
		/// <param name="gb"></param>
		/// <returns></returns>
		public static HandPokerComplieResult ComplierHandPoker(IEnumerable<GroupByPokerResult> gb) {
			if (gb == null || gb.Any() == false) return null;
			var pokerLength = gb.Sum(a => a.Count);

			if (pokerLength == 1) { //个
				return Utils.GetHandPokerComplieResult(HandPokerType.个, gb);
			}
			if (pokerLength == 2) { //对，王炸
				if (gb.Where(a => a.Count == 2).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.对, gb);
				if (gb.Where(a => a.Key == 16 || a.Key == 17).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.王炸, gb);
			}
			if (pokerLength == 3) { //三条
				if (gb.Where(a => a.Count == 3).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.三条, gb);
			}
			if (pokerLength == 4) { //四条炸，三条带一个，炸带二个
				if (gb.Where(a => a.Count == 4).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.四条炸, gb);
				if (gb.Where(a => a.Count == 3).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.三条带一个, gb);
				if (gb.Where(a => a.Key == 16 || a.Key == 17).Count() == 2) return Utils.GetHandPokerComplieResult(HandPokerType.炸带二个, gb);
			}
			if (pokerLength == 5) { //顺子，三条带一对
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 5 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);
				if (gb.Where(a => a.Count == 3).Any() && gb.Where(a => a.Count == 2).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.三条带一对, gb);
			}
			if (pokerLength == 6) { //顺子，连对，飞机，炸带二个
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 6 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);

				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 3 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 2 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机, gb);

				if (gb.Where(a => a.Count == 4).Any()) return Utils.GetHandPokerComplieResult(HandPokerType.炸带二个, gb);
				if (gb.Where(a => a.Key == 16 || a.Key == 17).Count() == 2 && gb.Select(a => a.Count == 2).Count() == 2) return Utils.GetHandPokerComplieResult(HandPokerType.炸带二个, gb);
			}
			if (pokerLength == 7) { //顺子
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 7 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);
			}
			if (pokerLength == 8) { //顺子，连对，飞机带个，炸带二对
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 8 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);

				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 4 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 2 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N个, gb);

				if (gb.Where(a => a.Count == 4).Any() && gb.Where(a => a.Count == 2).Count() == 2) return Utils.GetHandPokerComplieResult(HandPokerType.炸带二对, gb);
			}
			if (pokerLength == 9) { //顺子，飞机
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 9 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 3 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机, gb);
			}
			if (pokerLength == 10) { //顺子，连对，飞机带队
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 10 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);

				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 5 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 2 && Utils.IsSeries(gb3) && gb.Where(a => a.Count == 2).Count() == 2) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N对, gb);
			}
			if (pokerLength == 11) { //顺子
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 11 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);
			}
			if (pokerLength == 12) { //顺子，连对，飞机，飞机带个
				var gb1 = gb.Where(a => a.Count == 1).Select(a => a.Key).ToArray();
				if (gb1.Length == 12 && Utils.IsSeries(gb1)) return Utils.GetHandPokerComplieResult(HandPokerType.顺子, gb);

				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 6 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 4 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机, gb);
				if (gb3.Length == 3 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N个, gb);
			}
			if (pokerLength == 14) { //连对
				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 7 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);
			}
			if (pokerLength == 15) { //飞机，飞机带队
				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 5 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机, gb);
				if (gb3.Length == 3 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N对, gb);
			}
			if (pokerLength == 16) { //连对，飞机带个
				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 8 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 4 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N个, gb);
			}
			if (pokerLength == 18) { //连对，飞机
				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 9 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 6 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机, gb);
			}
			if (pokerLength == 20) { //连对，飞机带个，飞机带队
				var gb2 = gb.Where(a => a.Count == 2).Select(a => a.Key).ToArray();
				if (gb2.Length == 10 && Utils.IsSeries(gb2)) return Utils.GetHandPokerComplieResult(HandPokerType.连对, gb);

				var gb3 = gb.Where(a => a.Count == 3).Select(a => a.Key).ToArray();
				if (gb3.Length == 5 && Utils.IsSeries(gb3)) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N个, gb);
				if (gb3.Length == 4 && Utils.IsSeries(gb3) && gb.Where(a => a.Count == 2).Count() == 4) return Utils.GetHandPokerComplieResult(HandPokerType.飞机带N对, gb);
			}
			return null;
		}
		public static int CompareHandPoker(HandPokerInfo poker1, HandPokerInfo poker2) {
			if (poker1 == null) return -1;
			if (poker2 == null) return 1;
			switch (poker2.Result.Type) {
				case HandPokerType.个:
				case HandPokerType.对:
				case HandPokerType.三条:
				case HandPokerType.三条带一个:
				case HandPokerType.三条带一对:
					if (poker1.Result.Type == poker2.Result.Type) return poker1.Result.CompareValue.CompareTo(poker2.Result.CompareValue);
					if (poker1.Result.Type == HandPokerType.四条炸 || poker1.Result.Type == HandPokerType.王炸) return 1;
					return -1;
				case HandPokerType.顺子:
				case HandPokerType.连对:
				case HandPokerType.飞机:
				case HandPokerType.飞机带N个:
				case HandPokerType.飞机带N对:
					if (poker1.Result.Type == poker2.Result.Type && poker1.Result.Value.Length == poker1.Result.Value.Length) return poker1.Result.CompareValue.CompareTo(poker2.Result.CompareValue);
					if (poker1.Result.Type == HandPokerType.四条炸 || poker1.Result.Type == HandPokerType.王炸) return 1;
					return -1;
				case HandPokerType.炸带二个:
				case HandPokerType.炸带二对:
					if (poker1.Result.Type == poker2.Result.Type) return poker1.Result.CompareValue.CompareTo(poker2.Result.CompareValue);
					if (poker1.Result.Type == HandPokerType.四条炸 || poker1.Result.Type == HandPokerType.王炸) return 1;
					return -1;
				case HandPokerType.四条炸:
				case HandPokerType.王炸:
					if (poker1.Result.Type == poker2.Result.Type) return poker1.Result.CompareValue.CompareTo(poker2.Result.CompareValue);
					if (poker1.Result.Type == HandPokerType.四条炸 || poker1.Result.Type == HandPokerType.王炸) return 1;
					return -1;
			}
			return -1;
		}

		/// <summary>
		/// 从手中所有牌中，查找能压死 uphand 的打法
		/// </summary>
		/// <param name="allpoker">所有牌</param>
		/// <param name="uphand">要压死的牌</param>
		/// <returns></returns>
		public static List<int[]> GetAllTips(IEnumerable<int> allpoker, HandPokerInfo uphand) {
			var pokers = allpoker.ToArray();
			var gb = Utils.GroupByPoker(pokers);
			var jokers = gb.Where(a => a.Count == 1 && a.Key == 16 || a.Key == 17).Select(a => a.poker.First()).ToArray();
			var ret = new List<int[]>();

			if (uphand == null) {
				var hand = Utils.ComplierHandPoker(gb); //尝试一手出完
				if (hand != null && 
					hand.Type != HandPokerType.炸带二个 && 
					hand.Type != HandPokerType.炸带二对)
					return new List<int[]>(new[] { hand.Value });

				var gb1 = gb.Where(a => a.Count == 1 && (jokers.Length == 2 && a.Key != 16 && a.Key != 17 || jokers.Length < 2)).OrderBy(a => a.Key).FirstOrDefault(); //忽略双王
				var gb2 = gb.Where(a => a.Count == 2).OrderBy(a => a.Key).FirstOrDefault();
				if (gb1 != null && (gb2 == null || gb1.Key < gb2.Key)) return new List<int[]>(new[] { gb1.poker.ToArray() });
				if (gb2 != null && (gb1 == null || gb2.Key < gb1.Key)) return new List<int[]>(new[] { gb2.poker.ToArray() });
				return new List<int[]>(new[] { new[] { gb.Min(a => a.Key) } });
			}
			if (uphand.Result.Type == HandPokerType.个) {
				var gb1 = gb.Where(a => a.Count == 1 && a.Key > uphand.Result.CompareValue && (jokers.Length == 2 && a.Key != 16 && a.Key != 17 || jokers.Length < 2)).OrderBy(a => a.Key); //忽略双王
				if (gb1.Any()) ret.AddRange(gb1.Select(a => a.poker.ToArray()));
				if (gb1.Any() == false) {
					var gb2 = gb.Where(a => a.Count == 2 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
					if (gb2.Any()) {
						foreach (var g2 in gb2) ret.AddRange(g2.poker.OrderBy(a => a).Select(a => new[] { a }));
					}
				}
				if (gb1.Any() == false) {
					var gb3 = gb.Where(a => a.Count == 3 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
					if (gb3.Any()) {
						foreach (var g3 in gb3) ret.AddRange(g3.poker.OrderBy(a => a).Select(a => new[] { a }));
					}
				}
				if (ret.Any() == false) {
					var gb4 = gb.Where(a => a.Count == 4).OrderByDescending(a => a.Key);
					if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				}
				if (ret.Any() == false) {
					if (jokers.Length == 2) ret.Add(jokers);
				}
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.对) {
				var gb2 = gb.Where(a => a.Count == 2 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb2.Any()) ret.AddRange(gb2.Select(a => a.poker.OrderByDescending(b => b).ToArray()));
				if (ret.Any() == false) {
					var gb3 = gb.Where(a => a.Count == 3 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
					if (gb3.Any()) ret.AddRange(gb3.Select(a => a.poker.Where((b, c) => c < 2).OrderByDescending(b => b).ToArray()));
				}
				if (ret.Any() == false) {
					var gb4 = gb.Where(a => a.Count == 4).OrderByDescending(a => a.Key);
					if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(b => b).ToArray()));
				}
				if (ret.Any() == false) {
					if (jokers.Length == 2) ret.Add(jokers);
				}
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.三条) {
				var gb3 = gb.Where(a => a.Count == 3 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb3.Any()) ret.AddRange(gb3.Select(a => a.poker.OrderByDescending(b => b).ToArray()));
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.三条带一个) {
				var gb3 = gb.Where(a => a.Count == 3 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb3.Any()) {
					foreach (var g3 in gb3) {
						var gb1 = gb.Where(a => a.Count == 1 && (jokers.Length == 2 && a.Key != 16 && a.Key != 17 || jokers.Length < 2)).OrderBy(a => a.Key); //忽略双王
						if (gb1.Any()) ret.AddRange(gb1.Select(a => g3.poker.OrderByDescending(b => b).Concat(a.poker).ToArray()));
						if (ret.Any() == false) {
							var gb2 = gb.Where(a => a.Count == 2).OrderBy(a => a.Key);
							if (gb2.Any()) {
								foreach (var g2 in gb2) ret.AddRange(g2.poker.OrderBy(a => a).Select(a => g3.poker.OrderByDescending(b => b).Concat(new[] { a }).ToArray()));
							}
						}
						if (ret.Any() == false) {
							var gb33 = gb.Where(a => a.Count == 3 && a.Key != g3.Key).OrderBy(a => a.Key);
							if (gb33.Any()) {
								foreach (var g33 in gb33) ret.AddRange(g33.poker.OrderBy(a => a).Select(a => g3.poker.OrderByDescending(b => b).Concat(new[] { a }).ToArray()));
							}
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.三条带一对) {
				var gb3 = gb.Where(a => a.Count == 3 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb3.Any()) {
					foreach (var g3 in gb3) {
						var gb2 = gb.Where(a => a.Count == 2).OrderBy(a => a.Key);
						if (gb2.Any()) ret.AddRange(gb2.Select(a => g3.poker.OrderByDescending(b => b).Concat(a.poker.OrderByDescending(b => b)).ToArray()));
						if (ret.Any() == false) {
							var gb33 = gb.Where(a => a.Count == 3 && a.Key != g3.Key).OrderBy(a => a.Key);
							if (gb33.Any()) ret.AddRange(gb33.Select(a => g3.poker.OrderByDescending(b => b).Concat(a.poker.Where((b, c) => c < 2).OrderByDescending(b => b)).ToArray()));
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.顺子) {
				var gbs = gb.Where(a => a.Count < 4 && a.Key < 15 && a.Key > uphand.Result.CompareValue - uphand.Result.Value.Length + 1).OrderBy(a => a.Key).ToArray().AsSpan();
				if (gbs.IsEmpty == false) {
					for (var a = 0; a < gbs.Length && gbs.Length - a >= uphand.Result.Value.Length; a++) {
						var ses = gbs.Slice(a, uphand.Result.Value.Length).ToArray();
						if (Utils.IsSeries(ses.Select(b => b.Key))) ret.Add(ses.Select(b => b.poker.First()).ToArray());
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.连对) {
				var gbs = gb.Where(a => a.Count > 1 && a.Count < 4 && a.Key < 15 && a.Key > uphand.Result.CompareValue - uphand.Result.Value.Length / 2 + 1).OrderBy(a => a.Key).ToArray().AsSpan();
				if (gbs.IsEmpty == false) {
					for (var a = 0; a < gbs.Length && gbs.Length - a >= uphand.Result.Value.Length / 2; a++) {
						var ses = gbs.Slice(a, uphand.Result.Value.Length / 2).ToArray();
						if (Utils.IsSeries(ses.Select(b => b.Key))) {
							var tmp2 = new List<int>();
							foreach (var se in ses) tmp2.AddRange(se.poker.Where((b, c) => c < 2).OrderByDescending(b => b));
							ret.Add(tmp2.ToArray());
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.飞机) {
				var gbs = gb.Where(a => a.Count >= 3 && a.Key < 15 && a.Key > uphand.Result.CompareValue - uphand.Result.Value.Length / 3 + 1).OrderBy(a => a.Key).ToArray().AsSpan();
				if (gbs.IsEmpty == false) {
					for (var a = 0; a < gbs.Length && gbs.Length - a >= uphand.Result.Value.Length / 3; a++) {
						var ses = gbs.Slice(a, uphand.Result.Value.Length / 3).ToArray();
						if (Utils.IsSeries(ses.Select(b => b.Key))) {
							var tmp3 = new List<int>();
							foreach (var se in ses) tmp3.AddRange(se.poker.Where((b, c) => c < 3).OrderByDescending(b => b));
							ret.Add(tmp3.ToArray());
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.飞机带N个) {
				var gbs = gb.Where(a => a.Count >= 3 && a.Key < 15 && a.Key > uphand.Result.CompareValue - uphand.Result.Value.Length / 4 + 1).OrderBy(a => a.Key).ToArray().AsSpan();
				if (gbs.IsEmpty == false) {
					for (var a = 0; a < gbs.Length && gbs.Length - a >= uphand.Result.Value.Length / 4; a++) {
						var ses = gbs.Slice(a, uphand.Result.Value.Length / 4).ToArray();
						if (Utils.IsSeries(ses.Select(b => b.Key))) {
							var tmp3 = new List<int>();
							foreach (var se in ses) tmp3.AddRange(se.poker.Where((b, c) => c < 3).OrderByDescending(b => b));

							var gb11 = gb.Where(z => z.Count == 1 && (jokers.Length == 2 && z.Key != 16 && z.Key != 17 || jokers.Length < 2)).OrderBy(z => z.Key); //忽略双王
							if (gb11.Any()) ret.AddRange(gb11.Select(z => tmp3.Concat(z.poker).ToArray()));
							if (ret.Any() == false) {
								var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key);
								if (gb22.Any()) {
									foreach (var g22 in gb22) ret.AddRange(g22.poker.OrderBy(z => z).Select(z => tmp3.Concat(new[] { a }).ToArray()));
								}
							}
							if (ret.Any() == false) {
								var gb33 = gb.Where(z => z.Count == 3 && ses.Where(y => y.Key == z.Key).Any() == false).OrderBy(z => z.Key);
								if (gb33.Any()) {
									foreach (var g33 in gb33) ret.AddRange(g33.poker.OrderBy(z => a).Select(z => tmp3.Concat(new[] { a }).ToArray()));
								}
							}
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.飞机带N对) {
				var gbs = gb.Where(a => a.Count >= 3 && a.Key < 15 && a.Key > uphand.Result.CompareValue - uphand.Result.Value.Length / 5 + 1).OrderBy(a => a.Key).ToArray().AsSpan();
				if (gbs.IsEmpty == false) {
					for (var a = 0; a < gbs.Length && gbs.Length - a >= uphand.Result.Value.Length / 5; a++) {
						var ses = gbs.Slice(a, uphand.Result.Value.Length / 5).ToArray();
						if (Utils.IsSeries(ses.Select(b => b.Key))) {
							var tmp3 = new List<int>();
							foreach (var se in ses) tmp3.AddRange(se.poker.Where((b, c) => c < 3).OrderByDescending(b => b));


							var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key);
							if (gb22.Any()) ret.AddRange(gb22.Select(z => tmp3.Concat(z.poker.OrderByDescending(b => b)).ToArray()));
							if (ret.Any() == false) {
								var gb33 = gb.Where(z => z.Count == 3 && ses.Where(y => y.Key == z.Key).Any() == false).OrderBy(z => z.Key);
								if (gb33.Any()) ret.AddRange(gb33.Select(z => tmp3.Concat(z.poker.Where((b, c) => c < 2).OrderByDescending(b => b)).ToArray()));
							}
						}
					}
				}
				var gb4 = gb.Where(a => a.Count == 4).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.炸带二个) {
				var gb4 = gb.Where(a => a.Count == 4 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb4.Any()) {
					foreach (var g4 in gb4) {
						var gb11 = gb.Where(z => z.Count == 1 && (jokers.Length == 2 && z.Key != 16 && z.Key != 17 || jokers.Length < 2)).OrderBy(z => z.Key).ToArray(); //忽略双王
						if (gb11.Length > 1) {
							for (var a = 0; a < gb11.Length; a++) {
								for (var b = a + 1; b < gb11.Length; b++) {
									ret.Add(g4.poker.OrderByDescending(z => z).Concat(gb11[a].poker).Concat(gb11[b].poker).ToArray());
								}
							}
						}
						if (ret.Any() == false) {
							var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key);
							if (gb22.Any()) ret.AddRange(gb22.Select(y => g4.poker.OrderByDescending(z => z).Concat(y.poker.OrderByDescending(z => z)).ToArray()));
						}
					}
					ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				}
				if (jokers.Length == 2) {
					var gb11 = gb.Where(z => z.Count == 1 && (jokers.Length == 2 && z.Key != 16 && z.Key != 17 || jokers.Length < 2)).OrderBy(z => z.Key).ToArray(); //忽略双王
					if (gb11.Length > 1) {
						for (var a = 0; a < gb11.Length; a++) {
							for (var b = a + 1; b < gb11.Length; b++) {
								ret.Add(jokers.Concat(gb11[a].poker).Concat(gb11[b].poker).ToArray());
							}
						}
					}
					if (ret.Any() == false) {
						var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key);
						if (gb22.Any()) ret.AddRange(gb22.Select(y => jokers.Concat(y.poker.OrderByDescending(z => z)).ToArray()));
					}
					ret.Add(jokers);
				}
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.炸带二对) {
				var gb4 = gb.Where(a => a.Count == 4 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb4.Any()) {
					foreach (var g4 in gb4) {
						var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key).ToArray();
						if (gb22.Length > 1) {
							for (var a = 0; a < gb22.Length; a++) {
								for (var b = a + 1; b < gb22.Length; b++) {
									ret.Add(g4.poker.OrderByDescending(z => z).Concat(gb22[a].poker.OrderByDescending(z => z)).Concat(gb22[b].poker.OrderByDescending(z => z)).ToArray());
								}
							}
						}
					}
					ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				}
				if (jokers.Length == 2) {
					var gb22 = gb.Where(z => z.Count == 2).OrderBy(z => z.Key).ToArray();
					if (gb22.Length > 1) {
						for (var a = 0; a < gb22.Length; a++) {
							for (var b = a + 1; b < gb22.Length; b++) {
								ret.Add(jokers.Concat(gb22[a].poker.OrderByDescending(z => z)).Concat(gb22[b].poker.OrderByDescending(z => z)).ToArray());
							}
						}
					}
					ret.Add(jokers);
				}
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.四条炸) {
				var gb4 = gb.Where(a => a.Count == 4 && a.Key > uphand.Result.CompareValue).OrderBy(a => a.Key);
				if (gb4.Any()) ret.AddRange(gb4.Select(a => a.poker.OrderByDescending(z => z).ToArray()));
				if (jokers.Length == 2) ret.Add(jokers);
				return ret;
			}
			if (uphand.Result.Type == HandPokerType.王炸) {
			}
			return ret;
		}

		public class GroupByPokerResult {
			public int Key { get; set; }
			public int Count { get; set; }
			public IEnumerable<int> poker { get; set; }
		}
		class GroupByPokerTmpResult {
			internal int count { get; set; } = 0;
			internal List<int> poker { get; set; } = new List<int>();
		}
	}

}
