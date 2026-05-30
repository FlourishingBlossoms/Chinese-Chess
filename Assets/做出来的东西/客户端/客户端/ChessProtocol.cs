using System;

public static class ChessProtocol
{
	public const int MSG_TYPE_MOVE = 1;
	public const int MSG_TYPE_RESULT = 2;
	public const int MSG_TYPE_GAME_OVER = 3;

	public struct MsgMove
	{
		public int msg_type;
		public int chess_id;
		public int to_log_x;
		public int to_log_y;
	}

	public struct MsgResult
	{
		public int msg_type;
		public int success;
		public int eated_id;
		public int next_turn_is_red;
	}

	public struct MsgGameOver
	{
		public int msg_type;
		public int winner_is_red;
	}

	public static byte[] BuildMove(int id, int x, int y)
	{
		byte[] data = new byte[16];
		Buffer.BlockCopy(BitConverter.GetBytes(MSG_TYPE_MOVE), 0, data, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(id), 0, data, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(x), 0, data, 8, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(y), 0, data, 12, 4);
		return data;
	}

	public static int Parse(byte[] data, int len, out int type, out object msg)
	{
		type = 0;
		msg = null;
		if (len < 4) return -1;
		type = BitConverter.ToInt32(data, 0);

		if (type == MSG_TYPE_RESULT && len >= 16)
		{
			MsgResult r = new MsgResult();
			r.msg_type = type;
			r.success = BitConverter.ToInt32(data, 4);
			r.eated_id = BitConverter.ToInt32(data, 8);
			r.next_turn_is_red = BitConverter.ToInt32(data, 12);
			msg = r;
			return 0;
		}
		if (type == MSG_TYPE_GAME_OVER && len >= 8)
		{
			MsgGameOver g = new MsgGameOver();
			g.msg_type = type;
			g.winner_is_red = BitConverter.ToInt32(data, 4);
			msg = g;
			return 0;
		}
		return -1;
	}
}