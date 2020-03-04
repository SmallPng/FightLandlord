using System;

namespace BetGame.DDZ {
	public class HandPokerInfo {
		/// <summary>
		/// 出牌时间
		/// </summary>
		public DateTime Time { get; set; }
		/// <summary>
		/// 这手牌出自哪位玩家
		/// </summary>
		public int PlayerIndex { get; set; }
		/// <summary>
		/// 牌编译结果
		/// </summary>
		public HandPokerComplieResult Result { get; set; }
	}

	public enum HandPokerType { 个, 对, 三条, 三条带一个, 三条带一对, 顺子, 连对, 飞机, 飞机带N个, 飞机带N对, 炸带二个, 炸带二对, 四条炸, 王炸 }

	public class HandPokerComplieResult {
		public HandPokerType Type { get; set; }
		/// <summary>
		/// 相同类型比较大小
		/// </summary>
		public int CompareValue { get; set; }
		/// <summary>
		/// 牌
		/// </summary>
		public int[] Value { get; set; }
		/// <summary>
		/// 牌面字符串
		/// </summary>
		public string[] Text { get; set; }
	}
}
